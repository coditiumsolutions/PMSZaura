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
        Console.WriteLine("=== Property Generation Script ===");
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

            // Get sizes from Configuration
            var sizesConfig = await context.Configurations
                .FirstOrDefaultAsync(c => c.ConfigKey == "sizes");
            var sizes = sizesConfig?.ConfigValue?.Split(',').Select(s => s.Trim()).ToArray()
                ?? new[] { "5 Marla", "7 Marla", "10 Marla", "1 Kanal", "2 Kanal" };

            Console.WriteLine($"Sizes: {string.Join(", ", sizes)}");
            Console.WriteLine();

            var random = new Random();
            var propertyTypes = new[] { "Residential", "Commercial", "Plot", "Apartment", "Villa" };
            var plotTypes = new[] { "Corner", "Inside", "Corner & Inside", "Main Road", "Back Side" };
            var blocks = new[] { "Block A", "Block B", "Block C", "Block D", "Block 1", "Block 2", "Block 3", "Block 4", "Block 5" };
            var streets = new[] { "Main Street", "Park Avenue", "Garden Road", "Market Street", "School Road", "Hospital Road", "Station Road", "Highway", "Boulevard", "Lane 1", "Lane 2", "Lane 3" };

            int propertiesPerProject = 100;
            int totalCreated = 0;

            foreach (var project in projects)
            {
                Console.WriteLine($"\nProcessing Project: {project.ProjectName} (Prefix: {project.Prefix})");

                // Get existing properties for this project to avoid duplicates
                var existingProperties = await context.Properties
                    .Where(p => p.ProjectID == project.ProjectID)
                    .Select(p => new { p.PlotNo, p.Block })
                    .ToListAsync();

                Console.WriteLine($"  Found {existingProperties.Count} existing properties");
                int createdForProject = 0;
                int plotNumber = 1;

                for (int i = 0; i < propertiesPerProject; i++)
                {
                    try
                    {
                        // Generate unique PlotNo and Block combination
                        string plotNo;
                        string block;
                        int attempts = 0;
                        do
                        {
                            block = blocks[random.Next(blocks.Length)];
                            plotNo = plotNumber.ToString();
                            plotNumber++;
                            attempts++;
                            if (attempts > 1000) // Safety check
                            {
                                plotNo = $"P{random.Next(1000, 9999)}";
                                break;
                            }
                        } while (existingProperties.Any(p => p.PlotNo == plotNo && p.Block == block));

                        // Generate PropertyID (random GUID-based, like the existing system)
                        string propertyID = Guid.NewGuid().ToString("N")[..10].ToUpper();

                        // Check if PropertyID already exists (very unlikely but check anyway)
                        if (await context.Properties.AnyAsync(p => p.PropertyID == propertyID))
                        {
                            propertyID = Guid.NewGuid().ToString("N")[..10].ToUpper();
                        }

                        var property = new Property
                        {
                            PropertyID = propertyID,
                            ProjectID = project.ProjectID,
                            PlotNo = plotNo,
                            Block = block,
                            Street = streets[random.Next(streets.Length)],
                            PlotType = plotTypes[random.Next(plotTypes.Length)],
                            PropertyType = propertyTypes[random.Next(propertyTypes.Length)],
                            Size = sizes[random.Next(sizes.Length)],
                            Status = "Available", // Unallotted
                            CreatedAt = DateTime.Now.AddDays(-random.Next(365)),
                            AdditionalInfo = $"Property in {project.ProjectName}"
                        };

                        context.Properties.Add(property);
                        createdForProject++;

                        // Add to existing list to avoid duplicates in same batch
                        existingProperties.Add(new { PlotNo = (string?)plotNo, Block = (string?)block });

                        if (createdForProject % 50 == 0)
                        {
                            await context.SaveChangesAsync();
                            Console.WriteLine($"  Created {createdForProject}/{propertiesPerProject} properties...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Error creating property {i + 1}: {ex.Message}");
                    }
                }

                if (createdForProject % 50 != 0)
                {
                    await context.SaveChangesAsync();
                }

                Console.WriteLine($"  ✓ Completed: Created {createdForProject} properties for {project.ProjectName}");
                totalCreated += createdForProject;
            }

            Console.WriteLine();
            Console.WriteLine($"=== Generation Complete ===");
            Console.WriteLine($"Total properties created: {totalCreated}");
            Console.WriteLine($"All properties are set to Status: Available (Unallotted)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }
}
