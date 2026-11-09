using System;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// P3: extracted from AuthService (god-class split).
    /// Pure helpers (no DB, no UI) — unit-testable.
    /// </summary>
    public static class PasswordPolicy
    {
        /// <summary>C2: fail closed on missing/empty/malformed password hashes.</summary>
        public static bool IsValidPasswordHash(string? hash)
        {
            if (string.IsNullOrWhiteSpace(hash)) return false;
            if (hash.Length != 60) return false;
            if (!(hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$"))) return false;
            return true;
        }

        public static bool IsValidPassword(string? password, int minLength = 4)
            => !string.IsNullOrEmpty(password) && password.Length >= minLength;

        public static bool IsBootstrapPasswordStrong(string? password)
            => !string.IsNullOrEmpty(password) && password.Length >= 12;
    }
}
