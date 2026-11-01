using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Salzee.Scripts
{
    public class GenerateCustomersScript
    {
        private readonly PMSDbContext _context;
        private readonly Random _random = new Random();

        // Sample data for generating customers
        private readonly string[] _firstNames = {
            "Ahmed", "Ali", "Hassan", "Hussain", "Muhammad", "Usman", "Bilal", "Zain", "Hamza", "Omar",
            "Fatima", "Ayesha", "Zainab", "Maryam", "Khadija", "Amina", "Hafsa", "Sara", "Aisha", "Sumaya"
        };

        private readonly string[] _lastNames = {
            "Khan", "Ahmed", "Ali", "Hassan", "Hussain", "Malik", "Sheikh", "Butt", "Raza", "Iqbal",
            "Abbas", "Rizvi", "Naqvi", "Zaidi", "Jafri", "Shah", "Mirza", "Baig", "Qureshi", "Siddiqui"
        };

        private readonly string[] _cities = {
            "Karachi", "Lahore", "Islamabad", "Rawalpindi", "Faisalabad", "Multan", "Peshawar", "Quetta", "Hyderabad", "Gujranwala"
        };

        private readonly string[] _genders = { "Male", "Female" };

        public GenerateCustomersScript(PMSDbContext context)
        {
            _context = context;
        }

        public async Task<int> GenerateCustomersForAllProjects(int customersPerProject = 100)
        {
            int totalCreated = 0;

            // Get all projects
            var projects = await _context.Projects
                .Where(p => !string.IsNullOrEmpty(p.Prefix))
                .ToListAsync();

            if (!projects.Any())
            {
                throw new Exception("No projects found with prefixes.");
            }

            // Get sizes and subprojects from Configuration
            var sizesConfig = await _context.Configurations
                .FirstOrDefaultAsync(c => c.ConfigKey == "sizes");
            var sizes = sizesConfig?.ConfigValue?.Split(',').Select(s => s.Trim()).ToArray() 
                ?? new[] { "5 Marla", "7 Marla", "10 Marla" };

            var subProjectsConfig = await _context.Configurations
                .FirstOrDefaultAsync(c => c.ConfigKey == "subprojects");
            var subProjects = subProjectsConfig?.ConfigValue?.Split(',').Select(s => s.Trim()).ToArray()
                ?? new[] { "Phase 1", "Phase 2", "Phase 3" };

            // Get payment plans grouped by project
            var paymentPlansByProject = await _context.PaymentPlans
                .Where(pp => pp.ProjectID != null)
                .GroupBy(pp => pp.ProjectID!)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            foreach (var project in projects)
            {
                Console.WriteLine($"\nProcessing Project: {project.ProjectName} (Prefix: {project.Prefix})");

                // Get payment plans for this project
                if (!paymentPlansByProject.TryGetValue(project.ProjectID, out var paymentPlans) || !paymentPlans.Any())
                {
                    Console.WriteLine($"  Warning: No payment plans found for project {project.ProjectName}. Skipping...");
                    continue;
                }

                // Get existing customers for this project to determine starting number
                var existingCustomers = await _context.Customers
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
                        {
                            existingNumberPart = existingNumberPart.Substring(1);
                        }
                        if (int.TryParse(existingNumberPart, out int parsedNumber))
                        {
                            if (parsedNumber > maxNumber)
                            {
                                maxNumber = parsedNumber;
                            }
                        }
                    }
                }

                int startNumber = maxNumber + 1;
                int createdForProject = 0;

                for (int i = 0; i < customersPerProject; i++)
                {
                    try
                    {
                        // Generate CustomerID
                        int customerNumber = startNumber + i;
                        int availableLength = 10 - project.Prefix.Length;
                        int maxDigits = Math.Min(5, availableLength);
                        string numberPart = customerNumber.ToString().PadLeft(maxDigits, '0');
                        if (numberPart.Length > availableLength)
                        {
                            numberPart = numberPart.Substring(numberPart.Length - availableLength);
                        }
                        string customerID = $"{project.Prefix}{numberPart}";

                        // Check if customer already exists
                        if (await _context.Customers.AnyAsync(c => c.CustomerID == customerID))
                        {
                            Console.WriteLine($"  Customer {customerID} already exists. Skipping...");
                            continue;
                        }

                        // Select random payment plan for this project
                        var selectedPlan = paymentPlans[_random.Next(paymentPlans.Count)];

                        // Generate customer data
                        var customer = new Customer
                        {
                            CustomerID = customerID,
                            ProjectID = project.ProjectID,
                            PlanID = selectedPlan.PlanID,
                            FullName = $"{_firstNames[_random.Next(_firstNames.Length)]} {_lastNames[_random.Next(_lastNames.Length)]}",
                            FatherName = $"{_firstNames[_random.Next(_firstNames.Length)]} {_lastNames[_random.Next(_lastNames.Length)]}",
                            CNIC = GenerateCNIC(),
                            Phone = GeneratePhone(),
                            Email = GenerateEmail(customerID),
                            Gender = _genders[_random.Next(_genders.Length)],
                            Nationality = "Pakistani",
                            City = _cities[_random.Next(_cities.Length)],
                            Country = "Pakistan",
                            MailingAddress = GenerateAddress(),
                            PermanentAddress = GenerateAddress(),
                            SubProject = subProjects[_random.Next(subProjects.Length)],
                            RegisteredSize = sizes[_random.Next(sizes.Length)],
                            Status = "Active",
                            CreatedAt = DateTime.Now.AddDays(-_random.Next(365)) // Random date within last year
                        };

                        _context.Customers.Add(customer);
                        createdForProject++;

                        // Save in batches of 50
                        if (createdForProject % 50 == 0)
                        {
                            await _context.SaveChangesAsync();
                            Console.WriteLine($"  Created {createdForProject} customers for {project.ProjectName}...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Error creating customer {i + 1} for {project.ProjectName}: {ex.Message}");
                    }
                }

                // Save remaining customers
                if (createdForProject % 50 != 0)
                {
                    await _context.SaveChangesAsync();
                }

                Console.WriteLine($"  Completed: Created {createdForProject} customers for {project.ProjectName}");
                totalCreated += createdForProject;
            }

            return totalCreated;
        }

        private string GenerateCNIC()
        {
            // Format: 12345-1234567-1
            return $"{_random.Next(10000, 99999)}-{_random.Next(1000000, 9999999)}-{_random.Next(1, 9)}";
        }

        private string GeneratePhone()
        {
            // Format: 03XX-XXXXXXX
            string[] prefixes = { "0300", "0301", "0302", "0303", "0304", "0305", "0306", "0307", "0308", "0309", "0310", "0311", "0312", "0313", "0314", "0315", "0316", "0317", "0318", "0319", "0320", "0321", "0322", "0323", "0324", "0325", "0330", "0331", "0332", "0333", "0334", "0335", "0336", "0337", "0340", "0341", "0342", "0343", "0344", "0345", "0346", "0347" };
            return $"{prefixes[_random.Next(prefixes.Length)]}-{_random.Next(1000000, 9999999)}";
        }

        private string GenerateEmail(string customerID)
        {
            return $"customer{customerID.ToLower()}@example.com";
        }

        private string GenerateAddress()
        {
            string[] streets = { "Main Street", "Park Avenue", "Garden Road", "Market Street", "School Road", "Hospital Road", "Station Road", "Highway", "Boulevard", "Lane" };
            return $"House #{_random.Next(1, 999)}, {streets[_random.Next(streets.Length)]}, {_cities[_random.Next(_cities.Length)]}";
        }
    }
}
