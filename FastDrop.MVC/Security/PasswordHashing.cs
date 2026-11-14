using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace FastDrop.MVC.Security;

public static class PasswordHashing
{
    // Distinguish enhanced bcrypt from existing standard bcrypt hashes.
    public const string EnhancedPrefix = "bcrypt-sha384:";

    private static readonly Regex BcryptFormat = new(
        @"^\$2[aby]\$(0[4-9]|[12][0-9]|3[01])\$[./A-Za-z0-9]{53}$",
        RegexOptions.CultureInvariant);

    public static string HashNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 100)
            throw new ValidationException("Passwords must contain between 8 and 100 characters.");

        return HashLegacyPassword(password);
    }

    internal static string HashLegacyPassword(string password) => EnhancedPrefix +
        BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 12);

    public static bool IsHash(string? storedPassword)
    {
        if (string.IsNullOrEmpty(storedPassword)) return false;
        var hash = storedPassword.StartsWith(EnhancedPrefix, StringComparison.Ordinal)
            ? storedPassword[EnhancedPrefix.Length..] : storedPassword;
        return BcryptFormat.IsMatch(hash);
    }

    public static bool Verify(string password, string? storedPassword)
    {
        if (string.IsNullOrEmpty(password) || !IsHash(storedPassword)) return false;
        try
        {
            return storedPassword!.StartsWith(EnhancedPrefix, StringComparison.Ordinal)
                ? BCrypt.Net.BCrypt.EnhancedVerify(password, storedPassword[EnhancedPrefix.Length..])
                : BCrypt.Net.BCrypt.Verify(password, storedPassword);
        }
        catch (BCrypt.Net.SaltParseException) { return false; }
        catch (ArgumentException) { return false; }
        catch (FormatException) { return false; }
    }
}
