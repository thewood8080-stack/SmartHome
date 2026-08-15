using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SmartHomeApi.Services;

namespace SmartHomeApi.Hubs;

/// <summary>
/// ערוץ העדכונים בזמן אמת.
/// כל חיבור משויך אוטומטית לקבוצת משק הבית של המשתמש המחובר, ולכן שידור
/// לקבוצה מגיע לבני אותו בית בלבד — אותה הפרדה שה-Global Query Filter מבצע על הנתונים.
/// </summary>
[Authorize]
public class SmartHomeHub : Hub
{
    private readonly ICurrentUserService _currentUser;

    public SmartHomeHub(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// שם הקבוצה נגזר בשרת בלבד.
    /// ה-client לא שולח 'join' ולא יכול לבקש להצטרף לבית של מישהו אחר.
    /// </summary>
    public static string GroupName(int householdId) => $"household-{householdId}";

    public override async Task OnConnectedAsync()
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(householdId.Value));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(householdId.Value));

        await base.OnDisconnectedAsync(exception);
    }
}
