# PowerShell script to generate customers
# This script uses the existing project's DLL

$ErrorActionPreference = "Stop"

Write-Host "=== Customer Generation Script ===" -ForegroundColor Cyan
Write-Host ""

# Build the project first
Write-Host "Building project..." -ForegroundColor Yellow
Set-Location "d:\PMS\PMS\PMS"
dotnet build --configuration Release --no-restore | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Create a simple C# file that can be executed
$scriptContent = @"
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;

class GenerateCustomers
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Customer Generation Script ===");
        Console.WriteLine();

        var connectionString = "Server=localhost;Database=DBZaura;User Id=sa;Password=Pakistan@786;Encrypt=Mandatory;TrustServerCertificate=true;";
        var optionsBuilder = new DbContextOptionsBuilder<PMSDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var context = new PMSDbContext(optionsBuilder.Options);

        try
        {
            Console.WriteLine("Connecting to database...");
            await context.Database.CanConnectAsync();
            Console.WriteLine("Connected successfully!");
            Console.WriteLine();

            var projects = await context.Projects.Where(p => !string.IsNullOrEmpty(p.Prefix)).ToListAsync();
            if (!projects.Any()) { Console.WriteLine("No projects found."); return; }

            Console.WriteLine($"Found {projects.Count} project(s):");
            foreach (var p in projects) Console.WriteLine($"  - {p.ProjectName} (Prefix: {p.Prefix})");
            Console.WriteLine();

            var sizesConfig = await context.Configurations.FirstOrDefaultAsync(c => c.ConfigKey == "sizes");
            var sizes = sizesConfig?.ConfigValue?.Split(',').Select(s => s.Trim()).ToArray() ?? new[] { "5 Marla", "7 Marla", "10 Marla" };

            var subProjectsConfig = await context.Configurations.FirstOrDefaultAsync(c => c.ConfigKey == "subprojects");
            var subProjects = subProjectsConfig?.ConfigValue?.Split(',').Select(s => s.Trim()).ToArray() ?? new[] { "Phase 1", "Phase 2", "Phase 3" };

            var paymentPlansByProject = await context.PaymentPlans.Where(pp => pp.ProjectID != null).GroupBy(pp => pp.ProjectID!).ToDictionaryAsync(g => g.Key, g => g.ToList());

            var random = new Random();
            var firstNames = new[] { "Ahmed", "Ali", "Hassan", "Hussain", "Muhammad", "Usman", "Bilal", "Zain", "Hamza", "Omar", "Fatima", "Ayesha", "Zainab", "Maryam", "Khadija" };
            var lastNames = new[] { "Khan", "Ahmed", "Ali", "Hassan", "Hussain", "Malik", "Sheikh", "Butt", "Raza", "Iqbal" };
            var cities = new[] { "Karachi", "Lahore", "Islamabad", "Rawalpindi", "Faisalabad" };
            var genders = new[] { "Male", "Female" };

            int customersPerProject = 100;
            int totalCreated = 0;

            foreach (var project in projects)
            {
                Console.WriteLine($"Processing Project: {project.ProjectName} (Prefix: {project.Prefix})");
                if (!paymentPlansByProject.TryGetValue(project.ProjectID, out var paymentPlans) || !paymentPlans.Any()) { Console.WriteLine("  No payment plans. Skipping..."); continue; }

                var existingCustomers = await context.Customers.Where(c => c.ProjectID == project.ProjectID && (c.CustomerID.StartsWith(project.Prefix) || c.CustomerID.StartsWith(project.Prefix + "-"))).Select(c => c.CustomerID).ToListAsync();
                int maxNumber = 0;
                foreach (var id in existingCustomers)
                {
                    if (id.StartsWith(project.Prefix))
                    {
                        string numPart = id.Substring(project.Prefix.Length);
                        if (numPart.StartsWith("-")) numPart = numPart.Substring(1);
                        if (int.TryParse(numPart, out int parsed) && parsed > maxNumber) maxNumber = parsed;
                    }
                }

                int startNumber = maxNumber + 1;
                Console.WriteLine($"  Starting from: {startNumber}");
                int created = 0;

                for (int i = 0; i < customersPerProject; i++)
                {
                    try
                    {
                        int num = startNumber + i;
                        int availLen = 10 - project.Prefix.Length;
                        string numPart = num.ToString().PadLeft(Math.Min(5, availLen), '0');
                        if (numPart.Length > availLen) numPart = numPart.Substring(numPart.Length - availLen);
                        string customerID = `$"{project.Prefix}{numPart}";

                        if (await context.Customers.AnyAsync(c => c.CustomerID == customerID)) continue;

                        var plan = paymentPlans[random.Next(paymentPlans.Count)];
                        var customer = new Customer
                        {
                            CustomerID = customerID, ProjectID = project.ProjectID, PlanID = plan.PlanID,
                            FullName = `$"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}",
                            FatherName = `$"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}",
                            CNIC = `$"{random.Next(10000, 99999)}-{random.Next(1000000, 9999999)}-{random.Next(1, 9)}",
                            Phone = `$"03{random.Next(10, 99)}-{random.Next(1000000, 9999999)}",
                            Email = `$"customer{customerID.ToLower()}@example.com",
                            Gender = genders[random.Next(genders.Length)], Nationality = "Pakistani",
                            City = cities[random.Next(cities.Length)], Country = "Pakistan",
                            MailingAddress = `$"House #{random.Next(1, 999)}, Street {random.Next(1, 50)}, {cities[random.Next(cities.Length)]}",
                            PermanentAddress = `$"House #{random.Next(1, 999)}, Street {random.Next(1, 50)}, {cities[random.Next(cities.Length)]}",
                            SubProject = subProjects[random.Next(subProjects.Length)],
                            RegisteredSize = sizes[random.Next(sizes.Length)],
                            Status = "Active", CreatedAt = DateTime.Now.AddDays(-random.Next(365))
                        };

                        context.Customers.Add(customer);
                        created++;
                        if (created % 50 == 0) { await context.SaveChangesAsync(); Console.WriteLine($"  Created {created}/{customersPerProject}..."); }
                    }
                    catch (Exception ex) { Console.WriteLine($"  Error: {ex.Message}"); }
                }

                if (created % 50 != 0) await context.SaveChangesAsync();
                Console.WriteLine(`$"  ✓ Created {created} customers");
                totalCreated += created;
            }

            Console.WriteLine(`$"Total created: {totalCreated}");
        }
        catch (Exception ex) { Console.WriteLine(`$"Error: {ex.Message}"); }
    }
}
"@

$scriptFile = "d:\PMS\PMS\PMS\Scripts\TempGenerate.cs"
$scriptContent | Out-File -FilePath $scriptFile -Encoding UTF8

Write-Host "Running customer generation..." -ForegroundColor Yellow
Write-Host ""

# Use dotnet to run the script with the project references
dotnet run --project "d:\PMS\PMS\PMS\PMS.csproj" --no-build -- "$scriptFile"

Remove-Item $scriptFile -ErrorAction SilentlyContinue
