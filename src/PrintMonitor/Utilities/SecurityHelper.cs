using System.Security.Cryptography;
using System.Text;

namespace PrintMonitor.Utilities;

public static class SecurityHelper
{
    public const string DefaultPassword = "admin";

    public static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password.Trim());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
