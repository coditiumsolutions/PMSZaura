using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using PMS.Data;
using PMS.Filters;
using PMS.Services;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// acc.Voucher.BankAccountID — omit EF mapping when DB predates Scripts/AMS_Alter_Voucher_BankAccount.sql (SqlException: Invalid column name 'BankAccountID').
PMSDbContextAccCompat.MapVoucherBankAccountColumn =
    builder.Configuration.GetValue<bool>("AmsAccCompat:MapVoucherBankAccountColumn", defaultValue: true);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AmsViewBagFilter>();
    options.Filters.Add<NavigationModuleFilter>();
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});
builder.Services.AddScoped<AmsAccessService>();

// Add Entity Framework
builder.Services.AddDbContext<PMSDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services
builder.Services.AddScoped<SeedDataService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<PMS.Services.IModulePermissionService, PMS.Services.ModulePermissionService>();
builder.Services.AddScoped<PMS.Services.ISiteConfigService, PMS.Services.SiteConfigService>();
builder.Services.AddSingleton<PMS.Services.INavigationService, PMS.Services.NavigationService>();
builder.Services.AddSingleton<PMS.Services.TotpSecretProtector>();
builder.Services.AddSingleton<PMS.Services.ITotpAuthenticatorService, PMS.Services.TotpAuthenticatorService>();
builder.Services.AddScoped<PMS.Services.ITwoFactorConfigService, PMS.Services.TwoFactorConfigService>();
builder.Services.AddScoped<ISurchargeService, SurchargeService>();
builder.Services.AddScoped<IAccountStatementService, AccountStatementService>();
builder.Services.AddScoped<IAccountStatementReportService, AccountStatementReportService>();
builder.Services.Configure<AmsPmsIntegrationOptions>(builder.Configuration.GetSection(AmsPmsIntegrationOptions.SectionName));
builder.Services.AddScoped<IAmsPmsIntegrationService, AmsPmsIntegrationService>();
builder.Services.AddScoped<AmsExportService>();
builder.Services.AddScoped<AmsCoaClearService>();
builder.Services.AddScoped<AmsAdminDeleteService>();
builder.Services.AddScoped<GroqAIService>();
builder.Services.Configure<AmsBackgroundJobsOptions>(builder.Configuration.GetSection(AmsBackgroundJobsOptions.SectionName));
builder.Services.AddHostedService<AmsAccountingJobsHostedService>();

// Persist Data Protection keys to keep auth cookies valid after IIS redeploy/restart.
var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dataProtectionPath))
{
    dataProtectionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PMS", "DataProtectionKeys");
}
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("PMS");

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7); // 7 days session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// Add authentication with persistent cookies (NO SESSION for auth)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // 30 days cookie expiration
        options.SlidingExpiration = true; // Reset expiration on each request (extends 30 days from last activity)
        options.Cookie.Name = ".PMS.Auth"; // Explicit cookie name
        options.Cookie.HttpOnly = true; // Prevent XSS attacks
        options.Cookie.IsEssential = true; // Required for authentication
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        options.Cookie.Path = "/"; // Available site-wide
        options.Cookie.MaxAge = TimeSpan.FromDays(30); // Explicit max age for cookie persistence
        
        // Ensure cookie validation events don't interfere with persistence
        options.Events.OnValidatePrincipal = async context =>
        {
            // With sliding expiration, this will refresh the cookie on each request
            // This ensures the user stays logged in as long as they're active
            if (context.ShouldRenew)
            {
                context.ShouldRenew = true;
            }
        };
        
        // Cookie will persist for 30 days, survives browser restart
        // With SlidingExpiration=true, cookie refreshes on each request, extending the 30-day period
    });

builder.Services.AddAuthorization();

builder.Services.AddCertificateForwarding(options =>
{
    options.CertificateHeader = "X-ARR-ClientCert";
    options.HeaderConverter = value =>
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        try
        {
            return new X509Certificate2(Convert.FromBase64String(value));
        }
        catch
        {
            return null;
        }
    };
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "https://smartventures.com.pk",
                "http://localhost:8080",
                "http://localhost:3000",
                "http://172.20.229.3:8099",
                "https://172.20.229.3:8099",
                "http://app.virtualsofttechnology.com",
                "https://app.virtualsofttechnology.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
var wellKnownPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", ".well-known");
Directory.CreateDirectory(wellKnownPath);
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/.well-known",
    FileProvider = new PhysicalFileProvider(wellKnownPath),
    ServeUnknownFileTypes = true,
    DefaultContentType = "text/plain"
});

app.UseRouting();

// Enable CORS early, before auth
app.UseCors("AllowSpecificOrigins");

// Add session middleware
app.UseSession();

// Add authentication and authorization middleware
app.UseCertificateForwarding();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Start Kestrel first, then initialize DB in the background so HTTPS binds even when SQL is unreachable.
_ = Task.Run(async () =>
{
    try
    {
        await PMS.Services.DatabaseInitializer.InitializeAsync(app.Services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.GetBaseException().Message}");
    }
});

app.Run();
