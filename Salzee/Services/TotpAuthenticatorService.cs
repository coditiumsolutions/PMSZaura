using System.Text;
using System.Text.Json;
using OtpNet;
using QRCoder;

namespace Salzee.Services
{
    public interface ITotpAuthenticatorService
    {
        string GenerateBase32Secret();
        string BuildOtpAuthUri(string issuer, string accountLabel, string base32Secret);
        bool VerifyTotp(string base32Secret, string sixDigitCode);
        byte[] CreateQrCodePng(string otpAuthUri);
    }

    public class TotpAuthenticatorService : ITotpAuthenticatorService
    {
        public string GenerateBase32Secret()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        public string BuildOtpAuthUri(string issuer, string accountLabel, string base32Secret)
        {
            // Google Key Uri Format: path label = percent-encoded "issuer:accountName" (single encoding pass).
            var issuerTrim = issuer.Trim();
            var accountTrim = (accountLabel ?? string.Empty).Trim();
            var label = string.IsNullOrEmpty(accountTrim)
                ? Uri.EscapeDataString(issuerTrim)
                : Uri.EscapeDataString($"{issuerTrim}:{accountTrim}");
            var encIssuer = Uri.EscapeDataString(issuerTrim);
            var secret = base32Secret.Trim().Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
            return $"otpauth://totp/{label}?secret={secret}&issuer={encIssuer}&digits=6";
        }

        public bool VerifyTotp(string base32Secret, string sixDigitCode)
        {
            // #region agent log
            AgentDebugLog.Write("H3", "TotpAuthenticatorService.VerifyTotp:entry", "verify_enter", new
            {
                secretLen = base32Secret?.Length ?? 0,
                codeLen = sixDigitCode?.Length ?? 0
            });
            // #endregion
            if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(sixDigitCode))
            {
                // #region agent log
                AgentDebugLog.Write("H3", "TotpAuthenticatorService.VerifyTotp", "early_null", new { });
                // #endregion
                return false;
            }
            var normalized = sixDigitCode.Trim().Replace(" ", "", StringComparison.Ordinal);
            if (normalized.Length != 6 || !normalized.All(char.IsDigit))
            {
                // #region agent log
                AgentDebugLog.Write("H3", "TotpAuthenticatorService.VerifyTotp", "early_format", new
                {
                    normalizedLen = normalized.Length,
                    allDigits = normalized.All(char.IsDigit)
                });
                // #endregion
                return false;
            }
            try
            {
                var secretNorm = base32Secret.Trim().Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
                var secretBytes = Base32Encoding.ToBytes(secretNorm);
                var totp = new Totp(secretBytes);
                var okTight = totp.VerifyTotp(normalized, out _, new VerificationWindow(1, 1));
                var okWide = totp.VerifyTotp(normalized, out _, new VerificationWindow(5, 5));
                var okLoose = totp.VerifyTotp(normalized, out _, new VerificationWindow(20, 20));
                // #region agent log
                AgentDebugLog.Write("H2", "TotpAuthenticatorService.VerifyTotp", "verify_windows", new
                {
                    okTight,
                    okWide,
                    okLoose,
                    secretByteLen = secretBytes.Length,
                    utc = DateTime.UtcNow.ToString("O")
                });
                // #endregion
                return okTight || okWide || okLoose;
            }
            catch (Exception ex)
            {
                // #region agent log
                AgentDebugLog.Write("H1", "TotpAuthenticatorService.VerifyTotp", "base32_exception", new
                {
                    exType = ex.GetType().Name
                });
                // #endregion
                return false;
            }
        }

        public byte[] CreateQrCodePng(string otpAuthUri)
        {
            using var qr = new QRCodeGenerator();
            using var data = qr.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            return png.GetGraphic(4);
        }
    }

    /// <summary>JSON payloads for Data Protection–wrapped cookies.</summary>
    public static class TwoFactorPendingSerializer
    {
        public static byte[] SerializePending(string userId, DateTimeOffset expiresUtc) =>
            JsonSerializer.SerializeToUtf8Bytes(new PendingDto(userId, expiresUtc), JsonOptions);

        public static PendingDto? DeserializePending(byte[] json)
        {
            try
            {
                return JsonSerializer.Deserialize<PendingDto>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static byte[] SerializeSetup(string userId, string secretBase32, DateTimeOffset expiresUtc) =>
            JsonSerializer.SerializeToUtf8Bytes(new SetupDto(userId, secretBase32, expiresUtc), JsonOptions);

        public static SetupDto? DeserializeSetup(byte[] json)
        {
            try
            {
                return JsonSerializer.Deserialize<SetupDto>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
        };

        public record PendingDto(string UserId, DateTimeOffset ExpiresUtc);
        public record SetupDto(string UserId, string SecretBase32, DateTimeOffset ExpiresUtc);
    }
}
