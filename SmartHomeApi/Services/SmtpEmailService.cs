using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace SmartHomeApi.Services;

/// <summary>
/// שליחת מייל דרך שרת SMTP חיצוני, מבוסס MailKit.
/// פרטי ההתחברות (Host, Port, Username, Password) נקראים מ-User Secrets בלבד —
/// כמו ב-<see cref="CloudinaryService"/>. From ו-FromName אינם סודיים ויושבים ב-appsettings.json.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string toEmail, string toName, string resetLink)
    {
        var settings = ReadSettings();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.From));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "איפוס סיסמה — SmartHome";

        message.Body = new BodyBuilder
        {
            HtmlBody = BuildHtmlBody(toName, resetLink),
            // גרסת טקסט לקוראי מייל שלא מציגים HTML.
            TextBody = BuildTextBody(toName, resetLink)
        }.ToMessageBody();

        await SendAsync(settings, message);

        // הכתובת נרשמת ללוג, הקישור והטוקן לא.
        _logger.LogInformation("נשלח מייל איפוס סיסמה אל {Email}", toEmail);
    }

    public async Task SendNewItemNotificationAsync(string toEmail, string toName, NewItemNotification notification)
    {
        var settings = ReadSettings();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.From));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"{notification.Kind} — SmartHome";

        message.Body = new BodyBuilder
        {
            HtmlBody = BuildNotificationHtmlBody(toName, notification),
            // גרסת טקסט לקוראי מייל שלא מציגים HTML.
            TextBody = BuildNotificationTextBody(toName, notification)
        }.ToMessageBody();

        await SendAsync(settings, message);

        _logger.LogInformation("נשלחה התראת מייל על {Kind} אל {Email}", notification.Kind, toEmail);
    }

    /// <summary>ההתחברות והשליחה עצמן — משותפות לכל סוגי המיילים.</summary>
    private static async Task SendAsync(EmailSettings settings, MimeMessage message)
    {
        using var client = new SmtpClient();

        try
        {
            // StartTlsWhenAvailable מכסה גם פורט 587 (STARTTLS) וגם 465 (SSL ישיר).
            await client.ConnectAsync(settings.Host, settings.Port, SecureSocketOptions.StartTlsWhenAvailable);
            await client.AuthenticateAsync(settings.Username, settings.Password);
            await client.SendAsync(message);
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true);
        }
    }

    /// <summary>
    /// קורא את הגדרות ה-SMTP. הבדיקה נעשית בזמן שליחה ולא בבנאי, כדי ששאר
    /// פעולות האימות (כניסה, הרשמה) ימשיכו לעבוד גם כשפרטי ה-SMTP לא הוגדרו.
    /// </summary>
    private EmailSettings ReadSettings()
    {
        var host = _configuration["Email:Host"];
        var portRaw = _configuration["Email:Port"];
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(portRaw) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            // ההודעה מציינת אילו הגדרות חסרות — בלי הערכים עצמם.
            throw new InvalidOperationException(
                "חסרים פרטי SMTP. הגדר Email:Host, Email:Port, Email:Username ו-Email:Password ב-User Secrets.");
        }

        if (!int.TryParse(portRaw, out var port))
            throw new InvalidOperationException("Email:Port חייב להיות מספר");

        var fromName = _configuration["Email:FromName"];
        if (string.IsNullOrWhiteSpace(fromName))
            fromName = "SmartHome";

        // אם לא הוגדרה כתובת שולח — שולחים מחשבון ההתחברות עצמו.
        // ספקים רבים (Gmail למשל) ממילא דוחים From שאינו החשבון המאומת.
        var from = _configuration["Email:From"];
        if (string.IsNullOrWhiteSpace(from))
            from = username;

        return new EmailSettings(host, port, username, password, from, fromName);
    }

    /// <summary>גוף המייל — עברית, RTL, בצבעי המערכת.</summary>
    private static string BuildHtmlBody(string name, string resetLink) => $$"""
        <div dir="rtl" style="font-family:Arial,Helvetica,sans-serif;background:#F8F6F0;padding:24px;">
          <div style="max-width:520px;margin:0 auto;background:#ffffff;border-radius:16px;padding:28px;">
            <h1 style="color:#1E3A5F;font-size:22px;margin:0 0 4px;">SmartHome</h1>
            <p style="color:#2C3E50;font-size:15px;margin:0 0 20px;">איפוס סיסמה</p>

            <p style="color:#2C3E50;font-size:15px;line-height:1.7;">
              שלום {{name}},<br />
              קיבלנו בקשה לאיפוס הסיסמה שלך. לחיצה על הכפתור תוביל לדף שבו אפשר לבחור סיסמה חדשה.
            </p>

            <p style="text-align:center;margin:28px 0;">
              <a href="{{resetLink}}"
                 style="display:inline-block;background:#1E3A5F;color:#ffffff;text-decoration:none;
                        padding:12px 32px;border-radius:10px;font-size:15px;font-weight:bold;">
                איפוס הסיסמה
              </a>
            </p>

            <p style="color:#2C3E50;font-size:13px;line-height:1.7;">
              הקישור תקף ל-30 דקות בלבד.<br />
              אם לא ביקשת לאפס סיסמה — אפשר להתעלם מהמייל הזה, הסיסמה הנוכחית תישאר כפי שהיא.
            </p>

            <p style="color:#7F8C8D;font-size:12px;line-height:1.7;margin-top:20px;
                      border-top:1px solid #F8F6F0;padding-top:14px;">
              אם הכפתור לא עובד, אפשר להעתיק את הכתובת הזו לדפדפן:<br />
              <span style="color:#C9A84C;word-break:break-all;">{{resetLink}}</span>
            </p>
          </div>
        </div>
        """;

    private static string BuildTextBody(string name, string resetLink) => $"""
        שלום {name},

        קיבלנו בקשה לאיפוס הסיסמה שלך ב-SmartHome.
        לאיפוס הסיסמה יש להיכנס לקישור הבא:

        {resetLink}

        הקישור תקף ל-30 דקות בלבד.
        אם לא ביקשת לאפס סיסמה — אפשר להתעלם מההודעה, הסיסמה הנוכחית תישאר כפי שהיא.

        SmartHome
        """;

    /// <summary>
    /// גוף התראת הפריט החדש — עברית, RTL, בצבעי המערכת.
    /// כותרת הפריט ושמות המשתמשים הם קלט חופשי, ולכן הם מקודדים לפני ההשתלה ב-HTML.
    /// </summary>
    private static string BuildNotificationHtmlBody(string name, NewItemNotification notification)
    {
        var kind = Encode(notification.Kind);
        var itemTitle = Encode(notification.ItemTitle);
        var createdBy = Encode(notification.CreatedByName);

        var detailsRow = string.IsNullOrWhiteSpace(notification.Details)
            ? string.Empty
            : $"""
               <p style="color:#2C3E50;font-size:14px;margin:0 0 6px;">{Encode(notification.Details)}</p>
               """;

        return $"""
            <div dir="rtl" style="font-family:Arial,Helvetica,sans-serif;background:#F8F6F0;padding:24px;">
              <div style="max-width:520px;margin:0 auto;background:#ffffff;border-radius:16px;padding:28px;">
                <h1 style="color:#1E3A5F;font-size:22px;margin:0 0 4px;">SmartHome</h1>
                <p style="color:#2C3E50;font-size:15px;margin:0 0 20px;">{kind}</p>

                <p style="color:#2C3E50;font-size:15px;line-height:1.7;margin:0 0 14px;">
                  שלום {Encode(name)},<br />
                  {kind} ברשימות הבית.
                </p>

                <div style="background:#F8F6F0;border-radius:10px;padding:16px;">
                  <p style="color:#1E3A5F;font-size:17px;font-weight:bold;margin:0 0 6px;">{itemTitle}</p>
                  {detailsRow}
                  <p style="color:#7F8C8D;font-size:13px;margin:0;">מאת {createdBy}</p>
                </div>

                <p style="color:#7F8C8D;font-size:12px;line-height:1.7;margin-top:20px;
                          border-top:1px solid #F8F6F0;padding-top:14px;">
                  קיבלת את ההודעה הזו כי מי שהוסיף את הפריט ביקש לעדכן את בני הבית.
                </p>
              </div>
            </div>
            """;
    }

    private static string BuildNotificationTextBody(string name, NewItemNotification notification)
    {
        var details = string.IsNullOrWhiteSpace(notification.Details)
            ? string.Empty
            : $"{notification.Details}{Environment.NewLine}";

        return $"""
            שלום {name},

            {notification.Kind} ברשימות הבית:

            {notification.ItemTitle}
            {details}מאת {notification.CreatedByName}

            SmartHome
            """;
    }

    /// <summary>קידוד HTML לקלט חופשי — מונע שבירה של גוף המייל.</summary>
    private static string Encode(string? value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    /// <summary>הגדרות השליחה אחרי אימות — נשארות בזיכרון הבקשה בלבד.</summary>
    private record EmailSettings(
        string Host,
        int Port,
        string Username,
        string Password,
        string From,
        string FromName);
}
