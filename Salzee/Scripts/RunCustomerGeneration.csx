#r "nuget: Microsoft.EntityFrameworkCore.SqlServer, 8.0.10"
#r "nuget: Microsoft.EntityFrameworkCore.Design, 8.0.10"

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;

// Build connection string
var connectionString = "Server=localhost;Database=PMSAbbas;User Id=sa;Password=Pakistan@786;Encrypt=Mandatory;TrustServerCertificate=true;";

var optionsBuilder = new DbContextOptionsBuilder<PMSDbContext>();
optionsBuilder.UseSqlServer(connectionString);

using var context = new PMSDbContext(optionsBuilder.Options);

Console.WriteLine("=== Customer Generation Script ===");
Console.WriteLine();

// Get all projects
var projects = await context.Projects
    .Where(p => !string.IsNullOrEmpty(p.Prefix))
    .ToListAsync();

if (!projects.Any())
{
    Console.WriteLine("No projects found with prefixes.");
    return;
}

Console.WriteLine($"Found {projects.Count} project(s):");
foreach (var p in projects)
{
    Console.WriteLine($"  - {p.ProjectName} (Prefix: {p.Prefix})");
}
Console.WriteLine();

// Get sizes and subprojects from Configuration
var sizesConfig = await context.Configurations
    .FirstOrDefaultAsync(c => c.ConfigKey == "sizes");
var sizes = sizesConfig?.ConfigValue?.Split(',').Select(s => s.Trim()).ToArray()
    ?? new[] { "5 Marla", "7 Marla", "10 Marla" };

var subProjectsConfig = await context.Configurations
    .FirstOrDefaultAsync(c => c.ConfigKey == "subprojects");
var subProjects = subProjectsConfig?.ConfigValue?.Split(',').Select(s => s.Trim()).ToArray()
    ?? new[] { "Phase 1", "Phase 2", "Phase 3" };

Console.WriteLine($"Sizes: {string.Join(", ", sizes)}");
Console.WriteLine($"SubProjects: {string.Join(", ", subProjects)}");
Console.WriteLine();

// Get payment plans grouped by project
var paymentPlansByProject = await context.PaymentPlans
    .Where(pp => pp.ProjectID != null)
    .GroupBy(pp => pp.ProjectID!)
    .ToDictionaryAsync(g => g.Key, g => g.ToList());

var random = new Random();
var firstNames = new[] { "Ahmed", "Ali", "Hassan", "Hussain", "Muhammad", "Usman", "Bilal", "Zain", "Hamza", "Omar", "Fatima", "Ayesha", "Zainab", "Maryam", "Khadija", "Amina", "Hafsa", "Sara", "Aisha", "Sumaya" };
var lastNames = new[] { "Khan", "Ahmed", "Ali", "Hassan", "Hussain", "Malik", "Sheikh", "Butt", "Raza", "Iqbal", "Abbas", "Rizvi", "Naqvi", "Zaidi", "Jafri" };
var cities = new[] { "Karachi", "Lahore", "Islamabad", "Rawalpindi", "Faisalabad", "Multan", "Peshawar", "Quetta", "Hyderabad", "Gujranwala" };
var genders = new[] { "Male", "Female" };

int customersPerProject = 100;
int totalCreated = 0;

foreach (var project in projects)
{
    Console.WriteLine($"\nProcessing Project: {project.ProjectName} (Prefix: {project.Prefix})");

    if (!paymentPlansByProject.TryGetValue(project.ProjectID, out var paymentPlans) || !paymentPlans.Any())
    {
        Console.WriteLine($"  Warning: No payment plans found. Skipping...");
        continue;
    }

    Console.WriteLine($"  Found {paymentPlans.Count} payment plan(s)");

    // Get existing customers to find max number
    var existingCustomers = await context.Customers
        .Where(c => c.ProjectID == project.ProjectID &&
                   (c.CustomerID.StartsWith(project.Prefix) || c.CustomerID.StartsWith(project.Prefix + "-")))
        .Select(c => c.CustomerID)
        .ToListAsync();

    int maxNumber = 0;
    foreach (var existingCustomerID in existingCustomers)
    {
        if (existingCustomerID.StartsWith(project.Prefix))
        {
            string existingNumberPart = existingCustomerID.Substring(project.Prefix.Length);
            if (existingNumberPart.StartsWith("-"))
                existingNumberPart = existingNumberPart.Substring(1);
            if (int.TryParse(existingNumberPart, out int parsedNumber) && parsedNumber > maxNumber)
                maxNumber = parsedNumber;
        }
    }

    int startNumber = maxNumber + 1;
    Console.WriteLine($"  Starting from CustomerID number: {startNumber}");
    int createdForProject = 0;

    for (int i = 0; i < customersPerProject; i++)
    {
        try
        {
            int customerNumber = startNumber + i;
            int availableLength = 10 - project.Prefix.Length;
            int maxDigits = Math.Min(5, availableLength);
            string numberPart = customerNumber.ToString().PadLeft(maxDigits, '0');
            if (numberPart.Length > availableLength)
                numberPart = numberPart.Substring(numberPart.Length - availableLength);
            string customerID = $"{project.Prefix}{numberPart}";

            if (await context.Customers.AnyAsync(c => c.CustomerID == customerID))
            {
                Console.WriteLine($"  Customer {customerID} already exists. Skipping...");
                continue;
            }

            var selectedPlan = paymentPlans[random.Next(paymentPlans.Count)];

            var customer = new Customer
            {
                CustomerID = customerID,
                ProjectID = project.ProjectID,
                PlanID = selectedPlan.PlanID,
                FullName = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}",
                FatherName = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}",
                CNIC = $"{random.Next(10000, 99999)}-{random.Next(1000000, 9999999)}-{random.Next(1, 9)}",
                Phone = $"03{random.Next(10, 99)}-{random.Next(1000000, 9999999)}",
                Email = $"customer{customerID.ToLower()}@example.com",
                Gender = genders[random.Next(genders.Length)],
                Nationality = "Pakistani",
                City = cities[random.Next(cities.Length)],
                Country = "Pakistan",
                MailingAddress = $"House #{random.Next(1, 999)}, Street {random.Next(1, 50)}, {cities[random.Next(cities.Length)]}",
                PermanentAddress = $"House #{random.Next(1, 999)}, Street {random.Next(1, 50)}, {cities[random.Next(cities.Length)]}",
                SubProject = subProjects[random.Next(subProjects.Length)],
                RegisteredSize = sizes[random.Next(sizes.Length)],
                Status = "Active",
                CreatedAt = DateTime.Now.AddDays(-random.Next(365))
            };

            context.Customers.Add(customer);
            createdForProject++;

            if (createdForProject % 50 == 0)
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"  Created {createdForProject}/{customersPerProject} customers...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating customer {i + 1}: {ex.Message}");
        }
    }

    if (createdForProject % 50 != 0)
    {
        await context.SaveChangesAsync();
    }

    Console.WriteLine($"  ✓ Completed: Created {createdForProject} customers for {project.ProjectName}");
    totalCreated += createdForProject;
}

Console.WriteLine();
Console.WriteLine($"=== Generation Complete ===");
Console.WriteLine($"Total customers created: {totalCreated}");
