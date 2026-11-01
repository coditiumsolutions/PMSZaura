using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace Salzee.Services
{
    /// <summary>Protects TOTP shared secrets at rest (DB) and in short-lived setup cookies.</summary>
    public class TotpSecretProtector
    {
        private readonly IDataProtector _userSecretProtector;
        private readonly IDataProtector _pendingLoginProtector;
        private readonly IDataProtector _setupProtector;

        public TotpSecretProtector(IDataProtectionProvider provider)
        {
            _userSecretProtector = provider.CreateProtector("Salzee.TOTP.UserSecret.v1");
            _pendingLoginProtector = provider.CreateProtector("Salzee.TOTP.PendingLogin.v1");
            _setupProtector = provider.CreateProtector("Salzee.TOTP.SetupSecret.v1");
        }

        public string ProtectForStorage(string plainBase32) =>
            Convert.ToBase64String(_userSecretProtector.Protect(Encoding.UTF8.GetBytes(plainBase32)));

        public string? UnprotectFromStorage(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return null;
            try
            {
                var bytes = _userSecretProtector.Unprotect(Convert.FromBase64String(stored));
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return null;
            }
        }

        public string ProtectPendingPayload(byte[] payload) =>
            Convert.ToBase64String(_pendingLoginProtector.Protect(payload));

        public byte[]? UnprotectPendingPayload(string? cookieValue)
        {
            if (string.IsNullOrWhiteSpace(cookieValue)) return null;
            try
            {
                return _pendingLoginProtector.Unprotect(Convert.FromBase64String(cookieValue));
            }
            catch
            {
                return null;
            }
        }

        public string ProtectSetupPayload(byte[] payload) =>
            Convert.ToBase64String(_setupProtector.Protect(payload));

        public byte[]? UnprotectSetupPayload(string? cookieValue)
        {
            if (string.IsNullOrWhiteSpace(cookieValue)) return null;
            try
            {
                return _setupProtector.Unprotect(Convert.FromBase64String(cookieValue));
            }
            catch
            {
                return null;
            }
        }
    }
}
