using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>
/// גמיפיקציה — לוח מובילים ואיפוס נקודות.
/// יושב על api/users אבל בנפרד מ-UsersController, כי זה מוגבל כולו למנהלים
/// ואילו לוח המובילים חייב להיות גלוי לכל בני הבית (גם LeaderboardPage וגם הדשבורד).
/// ApplicationUser לא נכלל ב-Global Query Filter, ולכן כל שאילתה כאן
/// מסננת במפורש לפי משק הבית של המשתמש המחובר.
/// הנקודות עצמן נצברות ב-TasksController.UpdateStatus — כאן רק קוראים ומאפסים.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class GamificationController : ControllerBase
{
    private const string AdminRoleLabel = "admin";
    private const string MemberRoleLabel = "member";

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _notifier;

    public GamificationController(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IRealtimeNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    /// <summary>
    /// לוח המובילים של הבית — כל בני הבית ממוינים לפי נקודות יורד.
    /// ה-client לא ממיין בעצמו (LeaderboardPage.tsx בונה את הפודיום מ-leaders[0..2]),
    /// ולכן הסדר שנקבע כאן הוא הסדר שנראה על המסך.
    /// </summary>
    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(IEnumerable<LeaderboardEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard()
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Ok(Array.Empty<LeaderboardEntryDto>());

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.HouseholdId == householdId)
            .OrderByDescending(u => u.Points)
            // שובר שוויון קבוע, כדי שהסדר לא יתחלף בין קריאות כשלשניים אותו ניקוד.
            .ThenBy(u => u.FullName)
            .ToListAsync();

        return Ok(users.Select(ToEntry));
    }

    /// <summary>
    /// איפוס נקודות לכל בני הבית.
    /// מנהל בלבד — כפתור האיפוס ב-LeaderboardPage.tsx מוצג רק למנהל,
    /// והכלל נאכף גם כאן ולא רק בממשק.
    /// האיפוס ידני בלבד; אין בפרויקט תשתית תזמון, ואיפוס שבועי אוטומטי נדחה לשלב נפרד.
    /// </summary>
    [HttpPost("reset-points")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPoints()
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        if (!_currentUser.IsManager)
            return Forbid();

        var users = await _db.Users
            .Where(u => u.HouseholdId == householdId)
            .ToListAsync();

        foreach (var user in users)
            user.Points = 0;

        await _db.SaveChangesAsync();

        // לוח מובילים פתוח אצל בן בית אחר חייב להתאפס גם הוא, בלי רענון.
        await _notifier.NotifyHouseholdAsync(householdId.Value, "gamification:reset", new { });

        return Ok(new MessageResponse("הנקודות אופסו"));
    }

    private static LeaderboardEntryDto ToEntry(ApplicationUser user) => new(
        user.Id,
        user.FullName,
        user.Points,
        user.IsManager ? AdminRoleLabel : MemberRoleLabel,
        user.ProfileImageUrl);
}
