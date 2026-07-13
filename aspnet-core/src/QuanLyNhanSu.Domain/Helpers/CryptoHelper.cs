using System;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyNhanSu.Helpers
{
    public static class CryptoHelper
    {
        private static readonly char[] Base62Chars =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

        /// <summary>
        /// Generates a cryptographically secure random string of the specified length.
        /// Uses Base62 encoding to ensure it is URL-safe and human-readable.
        /// </summary>
        public static string GenerateSecureKey(int length = 16)
        {
            var data = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }

            var result = new StringBuilder(length);
            foreach (var b in data)
            {
                result.Append(Base62Chars[b % Base62Chars.Length]);
            }

            return result.ToString().ToUpper(); // Uppercase for consistency with old format
        }
    }
}
