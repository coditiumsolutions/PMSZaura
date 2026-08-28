using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PMS.Services
{
    public interface IReportServiceClient
    {
        /// <summary>
        /// Fetches Account Statement PDF from the Coditium report service.
        /// </summary>
        Task<byte[]> GetAccountStatementPdfAsync(string customerId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// HTTP client for <c>coditium-report-service</c> (see repo <c>api.txt</c>).
    /// </summary>
    public class ReportServiceClient : IReportServiceClient
    {
        public const string HttpClientName = "ReportService";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReportServiceClient> _logger;

        public ReportServiceClient(
            IHttpClientFactory httpClientFactory,
            ILogger<ReportServiceClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<byte[]> GetAccountStatementPdfAsync(string customerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("customerId is required.", nameof(customerId));

            var id = customerId.Trim();
            var url = $"/api/reports/account-statement?customerId={Uri.EscapeDataString(id)}";
            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var response = await client.GetAsync(url, cancellationToken);
            var mediaType = response.Content.Headers.ContentType?.MediaType;

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var message = await TryReadErrorMessageAsync(response, cancellationToken)
                    ?? $"Customer not found: {id}";
                throw new KeyNotFoundException(message);
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = await TryReadErrorMessageAsync(response, cancellationToken)
                    ?? $"Report service returned {(int)response.StatusCode}.";
                _logger.LogError(
                    "Account statement PDF failed for {CustomerId}. Status={Status}. Message={Message}",
                    id, (int)response.StatusCode, message);
                throw new InvalidOperationException(message);
            }

            if (!string.Equals(mediaType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                var bodyPreview = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Account statement expected PDF for {CustomerId} but got Content-Type={ContentType}. Body={Body}",
                    id, mediaType, Trim(bodyPreview));
                throw new InvalidOperationException("Report service did not return a PDF.");
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                    return msg.GetString();
            }
            catch
            {
                // ignore parse errors
            }

            return null;
        }

        private static string Trim(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            text = text.Trim();
            return text.Length <= 400 ? text : text[..400] + "...";
        }
    }
}
