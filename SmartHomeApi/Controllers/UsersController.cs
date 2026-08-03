using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>
/// ניהול חברי הבית — למנהל בלבד.
/// ApplicationUser לא נכלל ב-Global Query Filter, ולכן כל שאילתה כאן
/// מסננת במפורש לפי משק הבית של המנהל המחובר.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = DbSeeder.ManagerRole)]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private const string AdminRoleLabel = "admin";
    private const string MemberRoleLabel = "member";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAppStatsService _stats;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IAppStatsService stats)
    {
        _userManager = userManager;
        _db = db;
        _currentUser = currentUser;
        _stats = stats;
    }

    /// <summary>כל חברי הבית — הממתינים לאישור ראשונים.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Ok(Array.Empty<UserListItemDto>());

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.HouseholdId == householdId)
            .OrderBy(u => u.IsApproved)
            .ThenBy(u => u.FullName)
            .ToListAsync();

        return Ok(users.Select(ToListItem));
    }

    /// <summary>אישור או חסימה של חבר בית.</summary>
    [HttpPatch("{id}/approve")]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(string id, ApproveUserRequest request)
    {
        var (user, error) = await FindInHouseholdAsync(id);
        if (error is not null) return error;

        var approved = request.Approved!.Value;

        if (!approved && user!.Id == _currentUser.UserId)
            return BadRequest(new MessageResponse("אי אפשר לחסום את עצמך"));

        user!.IsApproved = approved;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new MessageResponse(IdentityErrorTranslator.Translate(result.Errors)));

        // מבטל את העוגייה הקיימת של המשתמש שנחסם, כדי שהחסימה תיכנס לתוקף.
        if (!approved)
        {
            await _userManager.UpdateSecurityStampAsync(user);
            _stats.RemoveActiveUser(user.Id);
        }

        return Ok(ToListItem(user));
    }

    /// <summary>שינוי תפקיד — admin או member.</summary>
    [HttpPatch("{id}/role")]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(string id, ChangeRoleRequest request)
    {
        var requested = request.Role?.Trim().ToLowerInvariant();
        if (requested is not (AdminRoleLabel or MemberRoleLabel))
            return BadRequest(new MessageResponse("תפקיד לא מוכר"));

        var (user, error) = await FindInHouseholdAsync(id);
        if (error is not null) return error;

        var makeManager = requested == AdminRoleLabel;

        // הורדת ההרשאות של עצמך עלולה להשאיר את הבית בלי מנהל.
        if (!makeManager && user!.Id == _currentUser.UserId)
            return BadRequest(new MessageResponse("אי אפשר להסיר לעצמך הרשאות ניהול"));

        if (user!.IsManager == makeManager)
            return Ok(ToListItem(user));

        var addTo = makeManager ? DbSeeder.ManagerRole : DbSeeder.MemberRole;
        var removeFrom = makeManager ? DbSeeder.MemberRole : DbSeeder.ManagerRole;

        if (await _userManager.IsInRoleAsync(user, removeFrom))
        {
            var removeResult = await _userManager.RemoveFromRoleAsync(user, removeFrom);
            if (!removeResult.Succeeded)
                return BadRequest(new MessageResponse(IdentityErrorTranslator.Translate(removeResult.Errors)));
        }

        if (!await _userManager.IsInRoleAsync(user, addTo))
        {
            var addResult = await _userManager.AddToRoleAsync(user, addTo);
            if (!addResult.Succeeded)
                return BadRequest(new MessageResponse(IdentityErrorTranslator.Translate(addResult.Errors)));
        }

        user.IsManager = makeManager;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(new MessageResponse(IdentityErrorTranslator.Translate(updateResult.Errors)));

        // התפקיד יושב בעוגייה — בלי רענון החותם ההרשאה הישנה תמשיך לתפוס.
        await _userManager.UpdateSecurityStampAsync(user);

        return Ok(ToListItem(user));
    }

    /// <summary>מחיקת חבר בית.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var (user, error) = await FindInHouseholdAsync(id);
        if (error is not null) return error;

        if (user!.Id == _currentUser.UserId)
            return BadRequest(new MessageResponse("אי אפשר למחוק את עצמך"));

        var userId = user.Id;

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new MessageResponse(IdentityErrorTranslator.Translate(result.Errors)));

        _stats.RemoveActiveUser(userId);

        return Ok(new MessageResponse("המשתמש נמחק"));
    }

    /// <summary>מאתר משתמש בתוך משק הבית של המנהל בלבד — בידוד בין בתים.</summary>
    private async Task<(ApplicationUser? User, IActionResult? Error)> FindInHouseholdAsync(string id)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return (null, NotFound(new MessageResponse("משתמש לא נמצא")));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.HouseholdId == householdId);

        return user is null
            ? (null, NotFound(new MessageResponse("משתמש לא נמצא")))
            : (user, null);
    }

    private static UserListItemDto ToListItem(ApplicationUser user) => new(
        user.Id,
        user.Id,
        user.FullName,
        user.Email ?? string.Empty,
        user.IsManager ? AdminRoleLabel : MemberRoleLabel,
        user.IsApproved,
        user.Points,
        user.CreatedAt);
}
