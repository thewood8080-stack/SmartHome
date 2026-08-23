using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHomeApi.DTOs;

/// <summary>
/// תנועה בתקציב כפי ש-BudgetPage.tsx קורא אותה.
/// type יוצא כמחרוזת 'income'/'expense' ולא כערך ה-enum הגולמי —
/// המרה שנעשית ב-BudgetMapping, כדי שהמודל לא יכיר את המחרוזות של ה-client.
/// </summary>
public record BudgetDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Id,
    string Title,
    decimal Amount,
    string Type,
    string Category,
    DateTime Date,
    string? Note,
    BudgetUserDto? AddedBy,
    DateTime CreatedAt);

/// <summary>מי הוסיף את התנועה. ה-client קורא name בלבד.</summary>
public record BudgetUserDto(string Name);

public class CreateBudgetRequest
{
    [Required(ErrorMessage = "נדרשת כותרת")]
    [MaxLength(200, ErrorMessage = "הכותרת ארוכה מדי")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// nullable כדי ש-[Required] יזהה בקשה בלי סכום ויחזיר 400,
    /// במקום ש-decimal ריק ייפול בשקט ל-0.
    /// </summary>
    [Required(ErrorMessage = "נדרש סכום")]
    [Range(0.01, 100_000_000, ErrorMessage = "הסכום חייב להיות גדול מאפס")]
    public decimal? Amount { get; set; }

    /// <summary>'income' או 'expense' — נבדק ב-BudgetMapping.TryParseType.</summary>
    [Required(ErrorMessage = "נדרש סוג תנועה")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "נדרשת קטגוריה")]
    [MaxLength(50, ErrorMessage = "הקטגוריה ארוכה מדי")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "נדרש תאריך")]
    public DateTime? Date { get; set; }

    [MaxLength(500, ErrorMessage = "ההערה ארוכה מדי")]
    public string? Note { get; set; }
}

/// <summary>עדכון מלא של תנועה. אותם שדות כמו ביצירה.</summary>
public class UpdateBudgetRequest : CreateBudgetRequest;
