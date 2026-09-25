using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;
using PMS.Services;
using System.Security.Claims;

namespace PMS.Controllers
{
    [Authorize]
    public class SalesInquiryController : Controller
    {
        private const string ModuleKey = "SalesInquiry";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;

        public SalesInquiryController(PMSDbContext context, IModulePermissionService modulePermission)
        {
            _context = context;
            _modulePermission = modulePermission;
        }

        private async Task<IActionResult?> EnsurePermissionAsync(string requiredLevel)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var perm = await _modulePermission.GetPermissionAsync(userId, ModuleKey);
            if (requiredLevel == "Read" && !_modulePermission.CanRead(perm))
                return RedirectToAction("AccessDenied", "Account");
            if (requiredLevel == "Edit" && !_modulePermission.CanEdit(perm))
                return RedirectToAction("AccessDenied", "Account");
            if (requiredLevel == "Admin" && !_modulePermission.CanDelete(perm))
                return RedirectToAction("AccessDenied", "Account");
            ViewBag.CanCreate = _modulePermission.CanEdit(perm);
            ViewBag.CanEdit = _modulePermission.CanEdit(perm);
            ViewBag.CanDelete = _modulePermission.CanDelete(perm);
            return null;
        }

        // GET: Sales Inquiries List
        public async Task<IActionResult> Index(string status = "All")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            ViewBag.SelectedStatus = status;

            var query = _context.PropertyInquiries.AsQueryable();

            if (status != "All")
            {
                query = query.Where(i => i.Status == status);
            }

            var inquiries = await query
                .OrderByDescending(i => i.SubmittedAt)
                .ToListAsync();

            // Statistics
            ViewBag.TotalInquiries = await _context.PropertyInquiries.CountAsync();
            ViewBag.NewInquiries = await _context.PropertyInquiries.CountAsync(i => i.Status == "New");
            ViewBag.ContactedInquiries = await _context.PropertyInquiries.CountAsync(i => i.IsContacted);
            ViewBag.ConvertedInquiries = await _context.PropertyInquiries.CountAsync(i => i.Status == "Converted");

            // Active users for Assign To dropdown (FullName)
            ViewBag.Users = await _context.Users
                .Where(u => u.IsActive && u.FullName != null && u.FullName != "")
                .OrderBy(u => u.FullName)
                .Select(u => u.FullName!)
                .ToListAsync();

            return View(inquiries);
        }

        // POST: Update Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int inquiryId, string status)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            var inquiry = await _context.PropertyInquiries.FindAsync(inquiryId);
            
            if (inquiry == null)
            {
                return NotFound();
            }

            inquiry.Status = status;
            
            if (status == "Contacted" || status == "Follow-up")
            {
                inquiry.IsContacted = true;
            }

            await _context.SaveChangesAsync();

            // Log activity
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, $"Updated Inquiry Status to {status}", "PropertyInquiry", inquiryId.ToString());
            }

            TempData["Success"] = "Status updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Mark as Contacted
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsContacted(int inquiryId, string method)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            var inquiry = await _context.PropertyInquiries.FindAsync(inquiryId);
            
            if (inquiry == null)
            {
                return NotFound();
            }

            inquiry.IsContacted = true;
            inquiry.Status = "Contacted";
            
            if (string.IsNullOrEmpty(inquiry.Notes))
            {
                inquiry.Notes = $"Contacted via {method} on {DateTime.Now:MMM dd, yyyy hh:mm tt}";
            }
            else
            {
                inquiry.Notes += $"\n\nContacted via {method} on {DateTime.Now:MMM dd, yyyy hh:mm tt}";
            }

            await _context.SaveChangesAsync();

            // Log activity
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, $"Contacted customer via {method}", "PropertyInquiry", inquiryId.ToString());
            }

            TempData["Success"] = $"Marked as contacted via {method}!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Add Notes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNotes(int inquiryId, string notes)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            var inquiry = await _context.PropertyInquiries.FindAsync(inquiryId);
            
            if (inquiry == null)
            {
                return NotFound();
            }

            var timestamp = DateTime.Now.ToString("MMM dd, yyyy hh:mm tt");
            var userName = User.Identity?.Name ?? "Unknown";
            
            if (string.IsNullOrEmpty(inquiry.Notes))
            {
                inquiry.Notes = $"[{timestamp} - {userName}]\n{notes}";
            }
            else
            {
                inquiry.Notes += $"\n\n[{timestamp} - {userName}]\n{notes}";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Notes added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Assign To
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTo(int inquiryId, string assignedTo)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            var inquiry = await _context.PropertyInquiries.FindAsync(inquiryId);
            
            if (inquiry == null)
            {
                return NotFound();
            }

            inquiry.AssignedTo = assignedTo;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Inquiry assigned to {assignedTo}!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Set Follow-up Date
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetFollowUp(int inquiryId, DateTime followUpDate)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            var inquiry = await _context.PropertyInquiries.FindAsync(inquiryId);
            
            if (inquiry == null)
            {
                return NotFound();
            }

            inquiry.FollowUpDate = followUpDate;
            inquiry.Status = "Follow-up";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Follow-up date set successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Performance Report (sales agent stats)
        public async Task<IActionResult> PerformanceReport()
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var inquiries = await _context.PropertyInquiries.ToListAsync();
            var byAgent = inquiries
                .GroupBy(i => i.AssignedTo ?? "(Unassigned)")
                .Select(g => new SalesAgentPerformanceViewModel
                {
                    AgentName = g.Key,
                    Total = g.Count(),
                    New = g.Count(i => i.Status == "New"),
                    Contacted = g.Count(i => i.Status == "Contacted"),
                    FollowUp = g.Count(i => i.Status == "Follow-up"),
                    Converted = g.Count(i => i.Status == "Converted"),
                    Closed = g.Count(i => i.Status == "Closed")
                })
                .OrderByDescending(x => x.Total)
                .ToList();
            return View(byAgent);
        }

        // POST: Delete Inquiry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int inquiryId)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            var inquiry = await _context.PropertyInquiries.FindAsync(inquiryId);
            
            if (inquiry == null)
            {
                return NotFound();
            }

            _context.PropertyInquiries.Remove(inquiry);
            await _context.SaveChangesAsync();

            // Log activity
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "Deleted Property Inquiry", "PropertyInquiry", inquiryId.ToString());
            }

            TempData["Success"] = "Inquiry deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: New inquiry form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            return View();
        }

        // POST: Create new inquiry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string fullName, string phoneNumber, string? emailAddress, string? inquiryType, string? message)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                TempData["Error"] = "Full name and phone number are required.";
                ViewBag.FullName = fullName;
                ViewBag.PhoneNumber = phoneNumber;
                ViewBag.EmailAddress = emailAddress;
                ViewBag.InquiryType = inquiryType;
                ViewBag.Message = message;
                return View();
            }

            var inquiry = new PropertyInquiry
            {
                FullName = fullName.Trim(),
                PhoneNumber = phoneNumber.Trim(),
                EmailAddress = string.IsNullOrWhiteSpace(emailAddress) ? null : emailAddress.Trim(),
                InquiryType = string.IsNullOrWhiteSpace(inquiryType) ? null : inquiryType.Trim(),
                Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
                SubmittedAt = DateTime.Now,
                Status = "New",
                IsContacted = false,
                CreatedAt = DateTime.Now,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.PropertyInquiries.Add(inquiry);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await LogActivity(userId, "Created Property Inquiry", "PropertyInquiry", inquiry.InquiryID.ToString());
            }

            TempData["Success"] = "Inquiry created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Helper method to log activities
        private async Task LogActivity(string userId, string action, string refType, string refId)
        {
            var activityLog = new ActivityLog
            {
                UserID = userId,
                Action = action,
                RefType = refType,
                RefID = refId,
                CreatedAt = DateTime.Now
            };

            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }
    }
}

