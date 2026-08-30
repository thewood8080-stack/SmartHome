namespace SmartHomeApi.Services;

/// <summary>שליחת מיילים יוצאים מהמערכת.</summary>
public interface IEmailService
{
    /// <summary>
    /// שולח מייל איפוס סיסמה עם קישור לדף האיפוס ב-client.
    /// זורק חריגה אם השליחה נכשלה — הקורא אחראי להחליט מה להחזיר למשתמש.
    /// </summary>
    Task SendPasswordResetAsync(string toEmail, string toName, string resetLink);

    /// <summary>
    /// שולח לבן בית אחד התראה על פריט חדש שנוצר בבית.
    /// זורק חריגה אם השליחה נכשלה — הקורא אחראי להחליט מה להחזיר למשתמש.
    /// </summary>
    Task SendNewItemNotificationAsync(string toEmail, string toName, NewItemNotification notification);
}

/// <summary>
/// תוכן ההתראה על פריט חדש. הטקסטים נקבעים בקונטרולר שיצר את הפריט,
/// כדי שגוף המייל יישאר זהה בכל המודולים ששולחים התראה.
/// </summary>
/// <param name="Kind">סוג הפריט בעברית — למשל "משימה חדשה". משמש גם ככותרת המייל.</param>
/// <param name="ItemTitle">שם הפריט או כותרת המשימה.</param>
/// <param name="CreatedByName">שם מי שהוסיף את הפריט.</param>
/// <param name="Details">שורת פירוט נוספת, או null כשאין מה להוסיף.</param>
public record NewItemNotification(
    string Kind,
    string ItemTitle,
    string CreatedByName,
    string? Details = null);
