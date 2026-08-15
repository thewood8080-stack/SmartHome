namespace SmartHomeApi.Services;

/// <summary>
/// שידור עדכונים בזמן אמת לכל בני משק בית מסוים.
/// עוטף את ה-Hub כדי שהקונטרולרים לא יהיו תלויים ישירות ב-SignalR.
/// </summary>
public interface IRealtimeNotifier
{
    /// <param name="householdId">משק הבית שיקבל את העדכון.</param>
    /// <param name="eventName">שם האירוע כפי שה-client מאזין לו, למשל 'shopping:created'.</param>
    /// <param name="payload">גוף ההודעה — DTO, לעולם לא ישות.</param>
    Task NotifyHouseholdAsync(int householdId, string eventName, object payload);
}
