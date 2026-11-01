using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Salzee.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CleanupOrphanedCustomers
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Cleanup Orphaned Customers Script ===");
            Console.WriteLine();

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Setup DbContext
            var optionsBuilder = new DbContextOptionsBuilder<PMSDbContext>();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

            using var context = new PMSDbContext(optionsBuilder.Options);

            try
            {
                // Find orphaned customers (customers whose ProjectID doesn't exist in Projects table)
                var orphanedCustomers = await context.Customers
                    .Where(c => c.ProjectID != null && 
                        !context.Projects.Any(p => p.ProjectID == c.ProjectID))
                    .ToListAsync();

                if (orphanedCustomers.Count == 0)
                {
                    Console.WriteLine("✓ No orphaned customers found. Database is clean!");
                    return;
                }

                Console.WriteLine($"Found {orphanedCustomers.Count} orphaned customer(s):");
                foreach (var customer in orphanedCustomers)
                {
                    Console.WriteLine($"  - CustomerID: {customer.CustomerID}, ProjectID: {customer.ProjectID}, Name: {customer.FullName ?? "N/A"}");
                }
                Console.WriteLine();

                Console.Write("Do you want to delete these customers and all their related data? (yes/no): ");
                var confirmation = Console.ReadLine()?.Trim().ToLower();

                if (confirmation != "yes" && confirmation != "y")
                {
                    Console.WriteLine("Operation cancelled.");
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("Starting cleanup...");

                int deletedCount = 0;
                foreach (var customer in orphanedCustomers)
                {
                    var customerId = customer.CustomerID;
                    Console.WriteLine($"Processing customer {customerId}...");

                    // Delete related records
                    // 1. Payments
                    var payments = await context.Payments
                        .Where(p => p.CustomerID == customerId)
                        .ToListAsync();
                    if (payments.Count > 0)
                    {
                        context.Payments.RemoveRange(payments);
                        Console.WriteLine($"  - Deleted {payments.Count} payment(s)");
                    }

                    // 2. Allotments
                    var allotments = await context.Allotments
                        .Where(a => a.CustomerID == customerId)
                        .ToListAsync();
                    if (allotments.Count > 0)
                    {
                        context.Allotments.RemoveRange(allotments);
                        Console.WriteLine($"  - Deleted {allotments.Count} allotment(s)");
                    }

                    // 3. Possessions
                    var possessions = await context.Possessions
                        .Where(p => p.CustomerID == customerId)
                        .ToListAsync();
                    if (possessions.Count > 0)
                    {
                        context.Possessions.RemoveRange(possessions);
                        Console.WriteLine($"  - Deleted {possessions.Count} possession(s)");
                    }

                    // 4. Penalties
                    var penalties = await context.Penalties
                        .Where(p => p.CustomerID == customerId)
                        .ToListAsync();
                    if (penalties.Count > 0)
                    {
                        context.Penalties.RemoveRange(penalties);
                        Console.WriteLine($"  - Deleted {penalties.Count} penalty/penalties");
                    }

                    // 5. Waivers
                    var waivers = await context.Waivers
                        .Where(w => w.CustomerID == customerId)
                        .ToListAsync();
                    if (waivers.Count > 0)
                    {
                        context.Waivers.RemoveRange(waivers);
                        Console.WriteLine($"  - Deleted {waivers.Count} waiver(s)");
                    }

                    // 6. Refunds
                    var refunds = await context.Refunds
                        .Where(r => r.CustomerID == customerId)
                        .ToListAsync();
                    if (refunds.Count > 0)
                    {
                        context.Refunds.RemoveRange(refunds);
                        Console.WriteLine($"  - Deleted {refunds.Count} refund(s)");
                    }

                    // 7. Transfers (CustomerID = customer file)
                    var transfers = await context.Transfers
                        .Where(t => t.CustomerID == customerId)
                        .ToListAsync();
                    if (transfers.Count > 0)
                    {
                        context.Transfers.RemoveRange(transfers);
                        Console.WriteLine($"  - Deleted {transfers.Count} transfer(s)");
                    }

                    // 8. NDCs
                    var ndcs = await context.NDCs
                        .Where(n => n.CustomerID == customerId)
                        .ToListAsync();
                    if (ndcs.Count > 0)
                    {
                        context.NDCs.RemoveRange(ndcs);
                        Console.WriteLine($"  - Deleted {ndcs.Count} NDC(s)");
                    }

                    // 9. CustomerLogs (should cascade, but delete explicitly)
                    var customerLogs = await context.CustomerLogs
                        .Where(cl => cl.CustomerID == customerId)
                        .ToListAsync();
                    if (customerLogs.Count > 0)
                    {
                        context.CustomerLogs.RemoveRange(customerLogs);
                        Console.WriteLine($"  - Deleted {customerLogs.Count} customer log(s)");
                    }

                    // 10. Attachments
                    var attachments = await context.Attachments
                        .Where(a => a.RefType == "Customer" && a.RefID == customerId)
                        .ToListAsync();
                    if (attachments.Count > 0)
                    {
                        context.Attachments.RemoveRange(attachments);
                        Console.WriteLine($"  - Deleted {attachments.Count} attachment(s)");
                    }

                    // Finally, delete the customer
                    context.Customers.Remove(customer);
                    deletedCount++;
                    Console.WriteLine($"  ✓ Deleted customer {customerId}");
                    Console.WriteLine();
                }

                // Save all changes
                await context.SaveChangesAsync();

                Console.WriteLine($"✓ Cleanup completed successfully!");
                Console.WriteLine($"  Total customers deleted: {deletedCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
