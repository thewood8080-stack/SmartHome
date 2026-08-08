using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using SmartHomeApi.DTOs;

namespace SmartHomeApi.Services;

/// <summary>
/// עטיפה ל-CloudinaryDotNet. אובייקט ה-Cloudinary נבנה פעם אחת (Singleton)
/// ומחזיק את המפתחות בזיכרון בלבד.
/// </summary>
public class CloudinaryService : ICloudinaryService
{
    /// <summary>כל התמונות יושבות תחת תיקייה אחת כדי שיהיה אפשר לזהות אותן בדשבורד.</summary>
    private const string ImageFolder = "smarthome/albums";

    private const string RawFolder = "smarthome/files";

    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
    {
        _logger = logger;

        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            // ההודעה מציינת אילו מפתחות חסרים — בלי הערכים עצמם.
            throw new InvalidOperationException(
                "חסרים מפתחות Cloudinary. הגדר Cloudinary:CloudName, Cloudinary:ApiKey ו-Cloudinary:ApiSecret ב-User Secrets.");
        }

        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
    }

    public async Task<(PhotoUploadResultDto? Result, string? Error)> UploadImageAsync(IFormFile file)
    {
        ImageUploadResult result;

        try
        {
            await using var stream = file.OpenReadStream();

            var parameters = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = ImageFolder,
                // שם הקובץ המקורי לא הופך למזהה: מונע התנגשויות ומונע דליפת שמות קבצים ב-URL.
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            result = await _cloudinary.UploadAsync(parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "שגיאה בפנייה ל-Cloudinary בהעלאת תמונה");
            return (null, "שגיאת תקשורת מול שירות האחסון");
        }

        if (result.Error is not null || result.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("העלאת תמונה ל-Cloudinary נכשלה. סטטוס {Status}, שגיאה: {Error}",
                result.StatusCode, result.Error?.Message);
            return (null, DescribeFailure(result.StatusCode));
        }

        return (new PhotoUploadResultDto(
            result.SecureUrl.ToString(),
            result.PublicId,
            result.Width,
            result.Height), null);
    }

    public async Task<(PhotoUploadResultDto? Result, string? Error)> UploadRawAsync(IFormFile file)
    {
        RawUploadResult result;

        try
        {
            await using var stream = file.OpenReadStream();

            var parameters = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = RawFolder
            };

            result = await _cloudinary.UploadAsync(parameters, "raw");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "שגיאה בפנייה ל-Cloudinary בהעלאת קובץ raw");
            return (null, "שגיאת תקשורת מול שירות האחסון");
        }

        if (result.Error is not null || result.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("העלאת קובץ raw ל-Cloudinary נכשלה. סטטוס {Status}, שגיאה: {Error}",
                result.StatusCode, result.Error?.Message);
            return (null, DescribeFailure(result.StatusCode));
        }

        return (new PhotoUploadResultDto(result.SecureUrl.ToString(), result.PublicId, null, null), null);
    }

    /// <summary>
    /// סיבה בעברית להצגה למשתמש. ההודעה המקורית של Cloudinary נשארת בלוג בלבד —
    /// היא באנגלית ולפעמים חושפת פרטי חשבון.
    /// </summary>
    private static string DescribeFailure(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "אימות מול שירות האחסון נכשל",
        HttpStatusCode.RequestEntityTooLarge => "הקובץ גדול מדי עבור שירות האחסון",
        HttpStatusCode.TooManyRequests => "חריגה ממכסת השימוש בשירות האחסון",
        HttpStatusCode.BadRequest => "שירות האחסון דחה את הקובץ",
        _ => "העלאה לשירות האחסון נכשלה"
    };

    public async Task<bool> DeleteAsync(string publicId, bool isRaw)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return false;

        var parameters = new DeletionParams(publicId)
        {
            ResourceType = isRaw ? ResourceType.Raw : ResourceType.Image,
            Invalidate = true
        };

        DeletionResult result;
        try
        {
            result = await _cloudinary.DestroyAsync(parameters);
        }
        catch (Exception ex)
        {
            // תקלת רשת מול Cloudinary היא כישלון מחיקה לכל דבר — הקורא לא ימחק מה-DB.
            _logger.LogError(ex, "שגיאה בפנייה ל-Cloudinary למחיקת {PublicId}", publicId);
            return false;
        }

        // "not found" נחשב הצלחה: הקובץ כבר לא שם, ואין סיבה לחסום את מחיקת הרשומה.
        if (string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        _logger.LogError("מחיקת {PublicId} מ-Cloudinary נכשלה. תוצאה: {Result}, שגיאה: {Error}",
            publicId, result.Result, result.Error?.Message);
        return false;
    }
}
