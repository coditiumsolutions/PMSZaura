using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Add Customers for Specific Project ===");
        Console.WriteLine();

        var projectID = "5CF67911C0"; // Apartment project
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

            // Get the project
            var project = await context.Projects
                .FirstOrDefaultAsync(p => p.ProjectID == projectID);

            if (project == null)
            {
                Console.WriteLine($"Project with ID '{projectID}' not found!");
                return;
            }

            if (string.IsNullOrEmpty(project.Prefix))
            {
                Console.WriteLine($"Project '{project.ProjectName}' does not have a Prefix. Cannot generate CustomerIDs.");
                return;
            }

            Console.WriteLine($"Project: {project.ProjectName} (Prefix: {project.Prefix})");
            Console.WriteLine();

            // Get payment plans for this project
            var paymentPlans = await context.PaymentPlans
                .Where(pp => pp.ProjectID == projectID)
                .ToListAsync();

            if (!paymentPlans.Any())
            {
                Console.WriteLine("Warning: No payment plans found for this project.");
                Console.WriteLine("Creating customers without payment plan assignment...");
            }
            else
            {
                Console.WriteLine($"Found {paymentPlans.Count} payment plan(s)");
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

            var random = new Random();
            var firstNames = new[] { "Ahmed", "Ali", "Hassan", "Hussain", "Muhammad", "Usman", "Bilal", "Zain", "Hamza", "Omar", "Fatima", "Ayesha", "Zainab", "Maryam", "Khadija", "Amina", "Hafsa", "Sara", "Aisha", "Sumaya" };
            var lastNames = new[] { "Khan", "Ahmed", "Ali", "Hassan", "Hussain", "Malik", "Sheikh", "Butt", "Raza", "Iqbal", "Abbas", "Rizvi", "Naqvi", "Zaidi", "Jafri" };
            var cities = new[] { "Karachi", "Lahore", "Islamabad", "Rawalpindi", "Faisalabad", "Multan", "Peshawar", "Quetta", "Hyderabad", "Gujranwala" };
            var genders = new[] { "Male", "Female" };

            // Get existing customers for this project to find max number
            var existingCustomers = await context.Customers
                .Where(c => c.ProjectID == projectID &&
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
            Console.WriteLine($"Starting from CustomerID number: {startNumber}");
            Console.WriteLine($"Adding 90 new customers...");
            Console.WriteLine();

            int customersToAdd = 90;
            int created = 0;

            for (int i = 0; i < customersToAdd; i++)
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
                        continue;
                    }

                    // Select random payment plan if available
                    string? planID = paymentPlans.Any() ? paymentPlans[random.Next(paymentPlans.Count)].PlanID : null;

                    var customer = new Customer
                    {
                        CustomerID = customerID,
                        ProjectID = projectID,
                        PlanID = planID,
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
                    created++;

                    if (created % 50 == 0)
                    {
                        await context.SaveChangesAsync();
                        Console.WriteLine($"  Created {created}/{customersToAdd} customers...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Error creating customer {i + 1}: {ex.Message}");
                }
            }

            if (created % 50 != 0)
            {
                await context.SaveChangesAsync();
            }

            Console.WriteLine();
            Console.WriteLine($"=== Generation Complete ===");
            Console.WriteLine($"Total customers created: {created}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }
}
