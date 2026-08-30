using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHomeApi.DTOs;

/// <summary>
/// רשומה רפואית כפי ש-MedicalPage.tsx קורא אותה.
/// memberId חוזר כאובייקט ולא כמחרוזת — הדף קורא rec.memberId?._id לפילטור
/// לפי בן משפחה ו-rec.memberId?.name לתצוגה.
/// type יוצא כמחרוזת ולא כערך ה-enum הגולמי — המרה שנעשית ב-MedicalMapping.
/// </summary>
public record MedicalRecordDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Id,
    MedicalMemberDto? MemberId,
    string Type,
    string Title,
    DateTime Date,
    string? Doctor,
    string? Clinic,
    string? Notes,
    DateTime? NextAppointment,
    DateTime CreatedAt);

/// <summary>
/// בן המשפחה שהרשומה שייכת לו.
/// בניגוד ל-TaskUserDto/BudgetUserDto, כאן ה-client קורא גם _id ולא רק name,
/// כי הפילטור לפי בן משפחה נעשה בצד ה-client לפי המזהה.
/// </summary>
public record MedicalMemberDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Name);

public class CreateMedicalRecordRequest
{
    /// <summary>מזהה בן המשפחה. חובה — נבדק מול משק הבית ב-MedicalController.</summary>
    [Required(ErrorMessage = "יש לבחור בן משפחה")]
    public string MemberId { get; set; } = string.Empty;

    /// <summary>'appointment' | 'medication' | 'vaccine' | 'note' — נבדק ב-MedicalMapping.TryParseType.</summary>
    [Required(ErrorMessage = "נדרש סוג רשומה")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "נדרשת כותרת")]
    [MaxLength(200, ErrorMessage = "הכותרת ארוכה מדי")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "נדרש תאריך")]
    public DateTime? Date { get; set; }

    [MaxLength(100, ErrorMessage = "שם הרופא ארוך מדי")]
    public string? Doctor { get; set; }

    [MaxLength(100, ErrorMessage = "שם המרפאה ארוך מדי")]
    public string? Clinic { get; set; }

    [MaxLength(1000, ErrorMessage = "ההערות ארוכות מדי")]
    public string? Notes { get; set; }

    /// <summary>
    /// התור הבא. מוגדר string? ולא DateTime? כי ה-client שולח מחרוזת ריקה
    /// כשלא נבחר תאריך, ומחרוזת ריקה מפילה את פענוח כל גוף הבקשה ל-400.
    /// אותה תבנית בדיוק כמו CreateTaskRequest.DueDate.
    /// </summary>
    public string? NextAppointment { get; set; }

    /// <summary>
    /// סימון ה-checkbox בטופס — האם לשלוח התראת מייל לשאר בני הבית.
    /// נקרא ביצירה בלבד; בעדכון השדה מתקבל ומתעלמים ממנו.
    /// </summary>
    public bool SendEmailNotification { get; set; }
}

/// <summary>עדכון מלא של רשומה. אותם שדות כמו ביצירה.</summary>
public class UpdateMedicalRecordRequest : CreateMedicalRecordRequest;
