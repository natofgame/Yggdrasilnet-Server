using System.Text.RegularExpressions;

namespace Yggdrasilnet.Server.Utils;

public static partial class PlayerNameValidator {
    private const int MinLength = 3;
    private const int MaxLength = 16;

    [GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial Regex AllowedChars();

    public static bool TryNormalize(string? raw, out string name, out string error) {
        name = (raw ?? string.Empty).Trim();

        if (name.Length < MinLength || name.Length > MaxLength) {
            error = $"Name must be {MinLength}-{MaxLength} characters.";
            return false;
        }

        if (!AllowedChars().IsMatch(name)) {
            error = "Name can only contain letters, digits and underscores.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}