using SmartHomeApi.Models;

namespace SmartHomeApi.DTOs;

/// <summary>
/// כל התרגום בין ה-enum של המודל למחרוזות שה-client מכיר.
/// מרוכז במקום אחד כדי שלא יהיו שתי גרסאות של אותו מיפוי.
/// </summary>
public static class BudgetMapping
{
    private static readonly Dictionary<BudgetType, string> TypeToClient = new()
    {
        [BudgetType.Income] = "income",
        [BudgetType.Expense] = "expense"
    };

    private static readonly Dictionary<string, BudgetType> ClientToType =
        TypeToClient.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase);

    public static string ToClientType(BudgetType type) => TypeToClient[type];

    /// <summary>מתרגם 'income'/'expense'. מחרוזת ריקה נופלת לברירת המחדל.</summary>
    public static bool TryParseType(string? value, out BudgetType type)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            type = BudgetType.Expense;
            return true;
        }

        return ClientToType.TryGetValue(value.Trim(), out type);
    }

    public static BudgetDto ToDto(BudgetEntry entry) => new(
        entry.Id.ToString(),
        entry.Id.ToString(),
        entry.Title,
        entry.Amount,
        ToClientType(entry.Type),
        entry.Category,
        entry.Date,
        entry.Note,
        ToUserDto(entry.AddedBy),
        entry.CreatedAt);

    private static BudgetUserDto? ToUserDto(ApplicationUser? user) => user is null
        ? null
        : new BudgetUserDto(user.FullName);

    /// <summary>ה-client שולח מחרוזת ריקה כשהשדה לא מולא.</summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
