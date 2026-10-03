using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;

/// <summary>
/// Seeds 100 Customers and 100 Properties per project for dashboard/testing.
/// Usage: dotnet run [connectionString]
/// If connectionString is omitted, uses: Server=localhost;Database=DBZaura;User Id=sa;Password=Pakistan@786;TrustServerCertificate=true;
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = args.Length > 0
            ? args[0]
            : "Server=localhost;Database=DBZaura;User Id=sa;Password=Pakistan@786;Encrypt=Mandatory;TrustServerCertificate=true;";

        var optionsBuilder = new DbContextOptionsBuilder<PMSDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var context = new PMSDbContext(optionsBuilder.Options);

        try
        {
            Console.WriteLine("Connecting to database...");
            await context.Database.CanConnectAsync();
            Console.WriteLine("Connected.\n");

            // --- List all projects ---
            var projects = await context.Projects.OrderBy(p => p.ProjectName).ToListAsync();
            Console.WriteLine("=== All projects in database ===");
            if (projects.Count == 0)
            {
                Console.WriteLine("No projects found. Add projects first.");
                return;
            }
            foreach (var p in projects)
            {
                var custCount = await context.Customers.CountAsync(c => c.ProjectID == p.ProjectID);
                var propCount = await context.Properties.CountAsync(pr => pr.ProjectID == p.ProjectID);
                Console.WriteLine($"  {p.ProjectID} | {p.ProjectName ?? "(no name)"} | Prefix: {p.Prefix ?? "-"} | Customers: {custCount} | Properties: {propCount}");
            }
            Console.WriteLine();

            // --- Seed 100 customers and 100 properties per project ---
            var sizes = await GetSizes(context);
            var subProjects = await GetSubProjects(context);
            var random = new Random();
            var firstNames = new[] { "Ahmed", "Ali", "Hassan", "Hussain", "Muhammad", "Usman", "Bilal", "Zain", "Hamza", "Omar", "Fatima", "Ayesha", "Zainab", "Maryam", "Khadija", "Amina", "Hafsa", "Sara", "Aisha", "Sumaya" };
            var lastNames = new[] { "Khan", "Ahmed", "Ali", "Hassan", "Hussain", "Malik", "Sheikh", "Butt", "Raza", "Iqbal", "Abbas", "Rizvi", "Naqvi", "Zaidi", "Jafri" };
            var cities = new[] { "Karachi", "Lahore", "Islamabad", "Rawalpindi", "Faisalabad", "Multan", "Peshawar", "Quetta", "Hyderabad", "Gujranwala" };
            var genders = new[] { "Male", "Female" };
            var propertyTypes = new[] { "Residential", "Commercial", "Plot", "Apartment", "Villa" };
            var plotTypes = new[] { "Corner", "Inside", "Corner & Inside", "Main Road", "Back Side" };
            var blocks = new[] { "Block A", "Block B", "Block C", "Block D", "Block 1", "Block 2", "Block 3", "Block 4", "Block 5", "Block 6", "Block 7", "Block 8" };
            var streets = new[] { "Main Street", "Park Avenue", "Garden Road", "Market Street", "School Road", "Hospital Road", "Station Road", "Highway", "Boulevard", "Lane 1", "Lane 2", "Lane 3", "Lane 4", "Lane 5" };

            int totalCustomersAdded = 0;
            int totalPropertiesAdded = 0;

            foreach (var project in projects)
            {
                Console.WriteLine($"--- Project: {project.ProjectName} ({project.ProjectID}) ---");

                // Ensure at least one PaymentPlan for this project (for customers)
                var projectPlans = await context.PaymentPlans.Where(pp => pp.ProjectID == project.ProjectID).ToListAsync();
                if (projectPlans.Count == 0)
                {
                    var anyPlan = await context.PaymentPlans.FirstOrDefaultAsync();
                    if (anyPlan == null)
                    {
                        var newPlanId = Guid.NewGuid().ToString("N")[..10].ToUpper();
                        context.PaymentPlans.Add(new PaymentPlan
                        {
                            PlanID = newPlanId,
                            ProjectID = project.ProjectID,
                            PlanName = (project.ProjectName ?? "Project") + " - Default Plan",
                            TotalAmount = 5000000,
                            Currency = "PKR",
                            DurationMonths = 60,
                            Frequency = "Monthly",
                            CreatedAt = DateTime.Now
                        });
                        await context.SaveChangesAsync();
                        projectPlans = await context.PaymentPlans.Where(pp => pp.ProjectID == project.ProjectID).ToListAsync();
                    }
                    else
                    {
                        projectPlans = new List<PaymentPlan> { anyPlan };
                    }
                }

                string? planId = projectPlans[random.Next(projectPlans.Count)].PlanID;
                string prefix = !string.IsNullOrEmpty(project.Prefix) ? project.Prefix.Trim() : "PRJ";

                // Existing customer IDs for this project (prefix-based)
                var existingCustomerIds = await context.Customers
                    .Where(c => c.ProjectID == project.ProjectID && c.CustomerID.StartsWith(prefix))
                    .Select(c => c.CustomerID)
                    .ToListAsync();
                int maxNum = 0;
                foreach (var id in existingCustomerIds)
                {
                    string numPart = id.Length > prefix.Length ? id.Substring(prefix.Length).TrimStart('-') : "";
                    if (int.TryParse(numPart, out int n) && n > maxNum) maxNum = n;
                }
                int startCustomerNum = maxNum + 1;

                // Add 100 customers
                int addedCustomers = 0;
                for (int i = 0; i < 100; i++)
                {
                    int customerNumber = startCustomerNum + i;
                    string numberPart = customerNumber.ToString().PadLeft(5, '0');
                    if (prefix.Length + numberPart.Length > 10) numberPart = numberPart.Substring(numberPart.Length - (10 - prefix.Length));
                    string customerId = prefix + numberPart;

                    if (await context.Customers.AnyAsync(c => c.CustomerID == customerId)) continue;

                    var customer = new Customer
                    {
                        CustomerID = customerId,
                        ProjectID = project.ProjectID,
                        PlanID = planId,
                        FullName = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}",
                        FatherName = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}",
                        CNIC = $"{random.Next(10000, 99999)}-{random.Next(1000000, 9999999)}-{random.Next(1, 9)}",
                        Phone = $"03{random.Next(10, 99)}-{random.Next(1000000, 9999999)}",
                        Email = $"cust{customerId}@example.com",
                        Gender = genders[random.Next(genders.Length)],
                        Nationality = "Pakistani",
                        City = cities[random.Next(cities.Length)],
                        Country = "Pakistan",
                        MailingAddress = $"House #{random.Next(1, 999)}, {cities[random.Next(cities.Length)]}",
                        PermanentAddress = $"House #{random.Next(1, 999)}, {cities[random.Next(cities.Length)]}",
                        SubProject = subProjects[random.Next(subProjects.Length)],
                        RegisteredSize = sizes[random.Next(sizes.Length)],
                        Status = "Active",
                        CreatedAt = DateTime.Now.AddDays(-random.Next(365))
                    };
                    context.Customers.Add(customer);
                    addedCustomers++;
                    totalCustomersAdded++;
                    if (addedCustomers % 50 == 0) await context.SaveChangesAsync();
                }
                if (addedCustomers % 50 != 0) await context.SaveChangesAsync();
                Console.WriteLine($"  Customers added: {addedCustomers}");

                // Existing properties for this project (to avoid duplicate PlotNo+Block)
                var existingProps = await context.Properties
                    .Where(p => p.ProjectID == project.ProjectID)
                    .Select(p => new { p.PlotNo, p.Block })
                    .ToListAsync();
                var existingSet = new HashSet<string>(existingProps.Select(x => $"{x.PlotNo ?? ""}|{x.Block ?? ""}"));
                int plotNumber = existingProps.Count + 1;

                // Add 100 properties
                int addedProperties = 0;
                for (int i = 0; i < 100; i++)
                {
                    string plotNo;
                    string? block;
                    int attempts = 0;
                    do
                    {
                        block = blocks[random.Next(blocks.Length)];
                        plotNo = (plotNumber++).ToString();
                        if (++attempts > 500) plotNo = $"P{random.Next(10000, 99999)}";
                    } while (existingSet.Contains($"{plotNo}|{block}") && attempts <= 500);
                    existingSet.Add($"{plotNo}|{block}");

                    string propertyId = Guid.NewGuid().ToString("N")[..10].ToUpper();
                    while (await context.Properties.AnyAsync(p => p.PropertyID == propertyId))
                        propertyId = Guid.NewGuid().ToString("N")[..10].ToUpper();

                    var property = new Property
                    {
                        PropertyID = propertyId,
                        ProjectID = project.ProjectID,
                        PlotNo = plotNo,
                        Block = block,
                        Street = streets[random.Next(streets.Length)],
                        PlotType = plotTypes[random.Next(plotTypes.Length)],
                        PropertyType = propertyTypes[random.Next(propertyTypes.Length)],
                        Size = sizes[random.Next(sizes.Length)],
                        Status = "Available",
                        CreatedAt = DateTime.Now.AddDays(-random.Next(365)),
                        AdditionalInfo = $"Seed data - {project.ProjectName}"
                    };
                    context.Properties.Add(property);
                    addedProperties++;
                    totalPropertiesAdded++;
                    if (addedProperties % 50 == 0) await context.SaveChangesAsync();
                }
                if (addedProperties % 50 != 0) await context.SaveChangesAsync();
                Console.WriteLine($"  Properties added: {addedProperties}");
                Console.WriteLine();
            }

            Console.WriteLine("=== Seed complete ===");
            Console.WriteLine($"Total customers added: {totalCustomersAdded}");
            Console.WriteLine($"Total properties added: {totalPropertiesAdded}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static async Task<string[]> GetSizes(PMSDbContext context)
    {
        var c = await context.Configurations.FirstOrDefaultAsync(x => x.ConfigKey == "sizes");
        return c?.ConfigValue?.Split(',').Select(s => s.Trim()).ToArray() ?? new[] { "5 Marla", "7 Marla", "10 Marla", "1 Kanal", "2 Kanal" };
    }

    static async Task<string[]> GetSubProjects(PMSDbContext context)
    {
        var c = await context.Configurations.FirstOrDefaultAsync(x => x.ConfigKey == "subprojects");
        return c?.ConfigValue?.Split(',').Select(s => s.Trim()).ToArray() ?? new[] { "Phase 1", "Phase 2", "Phase 3" };
    }
}
