using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;
using BCrypt.Net;

namespace PMS.Services
{
    public class SeedDataService
    {
        private readonly PMSDbContext _context;

        public SeedDataService(PMSDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // Seed ACL Roles
            await SeedRoles();

            // Seed Admin User
            await SeedAdminUser();

            // Seed Sample Projects
            await SeedProjects();

            // Seed Sample Properties
            await SeedProperties();

            // Seed Sample Payment Plans
            await SeedPaymentPlans();

            // Seed NDC section in Configuration (if missing)
            await SeedNDCConfiguration();
            await SeedWaiverConfiguration();
            await SeedPossessionConfiguration();

            await SeedEnforce2FAConfiguration();

            // Seed Users category in Configuration (Departments, Designations)
            await SeedUsersConfiguration();

            // Seed default module permissions for Admin users (full access)
            await SeedUserModulePermissionsAsync();

            // Legacy: merge AMS module rows into AccountsManagement (single accounting permission)
            await MigrateLegacyAmsModulePermissionsAsync();
        }

        private static readonly string[] PermissionRank = { "NoAccess", "Read", "Author", "Edit", "Admin" };

        private static int PermissionStrength(string? permission)
        {
            var i = Array.IndexOf(PermissionRank, permission ?? "NoAccess");
            return i < 0 ? 0 : i;
        }

        private static string StrongerPermission(string? a, string? b) =>
            PermissionStrength(a) >= PermissionStrength(b) ? (a ?? "NoAccess") : (b ?? "NoAccess");

        /// <summary>Moves UserModulePermission rows from ModuleKey AMS → AccountsManagement (keeps stronger of both).</summary>
        private async Task MigrateLegacyAmsModulePermissionsAsync()
        {
            while (await _context.UserModulePermissions.AnyAsync(p => p.ModuleKey == "AMS"))
            {
                var ams = await _context.UserModulePermissions.FirstAsync(p => p.ModuleKey == "AMS");
                var mgmt = await _context.UserModulePermissions.FirstOrDefaultAsync(p => p.UserID == ams.UserID && p.ModuleKey == "AccountsManagement");
                if (mgmt == null)
                {
                    _context.UserModulePermissions.Add(new UserModulePermission
                    {
                        UserID = ams.UserID,
                        ModuleKey = "AccountsManagement",
                        Permission = ams.Permission
                    });
                }
                else
                    mgmt.Permission = StrongerPermission(mgmt.Permission, ams.Permission);

                _context.UserModulePermissions.Remove(ams);
                await _context.SaveChangesAsync();
            }
        }

        private static readonly string[] ModuleKeys = new[]
        {
            "Home", "Registration", "Customer", "Transfer", "TransferFee", "NDC", "Project", "Dealer", "Property", "Payment",
            "Allotment", "Rental", "SalesInquiry", "Reports", "Account", "Settings", "ActivityLog",
            "AccountsManagement", "Ticket", "TesSQL", "InquiryApi", "Refund", "DuplicateFileTransfer", "Waiver", "PaymentAudit", "Possession"
        };

        private async Task SeedUserModulePermissionsAsync()
        {
            var adminUsers = await _context.Users.Where(u => u.RoleID == "ADMIN001").Select(u => u.UserID).ToListAsync();
            foreach (var userId in adminUsers)
            {
                var existingKeys = await _context.UserModulePermissions
                    .Where(p => p.UserID == userId)
                    .Select(p => p.ModuleKey)
                    .ToListAsync();
                foreach (var key in ModuleKeys)
                {
                    if (!existingKeys.Contains(key))
                    {
                        _context.UserModulePermissions.Add(new UserModulePermission
                        {
                            UserID = userId,
                            ModuleKey = key,
                            Permission = "Admin"
                        });
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedUsersConfiguration()
        {
            if (!await _context.Configurations.AnyAsync(c => c.ConfigKey == "departments"))
            {
                _context.Configurations.Add(new Configuration
                {
                    ConfigKey = "departments",
                    Category = "Users",
                    ConfigValue = "Admin,IT,Sales,Accounts,CRO,HR,Operations,Finance,Marketing",
                    Description = "User departments (comma-separated)",
                    CreatedAt = DateTime.Now
                });
            }
            if (!await _context.Configurations.AnyAsync(c => c.ConfigKey == "designations"))
            {
                _context.Configurations.Add(new Configuration
                {
                    ConfigKey = "designations",
                    Category = "Users",
                    ConfigValue = "System Administrator,Manager,CRO,Sales Officer,Accountant,HR Officer,Executive",
                    Description = "User designations (comma-separated)",
                    CreatedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedNDCConfiguration()
        {
            if (!await _context.Configurations.AnyAsync(c => c.ConfigKey == "NDCWorkFlowStatus"))
            {
                _context.Configurations.Add(new Configuration
                {
                    ConfigKey = "NDCWorkFlowStatus",
                    Category = "NDC",
                    ConfigValue = "Initiated,Approved,Declined",
                    Description = "NDC workflow statuses (comma-separated)",
                    CreatedAt = DateTime.Now
                });
            }
            if (!await _context.Configurations.AnyAsync(c => c.ConfigKey == "NDCExpiry"))
            {
                _context.Configurations.Add(new Configuration
                {
                    ConfigKey = "NDCExpiry",
                    Category = "NDC",
                    ConfigValue = "14",
                    Description = "NDC validity in days from creation",
                    CreatedAt = DateTime.Now
                });
            }
            if (!await _context.Configurations.AnyAsync(c => c.ConfigKey == "NDCStartNormal"))
            {
                _context.Configurations.Add(new Configuration
                {
                    ConfigKey = "NDCStartNormal",
                    Category = "NDC",
                    ConfigValue = "3",
                    Description = "Issued date = creation date + this many days (used unless type contains Urgent)",
                    CreatedAt = DateTime.Now
                });
            }
            if (!await _context.Configurations.AnyAsync(c => c.ConfigKey == "NDCStartUrgent"))
            {
                _context.Configurations.Add(new Configuration
                {
                    ConfigKey = "NDCStartUrgent",
                    Category = "NDC",
                    ConfigValue = "0",
                    Description = "Issued date = creation date + this many days when NDC Type contains Urgent",
                    CreatedAt = DateTime.Now
                });
            }
            if (!await _context.Configurations.AnyAsync(c => c.ConfigKey == "NDCType"))
            {
                _context.Configurations.Add(new Configuration
                {
                    ConfigKey = "NDCType",
                    Category = "NDC",
                    ConfigValue = "Normal Transfer,Urgent Transfer,Family Transfer,Death Transfer",
                    Description = "NDC types (comma-separated)",
                    CreatedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedWaiverConfiguration()
        {
            if (!await _context.Configurations.AnyAsync(c => c.ConfigKey == "WaiverWorkFlow"))
            {
                _context.Configurations.Add(new Configuration
                {
                    ConfigKey = "WaiverWorkFlow",
                    Category = "Waiver",
                    ConfigValue = "Initialted,Approved,Declined",
                    Description = "Waiver workflow statuses (comma-separated)",
                    CreatedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedPossessionConfiguration()
        {
            if (!await _context.Configurations.AnyAsync(c => c.ConfigKey == "possessionworkflow"))
            {
                _context.Configurations.Add(new Configuration
                {
                    ConfigKey = "possessionworkflow",
                    Category = "Possession",
                    ConfigValue = "Initiated,Operations Desk,Approved,Declined",
                    Description = "Possession workflow statuses (comma-separated)",
                    CreatedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedEnforce2FAConfiguration()
        {
            if (!await _context.Configurations.AnyAsync(c => c.ConfigKey == "Enforce2FA"))
            {
                _context.Configurations.Add(new Configuration
                {
                    ConfigKey = "Enforce2FA",
                    Category = "Security",
                    ConfigValue = "false",
                    Description = "When true, all users must complete Google Authenticator (TOTP) after password. When false, only users with TwoFactorEnabled are prompted.",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedRoles()
        {
            if (!await _context.ACLs.AnyAsync())
            {
                var roles = new List<ACL>
                {
                    new ACL { RoleID = "ADMIN001", RoleName = "Admin", Permissions = "All" },
                    new ACL { RoleID = "MANAGER01", RoleName = "Manager", Permissions = "Customers,Properties,Payments" },
                    new ACL { RoleID = "STAFF001", RoleName = "Staff", Permissions = "Customers,Properties" }
                };

                _context.ACLs.AddRange(roles);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedAdminUser()
        {
            // CHAR(10) UserID is space-padded in SQL; check both ID and email to avoid PK collisions
            // when the row already exists under a different email.
            if (!await UserExistsAsync("USER00001", "admin@jubasmartcity.com"))
            {
                var adminUser = new User
                {
                    UserID = "USER00001",
                    FullName = "System Administrator",
                    Email = "admin@jubasmartcity.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    RoleID = "ADMIN001",
                    Designation = "System Administrator",
                    Department = "IT",
                    UserType = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();
            }

            // Seed additional admin user: abbas@pms.com
            if (!await UserExistsAsync("USER00002", "abbas@pms.com"))
            {
                var abbasUser = new User
                {
                    UserID = "USER00002",
                    FullName = "Abbas",
                    Email = "abbas@pms.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    RoleID = "ADMIN001",
                    Designation = "Administrator",
                    Department = "Admin",
                    UserType = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(abbasUser);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<bool> UserExistsAsync(string userId, string email)
        {
            var paddedId = userId.PadRight(10);
            return await _context.Users.AnyAsync(u =>
                u.UserID == userId ||
                u.UserID == paddedId ||
                u.Email == email);
        }

        private async Task SeedProjects()
        {
            if (!await _context.Projects.AnyAsync())
            {
                var projects = new List<Project>
                {
                    new Project
                    {
                        ProjectID = "PROJ00001",
                        ProjectName = "Juba Smart City Phase 1",
                        Type = "Residential",
                        Location = "Juba, South Sudan",
                        Description = "First phase of Juba Smart City development with modern residential units",
                        CreatedAt = DateTime.Now
                    },
                    new Project
                    {
                        ProjectID = "PROJ00002",
                        ProjectName = "Juba Smart City Phase 2",
                        Type = "Commercial",
                        Location = "Juba, South Sudan",
                        Description = "Commercial phase with office buildings and shopping centers",
                        CreatedAt = DateTime.Now
                    },
                    new Project
                    {
                        ProjectID = "PROJ00003",
                        ProjectName = "Juba Smart City Phase 3",
                        Type = "Mixed",
                        Location = "Juba, South Sudan",
                        Description = "Mixed development with residential and commercial units",
                        CreatedAt = DateTime.Now
                    }
                };

                _context.Projects.AddRange(projects);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedProperties()
        {
            if (!await _context.Properties.AnyAsync())
            {
                var properties = new List<Property>();

                // Generate properties for each project
                for (int i = 1; i <= 3; i++)
                {
                    var projectId = $"PROJ0000{i}";
                    
                    for (int j = 1; j <= 20; j++)
                    {
                        properties.Add(new Property
                        {
                            PropertyID = $"PROP{i:D3}{j:D3}",
                            ProjectID = projectId,
                            PlotNo = $"{i}{j:D3}",
                            Street = $"Street {j}",
                            PlotType = j % 2 == 0 ? "Residential" : "Commercial",
                            Block = $"Block {((j - 1) / 5) + 1}",
                            PropertyType = j % 3 == 0 ? "Apartment" : "Plot",
                            Size = j % 2 == 0 ? "1000 sq ft" : "1500 sq ft",
                            Status = j <= 5 ? "Allotted" : "Available",
                            CreatedAt = DateTime.Now,
                            AdditionalInfo = $"Property in Juba Smart City Phase {i}"
                        });
                    }
                }

                _context.Properties.AddRange(properties);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedPaymentPlans()
        {
            if (!await _context.PaymentPlans.AnyAsync())
            {
                var paymentPlans = new List<PaymentPlan>
                {
                    new PaymentPlan
                    {
                        PlanID = "PLAN00001",
                        ProjectID = "PROJ00001",
                        PlanName = "Basic Residential Plan",
                        TotalAmount = 50000,
                        TotalAmountUSD = 50000,
                        ExchangeRate = 1m,
                        Currency = "PKR",
                        DurationMonths = 36,
                        Frequency = "Monthly",
                        Description = "Basic payment plan for residential properties",
                        CreatedAt = DateTime.Now
                    },
                    new PaymentPlan
                    {
                        PlanID = "PLAN00002",
                        ProjectID = "PROJ00001",
                        PlanName = "Premium Residential Plan",
                        TotalAmount = 75000,
                        TotalAmountUSD = 75000,
                        ExchangeRate = 1m,
                        Currency = "PKR",
                        DurationMonths = 48,
                        Frequency = "Monthly",
                        Description = "Premium payment plan for residential properties",
                        CreatedAt = DateTime.Now
                    },
                    new PaymentPlan
                    {
                        PlanID = "PLAN00003",
                        ProjectID = "PROJ00002",
                        PlanName = "Commercial Plan",
                        TotalAmount = 100000,
                        TotalAmountUSD = 100000,
                        ExchangeRate = 1m,
                        Currency = "PKR",
                        DurationMonths = 60,
                        Frequency = "Quarterly",
                        Description = "Payment plan for commercial properties",
                        CreatedAt = DateTime.Now
                    }
                };

                _context.PaymentPlans.AddRange(paymentPlans);
                await _context.SaveChangesAsync();

                // Seed payment schedules
                await SeedPaymentSchedules();
            }
        }

        private async Task SeedPaymentSchedules()
        {
            var paymentPlans = await _context.PaymentPlans.ToListAsync();

            foreach (var plan in paymentPlans)
            {
                if (!plan.DurationMonths.HasValue || plan.DurationMonths.Value <= 0)
                    continue;

                var schedules = new List<PaymentSchedule>();
                var durationMonths = plan.DurationMonths.Value;
                var monthlyAmount = plan.TotalAmount / durationMonths;

                for (int i = 1; i <= durationMonths; i++)
                {
                    schedules.Add(new PaymentSchedule
                    {
                        ScheduleID = $"SCH{plan.PlanID.Substring(4)}{i:D3}",
                        PlanID = plan.PlanID,
                        PaymentDescription = $"Installment {i} of {durationMonths}",
                        InstallmentNo = i,
                        DueDate = DateTime.Now.AddMonths(i),
                        Amount = monthlyAmount,
                        AmountUSD = monthlyAmount,
                        SurchargeApplied = true,
                        SurchargeRate = 0.05m,
                        Description = $"Monthly installment for {plan.PlanName}"
                    });
                }

                _context.PaymentSchedules.AddRange(schedules);
            }

            await _context.SaveChangesAsync();
        }
    }
}
