using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHomeApi.DTOs;

/// <summary>
/// רכב כפי ש-VehiclePage.tsx קורא אותו.
/// שמות השדות כאן הם שמות ה-client ולא שמות הישות — הגישור נעשה כולו כאן
/// וב-VehicleMapping, בלי לגעת במודל ובלי migration:
///   Vehicle.LicensePlate     → plateNumber
///   Vehicle.InsuranceExpiry  → insurance
///   Vehicle.TestExpiry       → test
/// </summary>
public record VehicleDto(
    [property: JsonPropertyName("_id")] string LegacyId,
    string Id,
    string Name,
    string PlateNumber,
    // דגם. קיים בישות אבל אין לו שדה בטופס ב-VehiclePage.tsx, ולכן הוא חוזר
    // תמיד null. מוחזר כדי לא להשמיט שדה קיים מהישות.
    string? Model,
    int? Year,
    DateTime? LastService,
    DateTime? NextService,
    DateTime? Insurance,
    DateTime? Test,
    string? FuelType,
    string? Notes,
    VehicleUserDto? AddedBy,
    DateTime CreatedAt);

/// <summary>מי הוסיף את הרכב. ה-client קורא name בלבד.</summary>
public record VehicleUserDto(string Name);

public class CreateVehicleRequest
{
    [Required(ErrorMessage = "נדרש שם רכב")]
    [MaxLength(100, ErrorMessage = "שם הרכב ארוך מדי")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// לוחית רישוי. אופציונלי במכוון — ה-input ב-VehiclePage.tsx:83 הוא בלי
    /// required, והטופס נשלח ב-spread מלא ולכן שולח מחרוזת ריקה כשלא מולא.
    /// [Required] היה פוסל מחרוזת ריקה (AllowEmptyStrings=false) ומחזיר 400
    /// על כל רכב שנוסף בלי לוחית.
    /// העמודה עצמה היא NOT NULL, ולכן ריק נשמר כמחרוזת ריקה ולא כ-null.
    /// </summary>
    [MaxLength(20, ErrorMessage = "מספר הרישוי ארוך מדי")]
    public string? PlateNumber { get; set; }

    /// <summary>ה-client שולח undefined אמיתי כשהשדה ריק, ולכן int? נקשר כרגיל.</summary>
    public int? Year { get; set; }

    /// <summary>
    /// ארבעת התאריכים מוגדרים string? ולא DateTime? כי כל ה-inputs מסוג date
    /// ב-VehiclePage.tsx הם בלי required ונשלחים כמחרוזת ריקה כשלא נבחר תאריך,
    /// ומחרוזת ריקה מפילה את פענוח כל גוף הבקשה ל-400.
    /// אותה תבנית בדיוק כמו CreateMedicalRecordRequest.NextAppointment.
    /// </summary>
    public string? LastService { get; set; }

    public string? NextService { get; set; }

    public string? Insurance { get; set; }

    public string? Test { get; set; }

    [MaxLength(30, ErrorMessage = "סוג הדלק ארוך מדי")]
    public string? FuelType { get; set; }

    [MaxLength(1000, ErrorMessage = "ההערות ארוכות מדי")]
    public string? Notes { get; set; }
}

/// <summary>עדכון מלא של רכב. אותם שדות כמו ביצירה.</summary>
public class UpdateVehicleRequest : CreateVehicleRequest;
