using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Salzee.Data;
using Salzee.Models;

namespace Salzee.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowSpecificOrigins")]
    public class InquiryApiController : ControllerBase
    {
        private readonly PMSDbContext _context;

        public InquiryApiController(PMSDbContext context)
        {
            _context = context;
        }

        // GET: api/InquiryApi/Submit
        [HttpGet("Submit")]
        public async Task<IActionResult> Submit(
            string fullName,
            string phoneNumber,
            string? emailAddress = null,
            string? type = null,
            string? message = null)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    return BadRequest(new { success = false, message = "Full name is required" });
                }

                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return BadRequest(new { success = false, message = "Phone number is required" });
                }

                // Create new inquiry
                var inquiry = new PropertyInquiry
                {
                    FullName = fullName.Trim(),
                    PhoneNumber = phoneNumber.Trim(),
                    EmailAddress = emailAddress?.Trim(),
                    InquiryType = type?.Trim(),
                    Message = message?.Trim(),
                    SubmittedAt = DateTime.Now,
                    IPAddress = GetClientIpAddress(),
                    Status = "New",
                    IsContacted = false,
                    CreatedAt = DateTime.Now
                };

                // Save to database
                _context.PropertyInquiries.Add(inquiry);
                await _context.SaveChangesAsync();

                // Return success response
                return Ok(new
                {
                    success = true,
                    message = "Thank you! Your inquiry has been submitted successfully. Our team will contact you soon.",
                    inquiryId = inquiry.InquiryID
                });
            }
            catch (Exception ex)
            {
                // Log error (in production, use proper logging)
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your inquiry. Please try again.",
                    error = ex.Message
                });
            }
        }

        // Helper method to get client IP address
        private string GetClientIpAddress()
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                // Check for forwarded IP (if behind proxy/load balancer)
                if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                {
                    ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
                }
                
                return ipAddress ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    // Request model for API
    public class InquirySubmitRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? EmailAddress { get; set; }
        public string? Type { get; set; }
        public string? Message { get; set; }
    }
}

