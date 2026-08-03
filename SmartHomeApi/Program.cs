using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
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

// זה API — מחזירים קודי סטטוס במקום להפנות לדפי התחברות שלא קיימים.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
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
});

// --- CORS — חייב AllowCredentials בשביל עוגיית ה-Session ---
builder.Services.AddCors(o => o.AddPolicy("Client", p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ניקוי HTML מעורך הטקסט העשיר — הגנת XSS בצד השרת.
builder.Services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();

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

await DbSeeder.SeedAsync(app.Services);

app.Run();
