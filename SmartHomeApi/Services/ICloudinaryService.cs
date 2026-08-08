using SmartHomeApi.DTOs;

namespace SmartHomeApi.Services;

/// <summary>
/// כל הגישה ל-Cloudinary עוברת כאן. המפתחות נקראים מ-User Secrets בלבד
/// ולא יוצאים מהשירות — לא ב-URL, לא בלוג ולא בתשובה ללקוח.
/// </summary>
public interface ICloudinaryService
{
    /// <summary>
    /// מעלה תמונה. בהצלחה — Result מלא ו-Error ריק.
    /// בכישלון — Result ריק ו-Error מכיל סיבה בעברית שאפשר להציג למשתמש.
    /// </summary>
    Task<(PhotoUploadResultDto? Result, string? Error)> UploadImageAsync(IFormFile file);

    /// <summary>
    /// מעלה קובץ raw (PDF). מחזיר url ו-publicId; אין ממדים.
    /// נדרש בשלב 8ב.
    /// </summary>
    Task<(PhotoUploadResultDto? Result, string? Error)> UploadRawAsync(IFormFile file);

    /// <summary>
    /// מוחק קובץ מ-Cloudinary. מחזיר true גם כשהקובץ כבר לא קיים שם,
    /// כי אחרת רשומה יתומה בבסיס הנתונים לא הייתה ניתנת למחיקה לעולם.
    /// </summary>
    Task<bool> DeleteAsync(string publicId, bool isRaw);
}
