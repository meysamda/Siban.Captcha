using System.Security.Cryptography;
using System.Text;

namespace Siban.Captcha;

public static class CryptographyHelper
{
    public static string HashText(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
