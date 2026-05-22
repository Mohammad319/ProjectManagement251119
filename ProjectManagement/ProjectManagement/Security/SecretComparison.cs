using System.Security.Cryptography;
using System.Text;

namespace ProjectManagement.Security;

public static class SecretComparison
{
    public static bool FixedTimeEquals(string? suppliedSecret, string? expectedSecret)
    {
        if (string.IsNullOrWhiteSpace(suppliedSecret) || string.IsNullOrWhiteSpace(expectedSecret))
            return false;

        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedSecret));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedSecret));

        return CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash);
    }
}
