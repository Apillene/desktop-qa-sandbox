using System.Text.RegularExpressions;

namespace DesktopSandbox;

/// <summary>
/// Business rules used by the sandbox.
/// Several rules are intentionally simplified or bypassed on purpose:
/// this application is a training object for the QA portfolio, not production code.
/// </summary>
public static class Rules
{
    // Seeded defect D-01: the pattern accepts an address without a top-level domain.
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+$", RegexOptions.Compiled);

    public const int NoteLimit = 255;

    public static bool IsValidEmail(string email) => EmailPattern.IsMatch(email);

    public static bool IsValidAge(int age) => age is > 0 and < 150;

    public static bool IsValidPassword(string password) => password.Length >= 8;

    public static decimal ApplyDiscount(decimal total, decimal percent) =>
        Math.Round(total * (1 - percent / 100m), 2, MidpointRounding.AwayFromZero);

    // Seeded defect D-08: the note is silently cut to 255 characters.
    public static string TruncateNote(string text) =>
        text.Length <= NoteLimit ? text : text[..NoteLimit];
}
