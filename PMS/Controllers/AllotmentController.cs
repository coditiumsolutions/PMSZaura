using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Data;
using PMS.Models;
using PMS.Services;
using System.Security.Claims;
using System.Text.Json;

namespace PMS.Controllers
{
    [Authorize]
    public class AllotmentController : Controller
    {
        private const string ModuleKey = "Allotment";
        private const string WorkflowConfigKey = "allotmentworkflow";
        private const string PropertyStatusAvailable = "Available";
        private const string PropertyStatusReserved = "Reserved";
        private const string PropertyStatusAllotted = "Allotted";
        private readonly PMSDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IModulePermissionService _modulePermission;

        public AllotmentController(PMSDbContext context, IWebHostEnvironment environment, IModulePermissionService modulePermission)
        {
            _context = context;
            _environment = environment;
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

        private async Task<List<string>> GetWorkflowStatusesAsync()
        {
            var config = await _context.Configurations.FirstOrDefaultAsync(c => c.ConfigKey == WorkflowConfigKey);
            return config?.ConfigValue != null
                ? config.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string> { "Initiated", "Operations Desk", "Approved", "Declined" };
        }

        private static string ResolveWorkflowStatus(IEnumerable<string> statuses, string fallback)
        {
            return statuses.FirstOrDefault(s => string.Equals(s, fallback, StringComparison.OrdinalIgnoreCase)) ?? fallback;
        }

        private static int GetWorkflowIndex(List<string> statuses, string? currentStatus)
        {
            if (statuses.Count == 0) return -1;
            if (string.IsNullOrWhiteSpace(currentStatus)) return -1;
            for (var i = 0; i < statuses.Count; i++)
            {
                if (string.Equals(statuses[i], currentStatus, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Allotment workflow: linear pipeline (e.g. Initiated → Operations Desk), then a single decision:
        /// Approved or Declined (mutually exclusive terminals). Approved is locked.
        /// </summary>
        private async Task<(List<string> pipeline, string approved, string declined)> GetAllotmentWorkflowPartsAsync()
        {
            var statuses = await GetWorkflowStatusesAsync();
            var approved = ResolveWorkflowStatus(statuses, "Approved");
            var declined = ResolveWorkflowStatus(statuses, "Declined");
            var pipeline = statuses
                .Where(s => !string.Equals(s, approved, StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(s, declined, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (pipeline.Count == 0)
            {
                pipeline = new List<string>
                {
                    ResolveWorkflowStatus(statuses, "Initiated"),
                    ResolveWorkflowStatus(statuses, "Operations Desk")
                };
            }

            return (pipeline, approved, declined);
        }

        private async Task SetWorkflowStatusesViewBagAsync()
        {
            var statuses = await GetWorkflowStatusesAsync();
            ViewBag.WorkflowStatuses = statuses;
            ViewBag.InitiatedStatus = ResolveWorkflowStatus(statuses, "Initiated");
            ViewBag.OperationsDeskStatus = ResolveWorkflowStatus(statuses, "Operations Desk");
            ViewBag.ApprovedStatus = ResolveWorkflowStatus(statuses, "Approved");
            ViewBag.DeclinedStatus = ResolveWorkflowStatus(statuses, "Declined");
        }

        private async Task ApplyAllotmentSideEffectsAsync(Allotment allotment, string newStatus)
        {
            if (allotment.PropertyID == null) return;

            var statuses = await GetWorkflowStatusesAsync();
            var approved = ResolveWorkflowStatus(statuses, "Approved");
            var declined = ResolveWorkflowStatus(statuses, "Declined");

            var property = await _context.Properties.FirstOrDefaultAsync(p => p.PropertyID == allotment.PropertyID);
            if (property == null) return;

            if (string.Equals(newStatus, approved, StringComparison.OrdinalIgnoreCase))
            {
                property.Status = PropertyStatusAllotted;
                allotment.ApprovedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? allotment.ApprovedBy;
            }
            else if (string.Equals(newStatus, declined, StringComparison.OrdinalIgnoreCase))
            {
                // Release property back to pool so a new allotment can be initiated.
                property.Status = PropertyStatusAvailable;
            }
            else
            {
                // In-progress workflow: keep reserved if it isn't already allotted.
                if (!string.Equals(property.Status, PropertyStatusAllotted, StringComparison.OrdinalIgnoreCase))
                    property.Status = PropertyStatusReserved;
            }
        }

        // GET: Allotment/Index (Main page with grid and charts)
        public async Task<IActionResult> Index(string projectFilter = "All", string statusFilter = "All", string searchTerm = "")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            await SetWorkflowStatusesViewBagAsync();
            var workflowStatuses = ViewBag.WorkflowStatuses as List<string> ?? new List<string> { "Initiated", "Operations Desk", "Approved", "Declined" };
            var initiatedStatus = ResolveWorkflowStatus(workflowStatuses, "Initiated");
            var operationsDeskStatus = ResolveWorkflowStatus(workflowStatuses, "Operations Desk");
            var approvedStatus = ResolveWorkflowStatus(workflowStatuses, "Approved");
            var declinedStatus = ResolveWorkflowStatus(workflowStatuses, "Declined");

            // Get all allotments with related data
            var allotmentsQuery = _context.Allotments
                .Include(a => a.Customer)
                .Include(a => a.Property)
                .ThenInclude(p => p.Project)
                .Include(a => a.AllottedByUser)
                .AsQueryable();

            // Apply filters
            if (projectFilter != "All" && !string.IsNullOrEmpty(projectFilter))
            {
                allotmentsQuery = allotmentsQuery.Where(a => a.Property.ProjectID == projectFilter);
            }

            if (statusFilter != "All" && !string.IsNullOrEmpty(statusFilter))
            {
                allotmentsQuery = allotmentsQuery.Where(a => a.WorkFlowStatus == statusFilter);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                allotmentsQuery = allotmentsQuery.Where(a => 
                    a.AllotmentID.Contains(searchTerm) ||
                    a.CustomerID.Contains(searchTerm) ||
                    a.PropertyID.Contains(searchTerm) ||
                    a.Customer.FullName.Contains(searchTerm) ||
                    a.Property.PlotNo.Contains(searchTerm)
                );
            }

            var allotments = await allotmentsQuery
                .OrderByDescending(a => a.AllotmentDate)
                .ToListAsync();

            // Get chart data - Allotted vs Not Allotted per Project
            var projects = await _context.Projects.ToListAsync();
            var chartData = new List<object>();

            foreach (var project in projects)
            {
                var totalProperties = await _context.Properties
                    .Where(p => p.ProjectID == project.ProjectID)
                    .CountAsync();

                var allottedProperties = await _context.Properties
                    .Where(p => p.ProjectID == project.ProjectID && p.Status == "Allotted")
                    .CountAsync();

                var notAllotted = totalProperties - allottedProperties;

                chartData.Add(new
                {
                    projectName = project.ProjectName,
                    allotted = allottedProperties,
                    notAllotted = notAllotted,
                    total = totalProperties
                });
            }

            // Pass data to ViewBag
            ViewBag.Projects = projects;
            ViewBag.ChartData = JsonSerializer.Serialize(chartData);
            ViewBag.ProjectFilter = projectFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SearchTerm = searchTerm;

            // Statistics
            ViewBag.TotalAllotments = await _context.Allotments.CountAsync();
            ViewBag.InitiatedCount = await _context.Allotments.CountAsync(a => a.WorkFlowStatus == initiatedStatus);
            ViewBag.OperationsDeskCount = await _context.Allotments.CountAsync(a => a.WorkFlowStatus == operationsDeskStatus);
            ViewBag.Approved = await _context.Allotments.CountAsync(a => a.WorkFlowStatus == approvedStatus);
            ViewBag.Declined = await _context.Allotments.CountAsync(a => a.WorkFlowStatus == declinedStatus);
            ViewBag.TotalProperties = await _context.Properties.CountAsync();
            ViewBag.AllottedProperties = await _context.Properties.Where(p => p.Status == PropertyStatusAllotted).CountAsync();
            ViewBag.AvailableProperties = await _context.Properties.Where(p => p.Status == PropertyStatusAvailable).CountAsync();

            return View(allotments);
        }

        // GET: Allotment/Create
        public async Task<IActionResult> Create()
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            return View();
        }

        // POST: Search Customer by CustomerID
        [HttpPost]
        public async Task<IActionResult> SearchCustomer(string customerID)
        {
            if (string.IsNullOrEmpty(customerID))
            {
                return Json(new { success = false, message = "Please enter a Customer ID" });
            }

            var customer = await _context.Customers
                .Include(c => c.PaymentPlan)
                .ThenInclude(p => p.Project)
                .Include(c => c.Registration)
                .Include(c => c.Allotments)
                .FirstOrDefaultAsync(c => c.CustomerID == customerID);

            if (customer == null)
            {
                return Json(new { success = false, message = "Customer not found with ID: " + customerID });
            }

            // Check if customer is Active
            if (customer.Status != "Active")
            {
                return Json(new { 
                    success = false, 
                    message = $"Customer is not Active. Current Status: {customer.Status}" 
                });
            }

            // Check if customer already has an allotment
            if (customer.Allotments != null && customer.Allotments.Any(a => !string.Equals(a.WorkFlowStatus, "Declined", StringComparison.OrdinalIgnoreCase)))
            {
                var existingAllotment = customer.Allotments.FirstOrDefault();
                return Json(new { 
                    success = false, 
                    message = $"Customer already has a property allotted (Allotment ID: {existingAllotment?.AllotmentID}). One customer can only have ONE property." 
                });
            }

            // Check if customer has a payment plan
            if (customer.PlanID == null)
            {
                return Json(new { 
                    success = false, 
                    message = "Customer does not have a Payment Plan assigned" 
                });
            }

            // Return customer data
            return Json(new
            {
                success = true,
                customer = new
                {
                    customerID = customer.CustomerID,
                    fullName = customer.FullName,
                    cnic = customer.CNIC,
                    phone = customer.Phone,
                    email = customer.Email,
                    status = customer.Status,
                    registeredSize = customer.RegisteredSize,
                    subProject = customer.SubProject,
                    planName = customer.PaymentPlan?.PlanName,
                    projectID = customer.PaymentPlan?.ProjectID,
                    projectName = customer.PaymentPlan?.Project?.ProjectName
                }
            });
        }

        // POST: Get Available Properties based on Customer's Plan and Size
        [HttpPost]
        public async Task<IActionResult> GetAvailableProperties(string customerID)
        {
            var customer = await _context.Customers
                .Include(c => c.PaymentPlan)
                .FirstOrDefaultAsync(c => c.CustomerID == customerID);

            if (customer == null || customer.PaymentPlan == null)
            {
                return Json(new { success = false, message = "Customer or Payment Plan not found" });
            }

            var projectID = customer.PaymentPlan.ProjectID;
            var registeredSize = customer.RegisteredSize;

            // Get available properties matching customer's project and size
            var properties = await _context.Properties
                .Where(p => p.Status == "Available" 
                    && p.ProjectID == projectID 
                    && p.Size == registeredSize)
                .OrderBy(p => p.PlotNo)
                .Select(p => new
                {
                    propertyID = p.PropertyID,
                    plotNo = p.PlotNo,
                    block = p.Block,
                    size = p.Size,
                    propertyType = p.PropertyType,
                    street = p.Street
                })
                .ToListAsync();

            if (!properties.Any())
            {
                return Json(new { 
                    success = false, 
                    message = $"No available properties found matching Size: {registeredSize} in Project: {customer.PaymentPlan.Project?.ProjectName ?? projectID}" 
                });
            }

            return Json(new { success = true, properties = properties });
        }

        // POST: Create Allotment (AJAX endpoint)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateAllotmentFromCustomer(string customerID, string propertyID, 
            string allotmentType, string comments)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(customerID) || string.IsNullOrEmpty(propertyID))
                {
                    return Json(new { success = false, message = "Customer ID and Property ID are required" });
                }

                // Check if customer exists and is active
                var customer = await _context.Customers
                    .Include(c => c.Allotments)
                    .FirstOrDefaultAsync(c => c.CustomerID == customerID);

                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found" });
                }

                if (customer.Status != "Active")
                {
                    return Json(new { success = false, message = "Customer is not Active" });
                }

                // Check if customer already has an allotment
                if (customer.Allotments != null && customer.Allotments.Any(a => !string.Equals(a.WorkFlowStatus, "Declined", StringComparison.OrdinalIgnoreCase)))
                {
                    return Json(new { success = false, message = "Customer already has a property allotted. One customer can only have ONE property." });
                }

                // Check if property exists and is available
                var property = await _context.Properties
                    .Include(p => p.Allotments)
                    .FirstOrDefaultAsync(p => p.PropertyID == propertyID);

                if (property == null)
                {
                    return Json(new { success = false, message = "Property not found" });
                }

                if (property.Status != "Available")
                {
                    return Json(new { success = false, message = "Property is not available for allotment" });
                }

                // Check if property already has an allotment (double check)
                if (property.Allotments != null && property.Allotments.Any(a => !string.Equals(a.WorkFlowStatus, "Declined", StringComparison.OrdinalIgnoreCase)))
                {
                    return Json(new { success = false, message = "Property is already allotted to another customer" });
                }

                // Generate a new unique Allotment ID
                var allAllotmentIds = await _context.Allotments
                    .Select(a => a.AllotmentID)
                    .ToListAsync();

                int nextId = 1;
                if (allAllotmentIds.Any())
                {
                    int maxId = 0;
                    foreach (var id in allAllotmentIds)
                    {
                        if (int.TryParse(id, out int numericId))
                        {
                            if (numericId > maxId) maxId = numericId;
                        }
                        else if (id.StartsWith("ALLOT") && int.TryParse(id.AsSpan(5), out int prefixedNumericId))
                        {
                            if (prefixedNumericId > maxId) maxId = prefixedNumericId;
                        }
                    }
                    nextId = maxId + 1;
                }
                string allotmentID = nextId.ToString();

                // Get current user ID
                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "ADMIN";

                // Validate AllottedBy is not empty
                if (string.IsNullOrEmpty(userID))
                {
                    return Json(new { success = false, message = "User ID is required. Please ensure you are logged in." });
                }

                var workflowStatuses = await GetWorkflowStatusesAsync();
                var initiatedStatus = ResolveWorkflowStatus(workflowStatuses, "Initiated");

                // Create Allotment
                var allotment = new Allotment
                {
                    AllotmentID = allotmentID,
                    PropertyID = propertyID,
                    CustomerID = customerID,
                    AllottedBy = userID,
                    AllotmentDate = DateTime.Now,
                    AllottmentType = allotmentType ?? "Regular",
                    WorkFlowStatus = initiatedStatus,
                    Comments = comments
                };

                // Validate model
                if (!TryValidateModel(allotment))
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Validation failed: " + string.Join(", ", errors) });
                }

                _context.Allotments.Add(allotment);

                // Reserve the property until workflow is approved/declined.
                property.Status = PropertyStatusReserved;

                // Log activity
                var log = new ActivityLog
                {
                    UserID = userID,
                    Action = $"Property {propertyID} allotted to Customer {customerID}",
                    RefType = "Allotment",
                    RefID = allotmentID,
                    CreatedAt = DateTime.Now
                };
                _context.ActivityLogs.Add(log);

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Allotment initiated. Allotment ID: {allotmentID}",
                    allotmentID = allotmentID
                });
            }
            catch (DbUpdateException dbEx)
            {
                return Json(new { 
                    success = false, 
                    message = "Database error: " + dbEx.Message,
                    innerException = dbEx.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = "Error creating allotment: " + ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // POST: Create Allotment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAllotment(string customerID, string propertyID, 
            string allotmentType, string comments, IFormFile? attachment)
        {
            try
            {
                var denied = await EnsurePermissionAsync("Edit");
                if (denied != null) return denied;

                // Validate inputs
                if (string.IsNullOrEmpty(customerID) || string.IsNullOrEmpty(propertyID))
                {
                    TempData["Error"] = "Customer ID and Property ID are required";
                    return RedirectToAction(nameof(Create));
                }

                // Check if customer exists and is active
                var customer = await _context.Customers
                    .Include(c => c.Allotments)
                    .FirstOrDefaultAsync(c => c.CustomerID == customerID);

                if (customer == null)
                {
                    TempData["Error"] = "Customer not found";
                    return RedirectToAction(nameof(Create));
                }

                if (customer.Status != "Active")
                {
                    TempData["Error"] = "Customer is not Active";
                    return RedirectToAction(nameof(Create));
                }

                // Check if customer already has an allotment
                if (customer.Allotments != null && customer.Allotments.Any(a => !string.Equals(a.WorkFlowStatus, "Declined", StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["Error"] = "Customer already has a property allotted. One customer can only have ONE property.";
                    return RedirectToAction(nameof(Create));
                }

                // Check if property exists and is available
                var property = await _context.Properties
                    .Include(p => p.Allotments)
                    .FirstOrDefaultAsync(p => p.PropertyID == propertyID);

                if (property == null)
                {
                    TempData["Error"] = "Property not found";
                    return RedirectToAction(nameof(Create));
                }

                if (property.Status != "Available")
                {
                    TempData["Error"] = "Property is not available for allotment";
                    return RedirectToAction(nameof(Create));
                }

                // Check if property already has an allotment (double check)
                if (property.Allotments != null && property.Allotments.Any(a => !string.Equals(a.WorkFlowStatus, "Declined", StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["Error"] = "Property is already allotted to another customer";
                    return RedirectToAction(nameof(Create));
                }

                // Generate a new unique Allotment ID
                var allAllotmentIds = await _context.Allotments
                    .Select(a => a.AllotmentID)
                    .ToListAsync();

                int nextId = 1;
                if (allAllotmentIds.Any())
                {
                    int maxId = 0;
                    foreach (var id in allAllotmentIds)
                    {
                        if (int.TryParse(id, out int numericId))
                        {
                            if (numericId > maxId) maxId = numericId;
                        }
                        else if (id.StartsWith("ALLOT") && int.TryParse(id.AsSpan(5), out int prefixedNumericId))
                        {
                            if (prefixedNumericId > maxId) maxId = prefixedNumericId;
                        }
                    }
                    nextId = maxId + 1;
                }
                string allotmentID = nextId.ToString();

                // Get current user ID
                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "ADMIN";

                var workflowStatuses = await GetWorkflowStatusesAsync();
                var initiatedStatus = ResolveWorkflowStatus(workflowStatuses, "Initiated");

                // Create Allotment
                var allotment = new Allotment
                {
                    AllotmentID = allotmentID,
                    PropertyID = propertyID,
                    CustomerID = customerID,
                    AllottedBy = userID,
                    AllotmentDate = DateTime.Now,
                    AllottmentType = allotmentType ?? "Regular",
                    WorkFlowStatus = initiatedStatus,
                    Comments = comments
                };

                if (string.IsNullOrWhiteSpace(allotment.AllottmentType)) allotment.AllottmentType = "Regular";
                if (string.IsNullOrWhiteSpace(allotment.WorkFlowStatus)) allotment.WorkFlowStatus = initiatedStatus;

                _context.Allotments.Add(allotment);

                // Reserve property until approved/declined.
                property.Status = PropertyStatusReserved;

                // Handle file upload if attachment provided
                if (attachment != null && attachment.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "allotments");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{allotmentID}_{Path.GetFileName(attachment.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachment.CopyToAsync(fileStream);
                    }

                    // Save attachment record
                    var lastAttachment = await _context.Attachments
                        .OrderByDescending(a => a.AttachmentID)
                        .FirstOrDefaultAsync();
                    
                    int nextAttachId = 1;
                    if (lastAttachment != null && lastAttachment.AttachmentID.Length > 3)
                    {
                        int.TryParse(lastAttachment.AttachmentID.Substring(3), out nextAttachId);
                        nextAttachId++;
                    }
                    string attachmentID = "ATT" + nextAttachId.ToString("D7");

                    var attachmentRecord = new Attachment
                    {
                        AttachmentID = attachmentID,
                        RefType = "Allotment",
                        RefID = allotmentID,
                        FilePath = $"/uploads/allotments/{fileName}",
                        UploadedBy = userID,
                        UploadedAt = DateTime.Now
                    };

                    _context.Attachments.Add(attachmentRecord);
                }

                // Log activity
                var log = new ActivityLog
                {
                    UserID = userID,
                    Action = $"Allotment {allotmentID} initiated (reserved): property {propertyID} for customer {customerID}",
                    RefType = "Allotment",
                    RefID = allotmentID,
                    CreatedAt = DateTime.Now
                };
                _context.ActivityLogs.Add(log);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    $"Allotment request {allotmentID} submitted as \"{initiatedStatus}\". The plot is reserved until Operations approves ({ResolveWorkflowStatus(workflowStatuses, "Approved")}) or declines.";
                return RedirectToAction(nameof(Edit), new { id = allotmentID });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error creating allotment: " + ex.Message;
                return RedirectToAction(nameof(Create));
            }
        }

        // GET: Allotment/Details
        public async Task<IActionResult> Details(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var allotment = await _context.Allotments
                .Include(a => a.Customer)
                    .ThenInclude(c => c.PaymentPlan)
                        .ThenInclude(p => p.Project)
                .Include(a => a.Property)
                    .ThenInclude(p => p.Project)
                .Include(a => a.AllottedByUser)
                .FirstOrDefaultAsync(a => a.AllotmentID == id);

            if (allotment == null)
            {
                return NotFound();
            }

            ViewBag.Attachments = await _context.Attachments.AsNoTracking()
                .Where(a => a.RefType == "Allotment" && a.RefID == id)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();

            await SetWorkflowStatusesViewBagAsync();
            return View(allotment);
        }

        // GET: Allotment/Edit (workflow controls)
        public async Task<IActionResult> Edit(string id)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrEmpty(id)) return NotFound();

            var allotment = await _context.Allotments
                .Include(a => a.Customer)
                .Include(a => a.Property)
                    .ThenInclude(p => p.Project)
                .Include(a => a.AllottedByUser)
                .FirstOrDefaultAsync(a => a.AllotmentID == id);
            if (allotment == null) return NotFound();

            await SetWorkflowStatusesViewBagAsync();
            var (pipeline, approvedStatus, declinedStatus) = await GetAllotmentWorkflowPartsAsync();
            var wf = allotment.WorkFlowStatus ?? pipeline[0];
            var isApproved = string.Equals(wf, approvedStatus, StringComparison.OrdinalIgnoreCase);
            var isDeclined = string.Equals(wf, declinedStatus, StringComparison.OrdinalIgnoreCase);
            var pIdx = GetWorkflowIndex(pipeline, wf);

            ViewBag.PipelineStatuses = pipeline;
            ViewBag.IsAllotmentApproved = isApproved;
            ViewBag.IsAllotmentDeclined = isDeclined;
            var inPipeline = !isApproved && !isDeclined && pIdx >= 0;
            ViewBag.HasPipelinePrevious = inPipeline && pIdx > 0;
            ViewBag.HasPipelineNext = inPipeline && pIdx < pipeline.Count - 1;
            ViewBag.AtPipelineDecisionPoint = inPipeline && pIdx == pipeline.Count - 1;
            if (!isApproved && !isDeclined && pIdx < 0)
                ViewBag.WorkflowStatusWarning = "Current status does not match the configured pipeline. Use Move Back/Forward only after status is corrected in data.";
            return View(allotment);
        }

        // POST: Allotment/MoveStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveStatus(string id, string direction)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrEmpty(id)) return NotFound();

            var allotment = await _context.Allotments
                .Include(a => a.Property)
                .FirstOrDefaultAsync(a => a.AllotmentID == id);
            if (allotment == null) return NotFound();

            var (pipeline, approvedStatus, declinedStatus) = await GetAllotmentWorkflowPartsAsync();
            var wf = allotment.WorkFlowStatus ?? pipeline[0];

            if (string.Equals(wf, approvedStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "This allotment is approved and locked. It cannot be moved or declined.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            if (string.Equals(wf, declinedStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "This allotment was declined. Workflow cannot be changed.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var idx = GetWorkflowIndex(pipeline, wf);
            if (idx < 0)
            {
                allotment.WorkFlowStatus = pipeline[0];
                await _context.SaveChangesAsync();
                TempData["Error"] = "Status was reset to the first pipeline step.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            if (idx >= pipeline.Count - 1 && string.Equals(direction, "forward", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Use Approve or Decline from this step—not Move Forward.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var newIdx = idx;
            if (string.Equals(direction, "forward", StringComparison.OrdinalIgnoreCase))
                newIdx = Math.Min(pipeline.Count - 1, idx + 1);
            else if (string.Equals(direction, "backward", StringComparison.OrdinalIgnoreCase))
            {
                if (idx <= 0)
                {
                    TempData["Error"] = "Already at the first workflow step.";
                    return RedirectToAction(nameof(Edit), new { id });
                }
                newIdx = idx - 1;
            }
            else
            {
                return RedirectToAction(nameof(Edit), new { id });
            }

            var newStatus = pipeline[newIdx];
            if (!string.Equals(allotment.WorkFlowStatus, newStatus, StringComparison.OrdinalIgnoreCase))
            {
                allotment.WorkFlowStatus = newStatus;
                await ApplyAllotmentSideEffectsAsync(allotment, newStatus);

                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "ADMIN";
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userID,
                    Action = $"Allotment {id} moved to status '{newStatus}'",
                    RefType = "Allotment",
                    RefID = id,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Workflow updated: {newStatus}";
            }

            return RedirectToAction(nameof(Edit), new { id });
        }

        /// <summary>Terminal outcome from the last pipeline step only: Approved or Declined (not sequential).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAllotmentOutcome(string id, string outcome)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            if (string.IsNullOrEmpty(id)) return NotFound();

            var allotment = await _context.Allotments
                .Include(a => a.Property)
                .FirstOrDefaultAsync(a => a.AllotmentID == id);
            if (allotment == null) return NotFound();

            var (pipeline, approvedStatus, declinedStatus) = await GetAllotmentWorkflowPartsAsync();
            var wf = allotment.WorkFlowStatus ?? pipeline[0];

            if (string.Equals(wf, approvedStatus, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wf, declinedStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "This allotment is already complete.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var idx = GetWorkflowIndex(pipeline, wf);
            if (idx != pipeline.Count - 1)
            {
                TempData["Error"] = "Approve or Decline is only available at the final review step.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            string? newStatus = null;
            if (string.Equals(outcome, "approve", StringComparison.OrdinalIgnoreCase))
                newStatus = approvedStatus;
            else if (string.Equals(outcome, "decline", StringComparison.OrdinalIgnoreCase))
                newStatus = declinedStatus;

            if (newStatus == null)
            {
                TempData["Error"] = "Invalid outcome.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            allotment.WorkFlowStatus = newStatus;
            await ApplyAllotmentSideEffectsAsync(allotment, newStatus);

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "ADMIN";
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserID = userID,
                Action = $"Allotment {id} outcome set to '{newStatus}'",
                RefType = "Allotment",
                RefID = id,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Workflow complete: {newStatus}";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // GET: Allotment/UnAllot?allotmentId=&customerId=
        public async Task<IActionResult> UnAllot(string? allotmentId = null, string? customerId = null)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;

            var allotmentIdTrimmed = allotmentId?.Trim();
            var customerIdTrimmed = customerId?.Trim();

            var query = _context.Allotments
                .Include(a => a.Customer)
                .Include(a => a.Property)
                .ThenInclude(p => p!.Project)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(allotmentIdTrimmed))
            {
                query = query.Where(a => a.AllotmentID == allotmentIdTrimmed);
            }
            else if (!string.IsNullOrWhiteSpace(customerIdTrimmed))
            {
                query = query.Where(a => a.CustomerID == customerIdTrimmed);
            }

            var allotments = await query
                .OrderByDescending(a => a.AllotmentDate)
                .ToListAsync();

            ViewBag.FilteredAllotmentId = allotmentIdTrimmed;
            ViewBag.FilteredCustomerId = customerIdTrimmed
                ?? allotments.FirstOrDefault()?.CustomerID?.Trim();

            return View(allotments);
        }

        // POST: Allotment/ProcessUnAllot
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessUnAllot(string allotmentID, string reason, string? returnCustomerId = null)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            try
            {
                if (string.IsNullOrEmpty(allotmentID))
                {
                    TempData["Error"] = "Allotment ID is required";
                    return RedirectToAction(nameof(UnAllot));
                }

                var allotment = await _context.Allotments
                    .Include(a => a.Property)
                    .FirstOrDefaultAsync(a => a.AllotmentID == allotmentID);

                if (allotment == null)
                {
                    TempData["Error"] = "Allotment not found";
                    return RedirectToAction(nameof(UnAllot));
                }

                var propertyID = allotment.PropertyID;
                var customerID = allotment.CustomerID;
                var redirectCustomerId = !string.IsNullOrWhiteSpace(returnCustomerId)
                    ? returnCustomerId.Trim()
                    : customerID?.Trim();

                // Remove allotment
                _context.Allotments.Remove(allotment);

                // Update property status back to Available
                if (allotment.Property != null)
                {
                    allotment.Property.Status = "Available";
                }

                // Log activity
                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "ADMIN";
                var log = new ActivityLog
                {
                    UserID = userID,
                    Action = $"Allotment {allotmentID} cancelled. Property {propertyID} un-allotted from Customer {customerID}. Reason: {reason ?? "Not specified"}",
                    RefType = "Allotment",
                    RefID = allotmentID,
                    CreatedAt = DateTime.Now
                };
                _context.ActivityLogs.Add(log);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Allotment successfully cancelled. Property is now available.";
                if (!string.IsNullOrWhiteSpace(redirectCustomerId))
                {
                    return RedirectToAction("Details", "Customer", new { id = redirectCustomerId });
                }

                return RedirectToAction(nameof(UnAllot));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error cancelling allotment: " + ex.Message;
                return RedirectToAction(nameof(UnAllot));
            }
        }
    }
}

