using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace FastDrop.MVC.Security;

public sealed class JwtSettings
{
    // Fingerprint of the previously published key. It must never be reused,
    // including when an operator encodes that old value as Base64.
    private const string CompromisedKeyFingerprint =
        "AC67870F24F383DFB417980D9B73F8390E9831D2A40946B001E55F87CC4F9EB3";

    public string Issuer { get; }
    public string Audience { get; }
    public SymmetricSecurityKey SigningKey { get; }

    private JwtSettings(string issuer, string audience, byte[] keyBytes)
    {
        Issuer = issuer;
        Audience = audience;
        SigningKey = new SymmetricSecurityKey(keyBytes);
    }

    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        var configuredKey = configuration["JwtSettings:Key"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            throw new InvalidOperationException(
                "JwtSettings:Key is required. Configure a private Base64-encoded random key " +
                "of at least 32 bytes using user secrets or JwtSettings__Key.");
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(configuredKey);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "JwtSettings:Key must be a Base64-encoded random key of at least 32 bytes.");
        }

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("JwtSettings:Key must decode to at least 32 bytes.");
        }

        if (CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(keyBytes), Convert.FromHexString(CompromisedKeyFingerprint)))
        {
            throw new InvalidOperationException(
                "JwtSettings:Key matches a previously published key. Generate a new private key.");
        }

        var issuer = configuration["JwtSettings:Issuer"];
        var audience = configuration["JwtSettings:Audience"];
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("JwtSettings:Issuer and JwtSettings:Audience are required.");
        }

        return new JwtSettings(issuer, audience, keyBytes);
    }
}
