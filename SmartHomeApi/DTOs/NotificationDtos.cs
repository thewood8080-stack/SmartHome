namespace SmartHomeApi.DTOs;

/// <summary>
/// התראה בודדת, מחושבת מחדש בכל קריאה ל-GET api/notifications.
/// אין לה ייצוג בבסיס הנתונים ואין מצב "נקראה" — היא נגזרת מהנתונים הקיימים.
/// </summary>
/// <param name="Type">'event' | 'gift' | 'inventory' | 'medical' | 'vehicle'</param>
/// <param name="Severity">'info' | 'warning' | 'urgent' — לפי הקרבה לתאריך או חומרת המצב</param>
/// <param name="Title">כותרת קצרה, בדרך כלל שם הישות</param>
/// <param name="Message">המשפט שמוצג למשתמש</param>
/// <param name="RelatedId">מזהה הישות שממנה נגזרה ההתראה, כמחרוזת — כמו שאר ה-DTOs מול ה-client</param>
/// <param name="Date">
/// התאריך שההתראה מדברת עליו — מועד האירוע, התור או פקיעת הטסט.
/// זהו גם מפתח המיון, ולכן הוא לעולם לא "עכשיו" עבור התראות עתידיות.
/// </param>
public record NotificationDto(
    string Type,
    string Severity,
    string Title,
    string Message,
    string RelatedId,
    DateTime Date);
