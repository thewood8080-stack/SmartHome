using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Hubs;
using SmartHomeApi.Middleware;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

var builder = WebApplication.CreateBuilder(args);

// קובץ סודות מקומי — לא נכנס ל-Git (ראה .gitignore).
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// --- Database ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=smarthome.db"));

// --- Identity ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

// טוקן איפוס הסיסמה תקף ל-30 דקות במקום יממה שלמה (ברירת המחדל של Identity).
builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
    o.TokenLifespan = TimeSpan.FromMinutes(30));

// בפרודקשן ה-client וה-API יושבים על שני דומיינים נפרדים, ומבחינת הדפדפן זו
// בקשה cross-site. עוגייה עם SameSite=Lax פשוט לא תישלח, וההתחברות תיכשל בלי
// שום שגיאה מובנת. SameSite=None מחייב Secure, ולכן השניים הולכים יחד.
// בפיתוח נשארת ברירת המחדל, כי localhost רץ על http ועוגיית Secure לא תיקלט בו.
var crossSiteCookies = !builder.Environment.IsDevelopment();

// זה API — מחזירים קודי סטטוס במקום להפנות לדפי התחברות שלא קיימים.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;

    if (crossSiteCookies)
    {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
    // גם כאן הגוף חייב להיות { "message": "..." } — ה-client קורא רק את השדה הזה.
    options.Events.OnRedirectToLogin = ctx =>
        WriteMessageAsync(ctx.Response, StatusCodes.Status401Unauthorized, "נדרשת התחברות");
    options.Events.OnRedirectToAccessDenied = ctx =>
        WriteMessageAsync(ctx.Response, StatusCodes.Status403Forbidden, "אין לך הרשאה לפעולה הזו");
});

static Task WriteMessageAsync(HttpResponse response, int statusCode, string message)
{
    response.StatusCode = statusCode;
    response.ContentType = "application/json; charset=utf-8";
    return response.WriteAsJsonAsync(new MessageResponse(message));
}

// --- JSON — camelCase כדי שיתאים ל-client הקיים ---
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// --- SignalR — אותן הגדרות JSON כמו ב-Controllers, אחרת ה-client יקבל שמות שדות שונים ---
builder.Services.AddSignalR()
    .AddJsonProtocol(o =>
    {
        o.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ברירת המחדל של [ApiController] היא ValidationProblemDetails — פורמט שה-client לא מכיר.
builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.InvalidModelStateResponseFactory = context =>
    {
        var message = context.ModelState
            .SelectMany(entry => entry.Value?.Errors ?? new ModelErrorCollection())
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "הנתונים שנשלחו אינם תקינים";

        return new BadRequestObjectResult(new MessageResponse(message));
    };
});

// --- Session ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(2);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;

    // אותו שיקול כמו בעוגיית ההזדהות — בלי זה ה-Session לא שורד cross-site.
    if (crossSiteCookies)
    {
        o.Cookie.SameSite = SameSiteMode.None;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
});

// --- CORS — חייב AllowCredentials בשביל עוגיית ה-Session ---
// כתובת הפיתוח תמיד כלולה; כתובות נוספות מגיעות מקונפיגורציה, כך שאפשר
// להוסיף את כתובת הפרודקשן דרך משתנה סביבה (Cors__AllowedOrigins__0)
// בלי לגעת בקוד. סלאש מיותר בסוף הכתובת שובר את ההשוואה של הדפדפן, ולכן נחתך.
var allowedOrigins = new[] { "http://localhost:5173" }
    .Concat(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(o => o.AddPolicy("Client", p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// שידור עדכונים בזמן אמת מהקונטרולרים, מעל ה-Hub.
builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();

// ניקוי HTML מעורך הטקסט העשיר — הגנת XSS בצד השרת.
builder.Services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();

// אחסון תמונות. המפתחות נקראים מ-User Secrets בלבד — ראה CloudinaryService.
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();

// שליחת מיילים (איפוס סיסמה). פרטי ה-SMTP נקראים מ-User Secrets בלבד.
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// --- Application state ---
// singleton מעל IMemoryCache: מונה מבקרים ומשתמשים מחוברים, משותפים לכל הבקשות.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAppStatsService, AppStatsService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "SmartHome API",
    Version = "v1",
    Description = "API לניהול משק בית משפחתי"
}));

var app = builder.Build();

// שרת האירוח מסיים את ה-HTTPS בפרוקסי ומעביר פנימה HTTP רגיל. בלי לקרוא את
// כותרות ה-X-Forwarded, ההפניה ל-HTTPS תראה בקשת http ותפנה שוב ושוב — לולאה.
// ברירת המחדל מכבדת את הכותרות רק מפרוקסי loopback, ופרוקסי הענן אינו כזה,
// ולכן רשימות הפרוקסי הידועים מנוקות. הכותרות מגיעות מהפרוקסי בלבד; הפורט
// הפנימי לא חשוף לאינטרנט.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

// סדר ה-middleware קריטי.
app.UseCors("Client");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// אחרי האימות — כדי שהמונה ידע מי המשתמש המחובר.
app.UseMiddleware<VisitorTrackingMiddleware>();

app.MapControllers();
app.MapHub<SmartHomeHub>("/hubs/smarthome");

await DbSeeder.SeedAsync(app.Services);

// שרת האירוח מזריק את הפורט להאזנה במשתנה סביבה, וחייבים להאזין עליו על כל
// הממשקים ולא רק על localhost, אחרת בדיקת הזמינות נכשלת וה-deploy נופל.
// מקומית המשתנה לא מוגדר, והפורטים נשארים אלה שב-launchSettings.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();
