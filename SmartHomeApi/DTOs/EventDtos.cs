using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHomeApi.DTOs;

/// <summary>
/// אירוע בלוח השנה כפי ש-CalendarPage.tsx קורא אותו.
/// המודל שומר StartDate, אבל ה-client שולח וקורא בדיוק 'date' —
/// ולכן השם מפורש כאן, באותה שיטה שבה urgent/bought מגושרים ב-ShoppingItemDto.
/// </summary>
public record EventDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Id,
    string Title,
    string Description,
    [property: JsonPropertyName("date")] DateTime StartDate,
    bool AllDay,
    string Color,
    EventUserDto? CreatedBy,
    DateTime CreatedAt);

/// <summary>יוצר האירוע. ה-client קורא רק את ev.createdBy.name.</summary>
public record EventUserDto(string Name);

public class CreateEventRequest
{
    [Required(ErrorMessage = "נדרשת כותרת לאירוע")]
    [MaxLength(200, ErrorMessage = "כותרת האירוע ארוכה מדי")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// ה-client שולח 'date' ולא 'startDate'.
    /// nullable כדי ש-[Required] יזהה בקשה בלי תאריך ויחזיר 400,
    /// במקום ש-DateTime ריק ייפול בשקט ל-DateTime.MinValue.
    /// </summary>
    [Required(ErrorMessage = "נדרש תאריך לאירוע")]
    [JsonPropertyName("date")]
    public DateTime? StartDate { get; set; }

    /// <summary>הטופס ב-client לא מציג בורר שעה נפרד, ולכן ברירת המחדל היא אירוע יום שלם.</summary>
    public bool AllDay { get; set; } = true;

    [MaxLength(7, ErrorMessage = "קוד הצבע אינו תקין")]
    public string? Color { get; set; }
}

/// <summary>עדכון מלא של אירוע. אותם שדות כמו ביצירה.</summary>
public class UpdateEventRequest : CreateEventRequest;
