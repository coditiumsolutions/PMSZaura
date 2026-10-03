using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;

/// <summary>
/// Seeds 200 test tickets with statuses: Pending, Assigned, Ongoing, Discarded, Duplicate, Resolved.
/// Usage: dotnet run [connectionString]
/// </summary>
class Program
{
    static readonly string[] Statuses = { "Pending", "Assigned", "Ongoing", "Discarded", "Duplicate", "Resolved" };
    static readonly string[] Titles = {
        "Payment not reflecting", "Allotment letter delay", "Document verification", "Installment schedule query",
        "Property handover issue", "Refund request", "Update contact details", "Duplicate payment concern",
        "NOC application status", "Transfer of ownership", "Tax certificate", "Possession date inquiry",
        "Maintenance complaint", "Parking allocation", "Water connection", "Meter reading dispute",
        "Amenities access", "Complaint about dealer", "Plan upgrade request", "Reschedule payment"
    };
    static readonly string[] Descriptions = {
        "Customer reported that the payment made last week is not showing in the statement.",
        "Waiting for allotment letter since 2 weeks.",
        "Documents submitted for verification, need status update.",
        "Need clarification on next installment due date and amount.",
        "Handover delayed beyond agreed date.",
        "Request for refund of excess amount paid.",
        "Customer wants to update phone and email.",
        "Possible duplicate deduction, please check.",
        "NOC applied last month, no response yet.",
        "Inquiry about transfer process and charges.",
        "Need tax certificate for current financial year.",
        "When will possession be given?",
        "Lift not working in Block B.",
        "Parking space not as per agreement.",
        "Water supply issue in unit.",
        "Meter reading seems incorrect.",
        "Club house access card not working.",
        "Dealer not responding to calls.",
        "Want to switch to quarterly payment plan.",
        "Request to reschedule this month's payment."
    };

    static async Task Main(string[] args)
    {
        var connectionString = args.Length > 0
            ? args[0]
            : "Server=localhost;Database=DBZaura;User Id=sa;Password=Pakistan@786;Encrypt=Mandatory;TrustServerCertificate=true;";

        var options = new DbContextOptionsBuilder<PMSDbContext>().UseSqlServer(connectionString).Options;
        using var context = new PMSDbContext(options);

        try
        {
            Console.WriteLine("Connecting...");
            await context.Database.CanConnectAsync();
            Console.WriteLine("Connected.\n");

            var existingIds = await context.Tickets.Select(t => t.TicketID).ToListAsync();
            var users = await context.Users
                .Where(u => u.IsActive && u.FullName != null && u.FullName != "")
                .Select(u => u.FullName!)
                .ToListAsync();
            if (users.Count == 0) users.Add("CRO");
            var random = new Random();
            int added = 0;
            const int total = 200;

            for (int i = 0; i < total; i++)
            {
                string id = Guid.NewGuid().ToString("N")[..10].ToUpper();
                while (existingIds.Contains(id))
                {
                    id = Guid.NewGuid().ToString("N")[..10].ToUpper();
                }
                existingIds.Add(id);

                string status = Statuses[i % Statuses.Length];
                string createdBy = users[random.Next(users.Count)];
                string? assignedTo = (status == "Assigned" || status == "Ongoing") ? users[random.Next(users.Count)] : null;
                bool isClosed = (status == "Resolved" || status == "Discarded" || status == "Duplicate");
                DateTime created = DateTime.Now.AddDays(-random.Next(1, 180));
                DateTime? closingDate = isClosed ? created.AddDays(random.Next(1, 30)) : (DateTime?)null;

                var ticket = new Ticket
                {
                    TicketID = id,
                    CustomerID = $"CUST{random.Next(1000, 99999)}",
                    Email = $"customer{id.ToLower()}@example.com",
                    Contact = $"03{random.Next(10, 99)}-{random.Next(1000000, 9999999)}",
                    Title = Titles[random.Next(Titles.Length)] + " #" + (i + 1),
                    Description = Descriptions[random.Next(Descriptions.Length)],
                    CROComments = random.Next(3) == 0 ? $"Follow-up note for ticket {id}." : null,
                    Status = status,
                    CreatedBy = createdBy,
                    AssignedTo = assignedTo,
                    TicketClosingDate = closingDate,
                    CreatedAt = created
                };
                context.Tickets.Add(ticket);
                added++;
                if (added % 50 == 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($"  Added {added}/{total} tickets...");
                }
            }
            if (added % 50 != 0) await context.SaveChangesAsync();
            Console.WriteLine($"\nDone. Created {added} tickets with statuses: {string.Join(", ", Statuses)}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
