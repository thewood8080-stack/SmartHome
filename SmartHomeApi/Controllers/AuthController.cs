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
/// הרשמה, כניסה, יציאה ופרופיל.
/// האימות עצמו מבוסס עוגיית הזדהות + Session — לא טוקן.
/// שדה ה-token בתשובה הוא מזהה הסשן, ונשמר כדי לא לשבור את ה-client הקיים.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAppStatsService _stats;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IAppStatsService stats,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _currentUser = currentUser;
        _stats = stats;
        _logger = logger;
    }

    /// <summary>הרשמה — יצירת בית חדש (עם householdName) או הצטרפות לבית קיים (עם inviteCode).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
            return BadRequest(new MessageResponse("כתובת המייל כבר רשומה"));

        // בדיקת הסיסמה לפני יצירת משק הבית — אחרת סיסמה חלשה משאירה בית יתום.
        var passwordError = await ValidatePasswordAsync(request.Password);
        if (passwordError is not null)
            return BadRequest(new MessageResponse(passwordError));

        var isJoining = !string.IsNullOrWhiteSpace(request.InviteCode);

        if (!isJoining && string.IsNullOrWhiteSpace(request.HouseholdName))
            return BadRequest(new MessageResponse("נדרש שם הבית"));

        Household household;

        if (isJoining)
        {
            var code = request.InviteCode!.Trim().ToUpperInvariant();
            var existing = await _db.Households.FirstOrDefaultAsync(h => h.InviteCode == code);
            if (existing is null)
                return BadRequest(new MessageResponse("קוד הצטרפות שגוי"));

            household = existing;
        }
        else
        {
            household = new Household
            {
                Name = request.HouseholdName!.Trim(),
                InviteCode = await GenerateUniqueInviteCodeAsync()
            };
        }

        // מייסד הבית הוא המנהל שלו ומאושר מיד; מצטרף ממתין לאישור.
        var isFounder = !isJoining;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = request.Name.Trim(),
            IsApproved = isFounder,
            IsManager = isFounder
        };

        // הבית והמשתמש נכנסים יחד או בכלל לא — אחרת נשאר בית בלי מנהל.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        if (!isJoining)
        {
            _db.Households.Add(household);
            await _db.SaveChangesAsync();
        }

        user.HouseholdId = household.Id;

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(new MessageResponse(IdentityErrorTranslator.Translate(createResult.Errors)));
        }

        var role = isFounder ? DbSeeder.ManagerRole : DbSeeder.MemberRole;
        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            _logger.LogError("שיוך תפקיד נכשל בהרשמה: {Errors}",
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            return BadRequest(new MessageResponse("ההרשמה נכשלה, נסה שנית"));
        }

        await transaction.CommitAsync();

        // מצטרף שטרם אושר לא מקבל סשן — הוא עדיין לא רשאי להיכנס.
        if (!user.IsApproved)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                new AuthResponse(string.Empty, ToUserDto(user), ToHouseholdDto(household)));
        }

        var token = await StartSessionAsync(user);

        return StatusCode(
            StatusCodes.Status201Created,
            new AuthResponse(token, ToUserDto(user), ToHouseholdDto(household)));
    }

    /// <summary>כניסה עם מייל וסיסמה.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return BadRequest(new MessageResponse("מייל או סיסמה שגויים"));

        // lockoutOnFailure: false — נעילת חשבון תיכנס בשלב האבטחה ולא כאן.
        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!passwordCheck.Succeeded)
            return BadRequest(new MessageResponse("מייל או סיסמה שגויים"));

        if (!user.IsApproved)
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new MessageResponse("ממתין לאישור מנהל הבית"));

        var household = user.HouseholdId.HasValue
            ? await _db.Households.FirstOrDefaultAsync(h => h.Id == user.HouseholdId.Value)
            : null;

        var token = await StartSessionAsync(user);

        return Ok(new AuthResponse(token, ToUserDto(user), ToHouseholdDto(household)));
    }

    /// <summary>יציאה — מבטלת את העוגייה ואת הסשן.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var userId = _currentUser.UserId;

        if (userId is not null)
            _stats.RemoveActiveUser(userId);

        await _signInManager.SignOutAsync();
        _currentUser.Clear();

        return Ok(new MessageResponse("התנתקת בהצלחה"));
    }

    /// <summary>פרטי המשתמש המחובר.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized(new MessageResponse("נדרשת התחברות"));

        var household = user.HouseholdId.HasValue
            ? await _db.Households.FirstOrDefaultAsync(h => h.Id == user.HouseholdId.Value)
            : null;

        return Ok(new AuthResponse(
            HttpContext.Session.Id, ToUserDto(user), ToHouseholdDto(household)));
    }

    /// <summary>
    /// מוציא עוגיית הזדהות ושומר ב-Session את משק הבית ואת שם המשתמש,
    /// כדי שבקשות הבאות לא ישלפו את המשתמש מבסיס הנתונים.
    /// </summary>
    private async Task<string> StartSessionAsync(ApplicationUser user)
    {
        await _signInManager.SignInAsync(user, isPersistent: true);

        _currentUser.SetHousehold(user.HouseholdId);
        _currentUser.SetFullName(user.FullName);
        _stats.TrackActiveUser(user.Id);

        return HttpContext.Session.Id;
    }

    /// <summary>מריץ את מדיניות הסיסמאות של Identity ומחזיר הודעה בעברית, או null אם תקין.</summary>
    private async Task<string?> ValidatePasswordAsync(string password)
    {
        // הבודקים המובנים לא נוגעים במשתמש עצמו, ולכן מספיק מופע ריק לבדיקה מוקדמת.
        var probe = new ApplicationUser();

        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, probe, password);
            if (!result.Succeeded)
                return IdentityErrorTranslator.Translate(result.Errors);
        }

        return null;
    }

    private async Task<string> GenerateUniqueInviteCodeAsync()
    {
        while (true)
        {
            var code = Household.GenerateInviteCode();
            if (!await _db.Households.AnyAsync(h => h.InviteCode == code))
                return code;
        }
    }

    private static UserDto ToUserDto(ApplicationUser user) => new(
        user.Id,
        user.FullName,
        user.Email ?? string.Empty,
        user.IsManager ? "admin" : "member",
        user.IsApproved,
        user.Points);

    private static HouseholdDto? ToHouseholdDto(Household? household) => household is null
        ? null
        : new HouseholdDto(household.Id.ToString(), household.Name, household.InviteCode);
}
