using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    /// <summary>מקסימום בקשות איפוס סיסמה לאותה כתובת מייל בתוך חלון הזמן.</summary>
    private const int MaxResetRequestsPerWindow = 3;

    private static readonly TimeSpan ResetRequestWindow = TimeSpan.FromHours(1);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAppStatsService _stats;
    private readonly IEmailService _email;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IAppStatsService stats,
        IEmailService email,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _currentUser = currentUser;
        _stats = stats;
        _email = email;
        _cache = cache;
        _configuration = configuration;
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

    /// <summary>
    /// שליחת מייל לאיפוס סיסמה.
    /// התשובה זהה תמיד — 200 ואותה הודעה — בין אם הכתובת רשומה ובין אם לא,
    /// אחרת אפשר היה לגלות בניסוי וטעייה אילו כתובות מייל קיימות במערכת.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var email = request.Email.Trim();

        // ההודעה הגנרית היא התשובה היחידה שיוצאת מכאן, בכל אחד מהמסלולים.
        var genericResponse = Ok(new MessageResponse(
            "אם הכתובת קיימת במערכת, נשלח אליה מייל לאיפוס סיסמה"));

        _logger.LogInformation("בקשת איפוס סיסמה עבור {Email} בשעה {At:o}", email, DateTime.UtcNow);

        // המונה עולה על כל בקשה, גם לכתובת שאינה רשומה — אחרת עצם החסימה הייתה
        // מסגירה שהכתובת קיימת.
        if (!TryCountResetRequest(email))
        {
            _logger.LogWarning("בקשת איפוס סיסמה נחסמה בהגבלת קצב עבור {Email}", email);
            return genericResponse;
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return genericResponse;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var clientBaseUrl = (_configuration["ClientBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var resetLink = $"{clientBaseUrl}/reset-password" +
                        $"?email={Uri.EscapeDataString(user.Email!)}" +
                        $"&token={Uri.EscapeDataString(token)}";

        try
        {
            await _email.SendPasswordResetAsync(user.Email!, user.FullName, resetLink);
        }
        catch (Exception ex)
        {
            // כישלון שליחה נרשם ללוג בלבד. תשובת שגיאה כאן הייתה שוברת את הכלל
            // שהתשובה זהה תמיד, ומסגירה שהכתובת רשומה.
            _logger.LogError(ex, "שליחת מייל איפוס סיסמה נכשלה עבור {Email}", email);
        }

        return genericResponse;
    }

    /// <summary>
    /// קביעת סיסמה חדשה מתוך הקישור שנשלח במייל.
    /// ResetPasswordAsync מחליף גם את ה-SecurityStamp, ולכן סשנים ועוגיות קיימים מתבטלים.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var email = request.Email.Trim();

        _logger.LogInformation("בקשת החלפת סיסמה עבור {Email} בשעה {At:o}", email, DateTime.UtcNow);

        // הודעה אחת לכל סיבת כישלון של הקישור — לא מפרטים אם הטוקן פג, שונה או לא קיים.
        var invalidLink = new MessageResponse("הקישור אינו תקף או שפג תוקפו");

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return BadRequest(invalidLink);

        // בדיקת מדיניות הסיסמאות לפני האיפוס — אחרת סיסמה חלשה הייתה מוחזרת
        // כ"קישור לא תקף", והמשתמש לא היה מבין מה לתקן.
        var passwordError = await ValidatePasswordAsync(request.NewPassword);
        if (passwordError is not null)
            return BadRequest(new MessageResponse(passwordError));

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            // הטוקן עצמו לא נרשם ללוג — רק קודי השגיאה.
            _logger.LogWarning("איפוס סיסמה נכשל עבור {Email}: {Errors}",
                email, string.Join("; ", result.Errors.Select(e => e.Code)));
            return BadRequest(invalidLink);
        }

        _logger.LogInformation("הסיסמה של {Email} אופסה בהצלחה בשעה {At:o}", email, DateTime.UtcNow);

        return Ok(new MessageResponse("הסיסמה עודכנה בהצלחה. אפשר להתחבר עם הסיסמה החדשה"));
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

    /// <summary>
    /// מונה בקשות איפוס לאותה כתובת מייל ב-IMemoryCache.
    /// מחזיר false כשהמכסה בחלון הזמן נוצלה.
    /// </summary>
    private bool TryCountResetRequest(string email)
    {
        var key = $"pwreset:{email.ToLowerInvariant()}";

        // הרשומה נוצרת פעם אחת ופגה שעה אחרי הבקשה הראשונה. עדכון המונה נעשה
        // בתוך האובייקט ולא ב-Set חוזר, כדי שהחלון לא יתחיל מחדש בכל בקשה.
        var attempts = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ResetRequestWindow;
            return new ResetAttempts();
        })!;

        lock (attempts)
        {
            if (attempts.Count >= MaxResetRequestsPerWindow)
                return false;

            attempts.Count++;
            return true;
        }
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

    /// <summary>מונה בקשות האיפוס של כתובת מייל אחת, כפי שהוא נשמר ב-cache.</summary>
    private sealed class ResetAttempts
    {
        public int Count { get; set; }
    }
}
