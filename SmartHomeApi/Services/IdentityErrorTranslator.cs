using Microsoft.AspNetCore.Identity;

namespace SmartHomeApi.Services;

/// <summary>
/// מתרגם שגיאות Identity לעברית. Identity מחזיר טקסט באנגלית,
/// וה-client מציג את ההודעה כמו שהיא — לכן אסור להעביר אותה הלאה.
/// </summary>
public static class IdentityErrorTranslator
{
    /// <summary>מדיניות הסיסמאות כפי שהוגדרה ב-Program.cs — הטקסט חייב לשקף אותה.</summary>
    public const string PasswordPolicy = "הסיסמה חייבת להכיל לפחות 8 תווים, אות גדולה, אות קטנה וספרה";

    private static readonly Dictionary<string, string> Messages = new()
    {
        ["DuplicateEmail"] = "כתובת המייל כבר רשומה",
        ["DuplicateUserName"] = "כתובת המייל כבר רשומה",
        ["InvalidEmail"] = "כתובת מייל לא תקינה",
        ["InvalidUserName"] = "כתובת מייל לא תקינה",
        ["PasswordTooShort"] = "הסיסמה קצרה מדי — נדרשים לפחות 8 תווים",
        ["PasswordRequiresUpper"] = "הסיסמה חייבת להכיל אות גדולה באנגלית",
        ["PasswordRequiresLower"] = "הסיסמה חייבת להכיל אות קטנה באנגלית",
        ["PasswordRequiresDigit"] = "הסיסמה חייבת להכיל ספרה",
        ["PasswordRequiresNonAlphanumeric"] = "הסיסמה חייבת להכיל תו מיוחד",
        ["PasswordRequiresUniqueChars"] = "הסיסמה חייבת להכיל יותר תווים שונים",
        ["UserAlreadyHasPassword"] = "למשתמש כבר מוגדרת סיסמה",
        ["PasswordMismatch"] = "מייל או סיסמה שגויים"
    };

    /// <summary>
    /// מאחד את כל השגיאות למשפט אחד בעברית.
    /// כמה דרישות סיסמה שנכשלו יחד מאוחדות למשפט המדיניות המלא,
    /// כדי לא להטיח במשתמש רשימת שגיאות.
    /// </summary>
    public static string Translate(IEnumerable<IdentityError> errors)
    {
        var codes = errors.Select(e => e.Code).Distinct().ToList();
        if (codes.Count == 0)
            return "הפעולה נכשלה";

        var passwordCodes = codes.Where(c => c.StartsWith("Password", StringComparison.Ordinal)).ToList();
        var otherCodes = codes.Except(passwordCodes).ToList();

        var parts = new List<string>();

        if (passwordCodes.Count == 1)
            parts.Add(Messages.TryGetValue(passwordCodes[0], out var single) ? single : PasswordPolicy);
        else if (passwordCodes.Count > 1)
            parts.Add(PasswordPolicy);

        parts.AddRange(otherCodes.Select(code =>
            Messages.TryGetValue(code, out var message) ? message : "הפעולה נכשלה"));

        return string.Join(". ", parts.Distinct());
    }
}
