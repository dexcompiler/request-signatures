using System.Text;
using System.Security.Cryptography;

namespace RequestSigning.Common;

public sealed class ApiSigner(string secretKey)
{
    public string SignRequest(HttpMethod method, string endpoint, string timestamp, string? body = default)
    {
        var stringToSign = $"{method.Method.ToUpperInvariant()}\n{endpoint}\n{timestamp}\n{body ?? string.Empty}";
        
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
        return Convert.ToBase64String(signatureBytes);
    }
}