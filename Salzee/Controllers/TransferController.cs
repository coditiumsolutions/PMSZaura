using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models;
using Salzee.Services;
using System.Linq;
using System.Text;

namespace Salzee.Controllers
{
    [Authorize]
    public class TransferController : Controller
    {
        private const string ModuleKey = "Transfer";
        private readonly PMSDbContext _context;
        private readonly IModulePermissionService _modulePermission;
        private const string WorkflowCreated = "Created";
        private const string WorkflowAtAccounts = "At the Desk of Accounts";
        private const string WorkflowAtTransferApproval = "At the Desk of Transfer Approval";
        private const string WorkflowApproved = "Approved";

        public TransferController(PMSDbContext context, IModulePermissionService modulePermission)
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

        private async Task SetDuplicateFileTransferNavLinkAsync()
        {
            var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var dupPerm = await _modulePermission.GetPermissionAsync(uid, "DuplicateFileTransfer");
            ViewBag.ShowDuplicateFileTransferLink = _modulePermission.CanRead(dupPerm);
        }

        private static string NormalizeId(string? id)
        {
            return (id ?? string.Empty).Trim();
        }

        private static string SafeRefId(string? refId)
        {
            var normalized = NormalizeId(refId);
            if (string.IsNullOrEmpty(normalized)) return string.Empty;
            return normalized.Length <= 10 ? normalized : normalized.Substring(0, 10);
        }

        private static string SanitizePathSegment(string? segment)
        {
            // Windows does not allow trailing spaces/dots in folder names and disallows certain characters.
            var s = NormalizeId(segment);
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }

            // Extra safety for path separators.
            s = s.Replace(Path.DirectorySeparatorChar, '_')
                 .Replace(Path.AltDirectorySeparatorChar, '_');

            // Windows also rejects trailing dots/spaces.
            s = s.Trim().TrimEnd('.');
            return s;
        }

        public async Task<IActionResult> Index(string customerIdFilter = "", string workflowFilter = "")
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            var query = _context.Transfers
                .Include(t => t.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(customerIdFilter))
                query = query.Where(t => t.CustomerID.Contains(customerIdFilter));

            if (!string.IsNullOrWhiteSpace(workflowFilter))
            {
                if (string.Equals(workflowFilter, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(t =>
                        !string.Equals(t.WorkFlowStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(t.WorkFlowStatus, "Declined", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    query = query.Where(t => t.WorkFlowStatus == workflowFilter);
                }
            }

            var list = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            ViewBag.CustomerIdFilter = customerIdFilter ?? "";
            ViewBag.WorkflowFilter = workflowFilter ?? "";
            ViewBag.WorkflowStatuses = new[] { WorkflowCreated, WorkflowAtAccounts, WorkflowAtTransferApproval, WorkflowApproved };
            await SetDuplicateFileTransferNavLinkAsync();
            return View(list);
        }

        public async Task<IActionResult> Create(string? customerId = null)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            await SetDuplicateFileTransferNavLinkAsync();
            ViewBag.WorkflowStatuses = new[] { WorkflowCreated };
            ViewBag.TransferTypes = await GetTransferTypesAsync();
            SetCitiesAndCountriesViewBag();
            SetNationalitiesViewBag();
            SetPaymentMethodsViewBag();
            SetBanksViewBag();
            var model = new Transfer
            {
                WorkFlowStatus = WorkflowCreated,
                CreatedAt = DateTime.Now,
                BuyerDOB = new DateTime(1990, 1, 1)
            };
            if (!string.IsNullOrWhiteSpace(customerId))
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer != null)
                {
                    model.CustomerID = customer.CustomerID;
                    model.SellerName = customer.FullName;
                    model.SellerFatherName = customer.FatherName;
                    model.SellerCNIC = customer.CNIC;
                    model.SellerContact = customer.Phone;
                    model.SellerAddress = customer.MailingAddress ?? customer.PermanentAddress;
                }
            }
            return View(model);
        }

        private void SetCitiesAndCountriesViewBag()
        {
            var citiesConfig = _context.Configurations.FirstOrDefault(c => c.ConfigKey == "cities");
            ViewBag.Cities = citiesConfig?.ConfigValue != null
                ? citiesConfig.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string>();
            var countriesConfig = _context.Configurations.FirstOrDefault(c => c.ConfigKey == "countries");
            ViewBag.Countries = countriesConfig?.ConfigValue != null
                ? countriesConfig.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string>();
        }

        private void SetNationalitiesViewBag()
        {
            var nationalitiesConfig = _context.Configurations.FirstOrDefault(c => c.ConfigKey == "nationalities");
            ViewBag.Nationalities = nationalitiesConfig?.ConfigValue != null
                ? nationalitiesConfig.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList()
                : new List<string> { "Pakistani", "American", "British", "Canadian", "Chinese", "Indian", "Other" };
        }

        private void SetPaymentMethodsViewBag()
        {
            var config = _context.Configurations.FirstOrDefault(c => c.ConfigKey == "paymentmethods" || c.ConfigKey == "PaymentMethods");
            ViewBag.PaymentMethods = config?.ConfigValue != null
                ? config.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string> { "DD/DS", "Cash", "Bank Transfer", "Cheque", "Online", "Mobile Money" };
        }

        /// <summary>Bank list from Configuration key "banks" (comma-separated), same as Payments module.</summary>
        private void SetBanksViewBag()
        {
            var banksConfig = _context.Configurations.FirstOrDefault(c => c.ConfigKey == "banks");
            ViewBag.BankNames = banksConfig?.ConfigValue != null
                ? banksConfig.ConfigValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                : new List<string>();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Transfer model)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            model.TransferID = await GenerateTransferIdAsync();
            model.WorkFlowStatus ??= WorkflowCreated;
            model.CreatedAt = DateTime.Now;

            if (!model.BuyerDOB.HasValue)
            {
                model.BuyerDOB = new DateTime(1990, 1, 1);
            }

            if (await _context.Customers.AllAsync(c => c.CustomerID != model.CustomerID))
            {
                ModelState.AddModelError("CustomerID", "Customer ID not found.");
            }
            else
            {
                var today = DateTime.Today;
                var hasActiveNDC = await _context.NDCs
                    .AnyAsync(n => n.CustomerID == model.CustomerID.Trim()
                        && n.NDCExpiryDate.HasValue
                        && n.IssuedDate.Date <= today
                        && n.NDCExpiryDate.Value.Date >= today);
                if (!hasActiveNDC)
                {
                    ModelState.AddModelError("CustomerID", "No active NDC found for this customer. Transfer requires an NDC where current date is between Issue Date and Expiry Date.");
                }
            }

            ValidateBuyerInformation(model);

            if (model.TransferFeeDue == null)
                ModelState.AddModelError("TransferFeeDue", "Transfer Fee Due is required.");
            if (model.TransferFeePaid == null)
                ModelState.AddModelError("TransferFeePaid", "Transfer Fee Paid is required.");
            if (!model.PaymentDate.HasValue)
                ModelState.AddModelError("PaymentDate", "Payment Date is required.");
            if (string.IsNullOrWhiteSpace(model.PaymentMode))
                ModelState.AddModelError("PaymentMode", "Payment Mode is required.");
            if (string.IsNullOrWhiteSpace(model.PaymentChallanNo))
                ModelState.AddModelError("PaymentChallanNo", "Challan No is required.");

            if (string.Equals(model.WorkFlowStatus, WorkflowApproved, StringComparison.OrdinalIgnoreCase))
            {
                var due = model.TransferFeeDue ?? 0;
                var paid = model.TransferFeePaid ?? 0;
                if (Math.Abs(due - paid) > 0.001)
                    ModelState.AddModelError("", "When Workflow Status is Approved, Transfer Fee Due must equal Transfer Fee Paid.");
            }

            if (ModelState.IsValid)
            {
                model.BuyerContact = model.BuyerPhone;
                model.BuyerAddress = model.BuyerMailingAddress;
                _context.Transfers.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Edit), new { id = model.TransferID, showAttachments = true });
            }

            ViewBag.WorkflowStatuses = new[] { WorkflowCreated };
            ViewBag.TransferTypes = await GetTransferTypesAsync();
            SetCitiesAndCountriesViewBag();
            SetNationalitiesViewBag();
            SetPaymentMethodsViewBag();
            SetBanksViewBag();
            return View(model);
        }

        private async Task<List<string>> GetTransferTypesAsync()
        {
            var config = await _context.Configurations.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConfigKey == "NDCType");
            const string duplicateFileTransfer = "Duplicate File Transfer";
            var types = (config?.ConfigValue ?? "Normal Transfer,Urgent Transfer,Family Transfer,Death Transfer")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Where(t => !string.Equals(t, duplicateFileTransfer, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return types;
        }

        private async Task<string?> ResolveCustomerIdAsync(string? customerId)
        {
            var normalized = NormalizeId(customerId);
            if (string.IsNullOrEmpty(normalized)) return null;
            var exact = await _context.Customers.AsNoTracking()
                .Where(c => c.CustomerID == normalized)
                .Select(c => c.CustomerID)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(exact)) return exact;
            return await _context.Customers.AsNoTracking()
                .Where(c => c.CustomerID != null && c.CustomerID.Trim() == normalized)
                .Select(c => c.CustomerID)
                .FirstOrDefaultAsync();
        }

        private async Task<decimal?> LookupTransferFeeAsync(string projectId, string? subProject, string transferType, string? transferPriority)
        {
            var pid = NormalizeId(projectId);
            var sub = NormalizeId(subProject ?? string.Empty);
            var tt = NormalizeId(transferType);
            if (string.IsNullOrEmpty(pid) || string.IsNullOrEmpty(tt))
                return null;

            // Use ToLower() for case-insensitive match — string.Equals(..., OrdinalIgnoreCase) is not reliably translatable to SQL and can cause 500s.
            var ttLower = tt.ToLowerInvariant();
            var query = _context.TransferFees.AsNoTracking()
                .Where(t => t.ProjectID.Trim() == pid
                    && (t.SubProject ?? "").Trim() == sub
                    && (t.TransferType ?? "").Trim().ToLower() == ttLower);

            if (!string.IsNullOrWhiteSpace(transferPriority))
            {
                var pLow = transferPriority.Trim().ToLowerInvariant();
                return await query
                    .Where(t => (t.TransferPriority ?? "").Trim().ToLower() == pLow)
                    .OrderByDescending(t => t.CreatedOn)
                    .Select(t => (decimal?)t.AmountPerUnit)
                    .FirstOrDefaultAsync();
            }

            // Prefer non-Urgent row when priority not specified (same ordering intent as before).
            return await query
                .OrderBy(t => (t.TransferPriority ?? "").Trim().ToLower() == "urgent" ? 1 : 0)
                .ThenByDescending(t => t.CreatedOn)
                .Select(t => (decimal?)t.AmountPerUnit)
                .FirstOrDefaultAsync();
        }

        private static decimal ParsePropertySizeValue(string? registeredSize)
        {
            if (string.IsNullOrWhiteSpace(registeredSize))
                return 0m;

            var match = System.Text.RegularExpressions.Regex.Match(registeredSize, @"\d+(\.\d+)?");
            if (!match.Success)
                return 0m;

            return decimal.TryParse(match.Value, out var parsed) ? parsed : 0m;
        }

        [HttpPost]
        public async Task<IActionResult> GetTransferFeeForTransfer(string customerId, string transferType, string? transferPriority = null)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var resolvedId = await ResolveCustomerIdAsync(customerId);
            if (string.IsNullOrEmpty(resolvedId))
                return Json(new { success = false, message = "Customer not found." });

            var customer = await _context.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == resolvedId);
            if (customer == null)
                return Json(new { success = false, message = "Customer not found." });

            if (string.IsNullOrWhiteSpace(transferType))
                return Json(new { success = false, message = "Select a transfer type." });

            var amount = await LookupTransferFeeAsync(
                customer.ProjectID ?? string.Empty,
                customer.SubProject,
                transferType,
                transferPriority);

            if (!amount.HasValue)
            {
                return Json(new
                {
                    success = false,
                    message = "No matching transfer fee row. Add one under Transfer Fee for this project, sub-project, type, and priority."
                });
            }
            var propertySize = ParsePropertySizeValue(customer.RegisteredSize);
            if (propertySize <= 0m)
            {
                return Json(new
                {
                    success = false,
                    message = "Customer size does not contain a numeric value. Please correct Registered Size first."
                });
            }
            var totalAmount = Math.Round(amount.Value * propertySize, 2, MidpointRounding.AwayFromZero);
            return Json(new
            {
                success = true,
                amountPerUnit = (double)amount.Value,
                propertySize = (double)propertySize,
                amount = (double)totalAmount
            });
        }

        [HttpPost]
        public async Task<IActionResult> GetCustomerForTransfer(string customerId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            if (string.IsNullOrWhiteSpace(customerId))
                return Json(new { success = false, message = "Customer ID is required." });

            var resolvedId = await ResolveCustomerIdAsync(customerId);
            if (string.IsNullOrEmpty(resolvedId))
                return Json(new { success = false, message = "Customer not found." });

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == resolvedId);

            if (customer == null)
                return Json(new { success = false, message = "Customer not found." });

            var today = DateTime.Today;
            var activeNDC = await _context.NDCs
                .AsNoTracking()
                .Where(n => n.CustomerID != null && n.CustomerID.Trim() == resolvedId.Trim()
                    && n.NDCExpiryDate.HasValue
                    && n.IssuedDate.Date <= today
                    && n.NDCExpiryDate.Value.Date >= today)
                .FirstOrDefaultAsync();

            var hasActiveNDC = activeNDC != null;
            var ndcMessage = hasActiveNDC ? null : "No active NDC found. Transfer requires an NDC where current date is between Issue Date and Expiry Date.";

            return Json(new
            {
                success = true,
                customerID = customer.CustomerID,
                projectId = (customer.ProjectID ?? string.Empty).Trim(),
                subProject = (customer.SubProject ?? string.Empty).Trim(),
                sellerName = customer.FullName,
                sellerFatherName = customer.FatherName,
                sellerCNIC = customer.CNIC,
                sellerContact = customer.Phone,
                sellerAddress = customer.MailingAddress ?? customer.PermanentAddress,
                jointOwners = await _context.JointOwners
                    .AsNoTracking()
                    .Where(j => j.CustomerID == customer.CustomerID)
                    .OrderByDescending(j => j.CreatedAt)
                    .Select(j => new
                    {
                        id = j.Id,
                        jointOwnerName = j.JointOwnerName,
                        fatherName = j.FatherName,
                        cnic = j.CNIC,
                        contact = j.Contact,
                        address = j.Address,
                        percentage = j.Percentage
                    })
                    .ToListAsync(),
                hasActiveNDC,
                ndcMessage
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetTransferJointOwners(string transferId, string customerId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;

            var normalizedTransferId = NormalizeId(transferId);
            var normalizedCustomerId = NormalizeId(customerId);
            if (string.IsNullOrEmpty(normalizedTransferId) || string.IsNullOrEmpty(normalizedCustomerId))
            {
                return Json(new { success = false, message = "Transfer ID and Customer ID are required." });
            }

            var items = await _context.TransferJointOwners
                .AsNoTracking()
                .Where(j => j.TransferID == normalizedTransferId && j.CustomerID == normalizedCustomerId)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new
                {
                    id = j.Id,
                    transferID = j.TransferID,
                    customerID = j.CustomerID,
                    jointOwnerName = j.JointOwnerName,
                    fatherName = j.FatherName,
                    cnic = j.CNIC,
                    contact = j.Contact,
                    address = j.Address,
                    percentage = j.Percentage,
                    createdAt = j.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                    details = j.Details
                })
                .ToListAsync();

            return Json(new { success = true, transferJointOwners = items });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTransferJointOwner(string transferId, string customerId, string jointOwnerName, string? fatherName, string? cnic, string? contact, string? address, decimal? percentage, string? details)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;

            var normalizedTransferId = NormalizeId(transferId);
            var normalizedCustomerId = NormalizeId(customerId);
            var normalizedName = (jointOwnerName ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(normalizedTransferId) || string.IsNullOrEmpty(normalizedCustomerId))
            {
                return Json(new { success = false, message = "Transfer ID and Customer ID are required." });
            }
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return Json(new { success = false, message = "Joint owner name is required." });
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedName, @"^[a-zA-Z\s\.\-]+$"))
            {
                return Json(new { success = false, message = "Joint owner name must contain letters only." });
            }
            var normalizedCnic = string.IsNullOrWhiteSpace(cnic) ? string.Empty : cnic.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedCnic, @"^\d{5}-\d{7}-\d$"))
            {
                return Json(new { success = false, message = "CNIC is required in XXXXX-XXXXXXX-X format." });
            }
            var normalizedContact = string.IsNullOrWhiteSpace(contact) ? string.Empty : contact.Trim();
            var normalizedContactDigits = NormalizePhoneDigits(normalizedContact);
            if (string.IsNullOrEmpty(normalizedContactDigits) || normalizedContactDigits.Length < 11 || normalizedContactDigits.Length > 13)
            {
                return Json(new { success = false, message = "Contact number is required and must be between 11 and 13 digits." });
            }
            if (percentage.HasValue && (percentage.Value < 0m || percentage.Value > 100m))
            {
                return Json(new { success = false, message = "Percentage must be between 0 and 100." });
            }

            var transfer = await _context.Transfers.FirstOrDefaultAsync(t => t.TransferID == normalizedTransferId);
            if (transfer == null)
            {
                return Json(new { success = false, message = "Transfer not found." });
            }

            if (!string.Equals(transfer.CustomerID?.Trim(), normalizedCustomerId, StringComparison.Ordinal))
            {
                return Json(new { success = false, message = "Transfer and customer mismatch." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var entity = new TransferJointOwner
            {
                Id = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
                TransferID = normalizedTransferId,
                CustomerID = normalizedCustomerId,
                JointOwnerName = normalizedName,
                FatherName = string.IsNullOrWhiteSpace(fatherName) ? null : fatherName.Trim(),
                CNIC = normalizedCnic,
                Contact = normalizedContact,
                Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
                Percentage = percentage,
                CreatedAt = DateTime.Now,
                CreatedBy = userId,
                ModifiedBy = userId,
                Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim()
            };

            _context.TransferJointOwners.Add(entity);
            if (!string.IsNullOrEmpty(userId))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userId,
                    Action = $"Added transfer joint owner {entity.JointOwnerName}",
                    RefType = "TransferJointOwner",
                    RefID = entity.Id,
                    CreatedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Transfer joint owner added successfully." });
        }

        public async Task<IActionResult> Details(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            id = NormalizeId(id);
            if (string.IsNullOrEmpty(id)) return NotFound();

            var transfer = await _context.Transfers
                .Include(t => t.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .Include(t => t.Customer)
                    .ThenInclude(c => c!.JointOwners)
                .FirstOrDefaultAsync(t => t.TransferID == id);

            if (transfer == null) return NotFound();

            ViewBag.WorkflowStatuses = new[] { WorkflowCreated, WorkflowAtAccounts, WorkflowAtTransferApproval, WorkflowApproved };
            return View(transfer);
        }

        /// <summary>
        /// Printable transfer letter (proof of transfer) for approved transfers. Can be printed and handed to the new buyer.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TransferLetter(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            id = NormalizeId(id);
            if (string.IsNullOrEmpty(id)) return NotFound();

            var transfer = await _context.Transfers
                .Include(t => t.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .FirstOrDefaultAsync(t => t.TransferID == id);

            if (transfer == null) return NotFound();

            if (transfer.WorkFlowStatus != WorkflowApproved)
            {
                TempData["ErrorMessage"] = "Transfer letter is only available for approved transfers.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(transfer);
        }

        public async Task<IActionResult> TransferReceipt(string id)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            id = NormalizeId(id);
            if (string.IsNullOrEmpty(id)) return NotFound();

            var transfer = await _context.Transfers
                .Include(t => t.Customer)
                    .ThenInclude(c => c!.PaymentPlan)
                        .ThenInclude(p => p!.Project)
                .FirstOrDefaultAsync(t => t.TransferID == id);

            if (transfer == null) return NotFound();

            if (transfer.WorkFlowStatus == WorkflowApproved)
            {
                TempData["ErrorMessage"] = "This transfer is already approved. Use Transfer Letter instead.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(transfer);
        }

        public async Task<IActionResult> Edit(string id, bool showAttachments = false)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            id = NormalizeId(id);
            if (string.IsNullOrEmpty(id)) return NotFound();

            var transfer = await _context.Transfers
                .Include(t => t.Customer)
                .FirstOrDefaultAsync(t => t.TransferID == id);

            if (transfer == null) return NotFound();

            if (transfer.WorkFlowStatus == WorkflowApproved)
            {
                TempData["ErrorMessage"] = "This transfer is approved and cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.WorkflowStatuses = new[] { WorkflowCreated, WorkflowAtAccounts, WorkflowAtTransferApproval, WorkflowApproved };
            SetCitiesAndCountriesViewBag();
            SetNationalitiesViewBag();
            SetPaymentMethodsViewBag();
            SetBanksViewBag();
            ViewBag.OpenAttachmentsTab = showAttachments;
            return View(transfer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Transfer model)
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            id = NormalizeId(id);
            model.TransferID = NormalizeId(model.TransferID);
            if (string.IsNullOrEmpty(id) || id != model.TransferID) return NotFound();

                var existing = await _context.Transfers
                    .Include(t => t.Customer)
                    .FirstOrDefaultAsync(t => t.TransferID == id);

                if (existing == null) return NotFound();

                if (existing.WorkFlowStatus == WorkflowApproved)
                {
                    TempData["ErrorMessage"] = "This transfer is approved and cannot be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (!model.BuyerDOB.HasValue)
                {
                    model.BuyerDOB = new DateTime(1990, 1, 1);
                }

                ValidateBuyerInformation(model);

                if (string.Equals(model.WorkFlowStatus, WorkflowApproved, StringComparison.OrdinalIgnoreCase))
                {
                    var due = model.TransferFeeDue ?? 0;
                    var paid = model.TransferFeePaid ?? 0;
                    if (Math.Abs(due - paid) > 0.001)
                    {
                        ModelState.AddModelError("", "When Workflow Status is Approved, Transfer Fee Due must equal Transfer Fee Paid.");
                        ViewBag.WorkflowStatuses = new[] { WorkflowCreated, WorkflowAtAccounts, WorkflowAtTransferApproval, WorkflowApproved };
                        SetCitiesAndCountriesViewBag();
                        SetNationalitiesViewBag();
                        SetPaymentMethodsViewBag();
                        SetBanksViewBag();
                        return View(model);
                    }
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.WorkflowStatuses = new[] { WorkflowCreated, WorkflowAtAccounts, WorkflowAtTransferApproval, WorkflowApproved };
                    SetCitiesAndCountriesViewBag();
                    SetNationalitiesViewBag();
                    SetPaymentMethodsViewBag();
                    SetBanksViewBag();
                    return View(model);
                }

                existing.WorkFlowStatus = model.WorkFlowStatus;
                existing.SellerName = model.SellerName;
                existing.SellerFatherName = model.SellerFatherName;
                existing.SellerCNIC = model.SellerCNIC;
                existing.SellerContact = model.SellerContact;
                existing.SellerAddress = model.SellerAddress;
                existing.BuyerName = model.BuyerName;
                existing.BuyerFatherName = model.BuyerFatherName;
                existing.BuyerCNIC = model.BuyerCNIC;
                existing.BuyerPassportNo = model.BuyerPassportNo;
                existing.BuyerDOB = model.BuyerDOB;
                existing.BuyerGender = model.BuyerGender;
                existing.BuyerNationality = model.BuyerNationality;
                existing.BuyerEmail = model.BuyerEmail;
                existing.BuyerPhone = model.BuyerPhone;
                existing.BuyerMobile = model.BuyerMobile;
                existing.BuyerMobile2 = model.BuyerMobile2;
                existing.BuyerContact = model.BuyerPhone;
                existing.BuyerAddress = model.BuyerMailingAddress;
                existing.BuyerMailingAddress = model.BuyerMailingAddress;
                existing.BuyerPermanentAddress = model.BuyerPermanentAddress;
                existing.BuyerCity = model.BuyerCity;
                existing.BuyerCountry = model.BuyerCountry;
                existing.BuyerAttachments = model.BuyerAttachments;
                existing.SellerAttachments = model.SellerAttachments;
                existing.SellerBiometric = model.SellerBiometric;
                existing.BuyerBiometric = model.BuyerBiometric;
                existing.TransferFeeDue = model.TransferFeeDue;
                existing.TransferFeePaid = model.TransferFeePaid;
                existing.PaymentDate = model.PaymentDate;
                existing.PaymentMode = model.PaymentMode;
                existing.PaymentChallanNo = model.PaymentChallanNo;
                existing.Details = model.Details;
                existing.CROComments = model.CROComments;
                existing.AccountsComments = model.AccountsComments;
                existing.TransferComments = model.TransferComments;

                if (model.WorkFlowStatus == WorkflowApproved)
                {
                    var customer = existing.Customer;
                    if (customer != null)
                    {
                        customer.FullName = model.BuyerName ?? customer.FullName;
                        customer.FatherName = model.BuyerFatherName ?? customer.FatherName;
                        customer.CNIC = model.BuyerCNIC ?? customer.CNIC;
                        customer.PassportNo = model.BuyerPassportNo ?? customer.PassportNo;
                        customer.DOB = model.BuyerDOB ?? customer.DOB;
                        customer.Gender = model.BuyerGender ?? customer.Gender;
                        customer.Nationality = model.BuyerNationality ?? customer.Nationality;
                        customer.Email = model.BuyerEmail ?? customer.Email;
                        customer.Phone = model.BuyerPhone ?? customer.Phone;
                        customer.MobileNo = model.BuyerMobile ?? customer.MobileNo;
                        customer.MobileNo2 = model.BuyerMobile2 ?? customer.MobileNo2;
                        customer.MailingAddress = model.BuyerMailingAddress ?? customer.MailingAddress;
                        customer.PermanentAddress = model.BuyerPermanentAddress ?? customer.PermanentAddress;
                        customer.City = model.BuyerCity ?? customer.City;
                        customer.Country = model.BuyerCountry ?? customer.Country;
                    }

                    var transferJointOwners = await _context.TransferJointOwners
                        .Where(j => j.TransferID == existing.TransferID && j.CustomerID == existing.CustomerID)
                        .ToListAsync();

                    var oldJointOwners = await _context.JointOwners
                        .Where(j => j.CustomerID == existing.CustomerID)
                        .ToListAsync();
                    if (oldJointOwners.Count > 0)
                    {
                        _context.JointOwners.RemoveRange(oldJointOwners);
                    }

                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (transferJointOwners.Count > 0)
                    {
                        var newJointOwners = transferJointOwners.Select(j => new JointOwner
                        {
                            Id = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
                            CustomerID = j.CustomerID,
                            JointOwnerName = j.JointOwnerName,
                            FatherName = j.FatherName,
                            CNIC = j.CNIC,
                            Contact = j.Contact,
                            Address = j.Address,
                            Percentage = j.Percentage,
                            CreatedAt = DateTime.Now,
                            CreatedBy = userId,
                            ModifiedBy = userId,
                            Details = j.Details
                        }).ToList();
                        _context.JointOwners.AddRange(newJointOwners);
                    }

                    if (!string.IsNullOrEmpty(userId))
                    {
                        _context.ActivityLogs.Add(new ActivityLog
                        {
                            UserID = userId,
                            Action = $"Transfer approved: replaced customer joint owners (deleted {oldJointOwners.Count}, added {transferJointOwners.Count}).",
                            RefType = "Transfer",
                        RefID = SafeRefId(existing.TransferID),
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;

            id = NormalizeId(id);
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Transfer ID is required.";
                return RedirectToAction(nameof(Index));
            }

            var transfer = await _context.Transfers.FirstOrDefaultAsync(t => t.TransferID == id);
            if (transfer == null)
            {
                TempData["ErrorMessage"] = "Transfer not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!IsInitiatedStatus(transfer.WorkFlowStatus))
            {
                TempData["ErrorMessage"] = "Only initiated transfers can be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _context.Transfers.Remove(transfer);

            if (!string.IsNullOrEmpty(userId))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserID = userId,
                    Action = $"Delete Transfer {transfer.TransferID}",
                    RefType = "Transfer",
                    RefID = transfer.TransferID,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Transfer {transfer.TransferID} deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private void ValidateBuyerInformation(Transfer model)
        {
            var cnic = (model.BuyerCNIC ?? string.Empty).Trim();
            var passport = (model.BuyerPassportNo ?? string.Empty).Trim();
            var phone = (model.BuyerPhone ?? string.Empty).Trim();
            var mobile = (model.BuyerMobile ?? string.Empty).Trim();
            var mobile2 = (model.BuyerMobile2 ?? string.Empty).Trim();

            model.BuyerCNIC = string.IsNullOrWhiteSpace(cnic) ? null : cnic;
            model.BuyerPassportNo = string.IsNullOrWhiteSpace(passport) ? null : passport;
            model.BuyerPhone = string.IsNullOrWhiteSpace(phone) ? null : phone;
            model.BuyerMobile = string.IsNullOrWhiteSpace(mobile) ? null : mobile;
            model.BuyerMobile2 = string.IsNullOrWhiteSpace(mobile2) ? null : mobile2;

            if (string.IsNullOrWhiteSpace(model.BuyerName))
                ModelState.AddModelError(nameof(model.BuyerName), "Full Name is required.");

            if (!string.IsNullOrWhiteSpace(model.BuyerFatherName) && !System.Text.RegularExpressions.Regex.IsMatch(model.BuyerFatherName.Trim(), @"^[a-zA-Z\s\.\-]+$"))
                ModelState.AddModelError(nameof(model.BuyerFatherName), "Father's Name must contain letters only.");

            var cnicValid = !string.IsNullOrWhiteSpace(cnic) && System.Text.RegularExpressions.Regex.IsMatch(cnic, @"^\d{5}-\d{7}-\d$");
            if (!string.IsNullOrWhiteSpace(cnic) && !cnicValid)
                ModelState.AddModelError(nameof(model.BuyerCNIC), "National ID (CNIC) must be in format XXXXX-XXXXXXX-X.");

            if (!string.IsNullOrWhiteSpace(passport) && passport.Length < 5)
                ModelState.AddModelError(nameof(model.BuyerPassportNo), "Passport Number must be at least 5 characters if no CNIC.");

            if (string.IsNullOrWhiteSpace(cnic) && string.IsNullOrWhiteSpace(passport))
                ModelState.AddModelError(nameof(model.BuyerCNIC), "Either CNIC or Passport Number is required.");

            if (string.IsNullOrWhiteSpace(model.BuyerPhone))
                ModelState.AddModelError(nameof(model.BuyerPhone), "Phone (landline / primary contact) is required.");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.BuyerPhone, @"^[0-9\+]+$"))
                ModelState.AddModelError(nameof(model.BuyerPhone), "Phone must contain digits and '+' only.");

            if (string.IsNullOrWhiteSpace(model.BuyerMobile))
                ModelState.AddModelError(nameof(model.BuyerMobile), "Mobile is required.");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.BuyerMobile, @"^[0-9]+$"))
                ModelState.AddModelError(nameof(model.BuyerMobile), "Mobile must contain digits only.");

            if (!string.IsNullOrWhiteSpace(model.BuyerMobile2) && !System.Text.RegularExpressions.Regex.IsMatch(model.BuyerMobile2, @"^[0-9]+$"))
                ModelState.AddModelError(nameof(model.BuyerMobile2), "Mobile 2 must contain digits only.");

            var phoneNorm = NormalizePhoneDigits(model.BuyerPhone);
            var mobileNorm = NormalizePhoneDigits(model.BuyerMobile);
            var mobile2Norm = NormalizePhoneDigits(model.BuyerMobile2);

            if (!string.IsNullOrEmpty(mobile2Norm) && !string.IsNullOrEmpty(phoneNorm) && string.Equals(mobile2Norm, phoneNorm, StringComparison.Ordinal))
                ModelState.AddModelError(nameof(model.BuyerMobile2), "Mobile 2 must differ from Phone.");
            if (!string.IsNullOrEmpty(mobile2Norm) && !string.IsNullOrEmpty(mobileNorm) && string.Equals(mobile2Norm, mobileNorm, StringComparison.Ordinal))
                ModelState.AddModelError(nameof(model.BuyerMobile2), "Mobile 2 must differ from Mobile.");

            if (string.IsNullOrWhiteSpace(model.BuyerCity))
                ModelState.AddModelError(nameof(model.BuyerCity), "City is required.");

            if (string.IsNullOrWhiteSpace(model.BuyerCountry))
                ModelState.AddModelError(nameof(model.BuyerCountry), "Country is required.");

            if (string.IsNullOrWhiteSpace(model.BuyerMailingAddress))
                ModelState.AddModelError(nameof(model.BuyerMailingAddress), "Mailing Address is required.");
        }

        private static string NormalizePhoneDigits(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return new string(value.Where(char.IsDigit).ToArray());
        }

        private static bool IsInitiatedStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            return string.Equals(status.Trim(), WorkflowCreated, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status.Trim(), "Initiated", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> GenerateTransferIdAsync()
        {
            var prefix = "TRF-";
            var today = DateTime.Today.ToString("yyyyMMdd");
            var existing = await _context.Transfers
                .Where(t => t.TransferID.StartsWith(prefix + today))
                .OrderByDescending(t => t.TransferID)
                .Select(t => t.TransferID)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (!string.IsNullOrEmpty(existing) && existing.Length >= prefix.Length + today.Length + 2)
            {
                var part = existing[(prefix.Length + today.Length)..].TrimStart('-');
                if (int.TryParse(part, out var n)) seq = n + 1;
            }
            return prefix + today + "-" + seq.ToString("D4");
        }

        // ========== Attachment management (file upload like Customer module) ==========
        private static readonly string[] AllowedAttachmentExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };
        private static readonly string[] AllowedTransferAttachmentTypes =
        {
            "PowerOfAttorney",
            "PaymentReceipts",
            "BookingForm",
            "BuyerPhoto",
            "SellerPhoto",
            "BuyerCNIC",
            "SellerCNIC",
            "Other",
            // Backward compatibility for existing records
            "Seller",
            "Buyer"
        };
        private const long MaxAttachmentSize = 8 * 1024 * 1024; // 8MB

        [HttpPost]
        public async Task<IActionResult> UploadAttachment(string transferId, List<IFormFile> file, string attachmentType, string description = "")
        {
            var denied = await EnsurePermissionAsync("Edit");
            if (denied != null) return denied;
            try
            {
                var normalizedTransferId = NormalizeId(transferId);
                if (string.IsNullOrEmpty(normalizedTransferId))
                    return Json(new { success = false, message = "Transfer ID is required." });
                if (file == null || file.Count == 0)
                    return Json(new { success = false, message = "Please select a file to upload." });
                if (string.IsNullOrWhiteSpace(attachmentType) || !AllowedTransferAttachmentTypes.Contains(attachmentType))
                    return Json(new { success = false, message = "Invalid attachment type." });
                if (string.Equals(attachmentType, "Other", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(description))
                    return Json(new { success = false, message = "Title is required for Other Attachments." });

                var transfer = await _context.Transfers.FindAsync(normalizedTransferId);
                if (transfer == null)
                    return Json(new { success = false, message = "Transfer not found." });

                var safeId = SanitizePathSegment(normalizedTransferId);
                if (string.IsNullOrEmpty(safeId))
                {
                    return Json(new { success = false, message = "Invalid Transfer ID for file storage." });
                }
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "transfers", safeId);
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uploadedByRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var uploadedBy = string.IsNullOrEmpty(uploadedByRaw) ? null
                    : (uploadedByRaw.Length <= 10 ? uploadedByRaw : uploadedByRaw.Substring(0, 10));
                var uploadedAttachments = new List<Attachment>();

                foreach (var oneFile in file.Where(f => f != null && f.Length > 0))
                {
                    var ext = Path.GetExtension(oneFile.FileName).ToLowerInvariant();
                    if (!AllowedAttachmentExtensions.Contains(ext))
                        return Json(new { success = false, message = "Only image files (JPG, PNG, GIF, BMP) and PDF are allowed." });
                    if (oneFile.Length > MaxAttachmentSize)
                        return Json(new { success = false, message = "File size exceeds 8MB limit." });

                    var uniqueFileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    var relativePath = $"/uploads/transfers/{safeId}/{uniqueFileName}";

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await oneFile.CopyToAsync(stream);

                    var attachment = new Attachment
                    {
                        AttachmentID = Guid.NewGuid().ToString("N")[..10].ToUpper(),
                        RefType = "Transfer",
                        RefID = normalizedTransferId,
                        AttachmentType = attachmentType,
                        FileName = oneFile.FileName,
                        FilePath = relativePath,
                        FileSize = oneFile.Length,
                        FileType = oneFile.ContentType,
                        Description = description,
                        UploadedBy = uploadedBy,
                        UploadedAt = DateTime.Now
                    };
                    uploadedAttachments.Add(attachment);
                }

                if (uploadedAttachments.Count == 0)
                    return Json(new { success = false, message = "Please select a valid file to upload." });

                _context.Attachments.AddRange(uploadedAttachments);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "File uploaded successfully",
                    attachments = uploadedAttachments.Select(attachment => new
                    {
                        attachmentID = attachment.AttachmentID,
                        fileName = attachment.FileName,
                        filePath = attachment.FilePath,
                        fileSize = attachment.FileSize,
                        fileType = attachment.FileType,
                        attachmentType = attachment.AttachmentType,
                        description = attachment.Description,
                        uploadedAt = attachment.UploadedAt.ToString("MMM dd, yyyy hh:mm tt")
                    })
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null)
                    msg += " " + ex.InnerException.Message;
                return Json(new { success = false, message = msg });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttachments(string transferId)
        {
            var denied = await EnsurePermissionAsync("Read");
            if (denied != null) return denied;
            try
            {
                var normalizedTransferId = NormalizeId(transferId);
                if (string.IsNullOrEmpty(normalizedTransferId))
                    return Json(new { success = false, message = "Transfer ID is required." });

                var attachments = await _context.Attachments
                    .Where(a => a.RefType == "Transfer" && a.RefID == normalizedTransferId)
                    .OrderByDescending(a => a.UploadedAt)
                    .Select(a => new
                    {
                        attachmentID = a.AttachmentID,
                        fileName = a.FileName,
                        filePath = a.FilePath,
                        fileSize = a.FileSize,
                        fileType = a.FileType,
                        attachmentType = a.AttachmentType,
                        description = a.Description,
                        uploadedAt = a.UploadedAt.ToString("MMM dd, yyyy hh:mm tt")
                    })
                    .ToListAsync();

                return Json(new { success = true, attachments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttachment(string attachmentId)
        {
            var denied = await EnsurePermissionAsync("Admin");
            if (denied != null) return denied;
            try
            {
                if (string.IsNullOrEmpty(attachmentId))
                    return Json(new { success = false, message = "Attachment ID is required." });

                var attachment = await _context.Attachments
                    .FirstOrDefaultAsync(a => a.AttachmentID == attachmentId && a.RefType == "Transfer");
                if (attachment == null)
                    return Json(new { success = false, message = "Attachment not found." });

                if (!string.IsNullOrEmpty(attachment.FilePath))
                {
                    var trimmedPath = attachment.FilePath.TrimStart('~').TrimStart('/');
                    var fullPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        trimmedPath.Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }
                _context.Attachments.Remove(attachment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Attachment deleted." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
