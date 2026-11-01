using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Salzee.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserMacController : Controller
    {
        private readonly PMSDbContext _context;

        public UserMacController(PMSDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? userId)
        {
            var users = await _context.Users.OrderBy(u => u.FullName).ToListAsync();
            var selectedUserId = string.IsNullOrWhiteSpace(userId) ? users.FirstOrDefault()?.UserID : userId;

            var vm = new UserMacWhitelistViewModel
            {
                Users = users,
                SelectedUserId = selectedUserId
            };

            if (!string.IsNullOrWhiteSpace(selectedUserId))
            {
                vm.SelectedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserID == selectedUserId);
                vm.WhitelistedMacs = await _context.UserMacWhitelists
                    .Where(w => w.UserID == selectedUserId)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();

                vm.BlockedAttempts = await _context.BlockedMacLoginAttempts
                    .Where(a => a.UserID == selectedUserId && !a.IsWhitelisted)
                    .OrderByDescending(a => a.AttemptedAt)
                    .Take(100)
                    .ToListAsync();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWhitelist(string userId, string macAddress, string? deviceName, int? attemptId)
        {
            var normalizedMac = NormalizeThumbprint(macAddress);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(normalizedMac))
            {
                TempData["Error"] = "Please provide a valid certificate thumbprint.";
                return RedirectToAction(nameof(Index), new { userId });
            }

            var existing = await _context.UserMacWhitelists
                .FirstOrDefaultAsync(w => w.UserID == userId && w.MacAddress == normalizedMac);

            var actor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (existing == null)
            {
                _context.UserMacWhitelists.Add(new UserMacWhitelist
                {
                    UserID = userId,
                    MacAddress = normalizedMac,
                    DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim(),
                    AddedBy = actor,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.IsActive = true;
                existing.DeviceName = string.IsNullOrWhiteSpace(deviceName) ? existing.DeviceName : deviceName.Trim();
            }

            if (attemptId.HasValue)
            {
                var attempt = await _context.BlockedMacLoginAttempts.FirstOrDefaultAsync(a => a.Id == attemptId.Value && a.UserID == userId);
                if (attempt != null)
                {
                    attempt.IsWhitelisted = true;
                    attempt.WhitelistedBy = actor;
                    attempt.WhitelistedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Certificate {normalizedMac} whitelisted for user {userId}.";
            return RedirectToAction(nameof(Index), new { userId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id, string userId)
        {
            var row = await _context.UserMacWhitelists.FirstOrDefaultAsync(w => w.Id == id);
            if (row != null)
            {
                row.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Certificate entry deactivated.";
            }
            return RedirectToAction(nameof(Index), new { userId });
        }

        private static string? NormalizeThumbprint(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var hexOnly = Regex.Replace(input, "[^0-9A-Fa-f]", "").ToUpperInvariant();
            if (hexOnly.Length < 16 || !Regex.IsMatch(hexOnly, "^[0-9A-F]+$"))
                return null;

            return hexOnly;
        }
    }
}
