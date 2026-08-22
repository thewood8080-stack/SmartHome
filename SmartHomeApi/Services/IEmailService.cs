namespace SmartHomeApi.Services;

/// <summary>שליחת מיילים יוצאים מהמערכת.</summary>
public interface IEmailService
{
    /// <summary>
    /// שולח מייל איפוס סיסמה עם קישור לדף האיפוס ב-client.
    /// זורק חריגה אם השליחה נכשלה — הקורא אחראי להחליט מה להחזיר למשתמש.
    /// </summary>
    Task SendPasswordResetAsync(string toEmail, string toName, string resetLink);
}
