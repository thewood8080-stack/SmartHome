using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Models;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>
/// אלבומי תמונות.
/// הבידוד בין משקי בית נעשה ב-Global Query Filter על PhotoAlbum ו-Photo,
/// ולכן כל שאילתה כאן כבר מוגבלת למשק הבית של המשתמש המחובר.
/// מעליו יושבת שכבת Personal: אלבום אישי נראה לבעלים ולמנהל בלבד.
/// </summary>
[ApiController]
[Route("api/albums")]
[Authorize]
[Produces("application/json")]
public class AlbumsController : ControllerBase
{
    private const long MaxFileSizeBytes = 10L * 1024 * 1024;

    /// <summary>גבול לבקשה כולה — מאפשר כמה קבצים של 10MB בהעלאה אחת.</summary>
    private const long MaxRequestBytes = 60L * 1024 * 1024;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IHtmlSanitizerService _sanitizer;
    private readonly ICloudinaryService _cloudinary;

    public AlbumsController(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IHtmlSanitizerService sanitizer,
        ICloudinaryService cloudinary)
    {
        _db = db;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
        _cloudinary = cloudinary;
    }

    /// <summary>האלבומים שהמשתמש רשאי לראות.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PhotoAlbumDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlbums()
    {
        var rows = await VisibleAlbums()
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                Album = a,
                OwnerName = a.Owner!.FullName,
                PhotoCount = a.Photos.Count
            })
            .ToListAsync();

        return Ok(rows.Select(r => PhotoAlbumDto.From(
            r.Album, r.OwnerName, r.PhotoCount, CanModify(r.Album))));
    }

    /// <summary>יצירת אלבום חדש.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PhotoAlbumDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAlbum(PhotoAlbumCreateDto request)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        if (!TryParseVisibility(request.Visibility, out var visibility))
            return BadRequest(new MessageResponse("סוג הנראות אינו מוכר"));

        var album = new PhotoAlbum
        {
            Name = request.Name.Trim(),
            // ניקוי ה-HTML לפני השמירה — זו ההגנה מפני XSS, אותו מנגנון משלב 3.
            Description = _sanitizer.Sanitize(request.Description),
            Visibility = visibility,
            OwnerId = _currentUser.UserId!,
            HouseholdId = householdId.Value,
            CreatedAt = DateTime.UtcNow
        };

        _db.PhotoAlbums.Add(album);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await LoadAlbumDtoAsync(album.Id));
    }

    /// <summary>עדכון אלבום. הבעלים או מנהל בלבד.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PhotoAlbumDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAlbum(int id, PhotoAlbumUpdateDto request)
    {
        var album = await _db.PhotoAlbums.FirstOrDefaultAsync(a => a.Id == id);
        if (album is null || !CanView(album))
            return NotFound(new MessageResponse("האלבום לא נמצא"));

        if (!CanModify(album))
            return Forbid();

        if (!TryParseVisibility(request.Visibility, out var visibility))
            return BadRequest(new MessageResponse("סוג הנראות אינו מוכר"));

        album.Name = request.Name.Trim();
        album.Description = _sanitizer.Sanitize(request.Description);
        album.Visibility = visibility;

        await _db.SaveChangesAsync();

        return Ok(await LoadAlbumDtoAsync(album.Id));
    }

    /// <summary>
    /// מחיקת אלבום — כולל כל התמונות שבו, משני המקומות.
    /// Cloudinary קודם: אם קובץ כלשהו לא נמחק שם, בסיס הנתונים לא נגוע כלל.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> DeleteAlbum(int id)
    {
        var album = await _db.PhotoAlbums
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (album is null || !CanView(album))
            return NotFound(new MessageResponse("האלבום לא נמצא"));

        if (!CanModify(album))
            return Forbid();

        foreach (var photo in album.Photos)
        {
            if (!await _cloudinary.DeleteAsync(photo.CloudinaryPublicId, isRaw: false))
            {
                // מחיקה חלקית אפשרית כאן, אבל היא מתקנת את עצמה: בניסיון הבא
                // מה שכבר נמחק יחזיר "not found" שנחשב הצלחה, והתהליך ימשיך.
                return StatusCode(StatusCodes.Status502BadGateway, new MessageResponse(
                    "מחיקת אחת התמונות מהאחסון נכשלה. האלבום לא נמחק — נסה שוב."));
            }
        }

        // התמונות יורדות ב-Cascade יחד עם האלבום.
        _db.PhotoAlbums.Remove(album);
        await _db.SaveChangesAsync();

        return Ok(new MessageResponse("האלבום נמחק"));
    }

    /// <summary>התמונות שבאלבום.</summary>
    [HttpGet("{id:int}/photos")]
    [ProducesResponseType(typeof(IEnumerable<PhotoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlbumPhotos(int id)
    {
        var album = await _db.PhotoAlbums.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (album is null || !CanView(album))
            return NotFound(new MessageResponse("האלבום לא נמצא"));

        var photos = await _db.Photos
            .AsNoTracking()
            .Include(p => p.PhotoAlbum)
            .Include(p => p.UploadedBy)
            .Where(p => p.PhotoAlbumId == id)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

        return Ok(photos.Select(PhotoDto.From));
    }

    /// <summary>
    /// העלאת תמונות לאלבום. multipart/form-data, תומך בכמה קבצים בבקשה אחת.
    /// כל הקבצים נבדקים לפני שמעלים אפילו אחד — כדי שבקשה לא חוקית לא תשאיר חצי אלבום.
    /// </summary>
    [HttpPost("{id:int}/photos")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [ProducesResponseType(typeof(IEnumerable<PhotoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> UploadPhotos(int id)
    {
        var householdId = _currentUser.HouseholdId;
        if (householdId is null)
            return Forbid();

        var album = await _db.PhotoAlbums.FirstOrDefaultAsync(a => a.Id == id);
        if (album is null || !CanView(album))
            return NotFound(new MessageResponse("האלבום לא נמצא"));

        if (!CanModify(album))
            return Forbid();

        // נקרא ישירות מהטופס כדי לקבל קבצים בכל שם שדה.
        var files = Request.Form.Files;
        if (files.Count == 0)
            return BadRequest(new MessageResponse("לא נבחרו קבצים"));

        foreach (var file in files)
        {
            var error = await ValidateImageAsync(file);
            if (error is not null)
                return BadRequest(new MessageResponse(error));
        }

        // קובץ שנכשל בהעלאה לא מפיל את השאר — מה שעלה נשמר, והנכשלים חוזרים ברשימה עם הסיבה.
        var uploaded = new List<PhotoUploadResultDto>();
        var failed = new List<(string FileName, string Reason)>();

        foreach (var file in files)
        {
            var (result, error) = await _cloudinary.UploadImageAsync(file);

            if (result is null)
                failed.Add((Path.GetFileName(file.FileName), error ?? "העלאה לשירות האחסון נכשלה"));
            else
                uploaded.Add(result);
        }

        if (uploaded.Count == 0)
        {
            var details = string.Join("; ", failed.Select(f => $"{f.FileName} — {f.Reason}"));
            return StatusCode(StatusCodes.Status502BadGateway, new MessageResponse(
                $"העלאת כל הקבצים נכשלה. {details}"));
        }

        var photos = uploaded.Select(u => new Photo
        {
            PhotoAlbumId = album.Id,
            CloudinaryUrl = u.Url,
            CloudinaryPublicId = u.PublicId,
            Width = u.Width,
            Height = u.Height,
            UploadedById = _currentUser.UserId!,
            HouseholdId = householdId.Value,
            UploadedAt = DateTime.UtcNow
        }).ToList();

        _db.Photos.AddRange(photos);

        // אלבום בלי תמונת שער מקבל את הראשונה שהועלתה.
        album.CoverPhotoUrl ??= photos[0].CloudinaryUrl;

        await _db.SaveChangesAsync();

        var ids = photos.Select(p => p.Id).ToList();
        var saved = await _db.Photos
            .AsNoTracking()
            .Include(p => p.PhotoAlbum)
            .Include(p => p.UploadedBy)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            photos = saved.Select(PhotoDto.From),
            failed = failed.Select(f => new { fileName = f.FileName, reason = f.Reason })
        });
    }

    /// <summary>כל התמונות במשק הבית, מכל האלבומים. מנהל בלבד.</summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(IEnumerable<PhotoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllHouseholdPhotos()
    {
        if (!_currentUser.IsManager)
            return Forbid();

        // הפילטר הגלובלי כבר מגביל למשק הבית, ולכן אין כאן סינון ידני.
        var photos = await _db.Photos
            .AsNoTracking()
            .Include(p => p.PhotoAlbum)
            .Include(p => p.UploadedBy)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

        return Ok(photos.Select(PhotoDto.From));
    }

    /// <summary>
    /// האלבומים שהמשתמש רשאי לראות: כל אלבומי המשפחה, ובנוסף האישיים שלו.
    /// מנהל רואה הכל.
    /// </summary>
    private IQueryable<PhotoAlbum> VisibleAlbums()
    {
        if (_currentUser.IsManager)
            return _db.PhotoAlbums;

        var userId = _currentUser.UserId;
        return _db.PhotoAlbums.Where(a =>
            a.Visibility == AlbumVisibility.Family || a.OwnerId == userId);
    }

    private bool CanView(PhotoAlbum album) =>
        album.Visibility == AlbumVisibility.Family
        || album.OwnerId == _currentUser.UserId
        || _currentUser.IsManager;

    private bool CanModify(PhotoAlbum album) =>
        album.OwnerId == _currentUser.UserId || _currentUser.IsManager;

    private async Task<PhotoAlbumDto> LoadAlbumDtoAsync(int albumId)
    {
        var row = await _db.PhotoAlbums
            .AsNoTracking()
            .Where(a => a.Id == albumId)
            .Select(a => new
            {
                Album = a,
                OwnerName = a.Owner!.FullName,
                PhotoCount = a.Photos.Count
            })
            .FirstAsync();

        return PhotoAlbumDto.From(row.Album, row.OwnerName, row.PhotoCount, CanModify(row.Album));
    }

    /// <summary>"Family" | "Personal". ריק נופל ל-Family.</summary>
    private static bool TryParseVisibility(string? value, out AlbumVisibility visibility)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            visibility = AlbumVisibility.Family;
            return true;
        }

        return Enum.TryParse(value.Trim(), ignoreCase: true, out visibility)
               && Enum.IsDefined(visibility);
    }

    /// <summary>מחזיר הודעת שגיאה בעברית, או null אם הקובץ תקין.</summary>
    private static async Task<string?> ValidateImageAsync(IFormFile file)
    {
        var name = Path.GetFileName(file.FileName);

        if (file.Length == 0)
            return $"הקובץ '{name}' ריק";

        if (file.Length > MaxFileSizeBytes)
            return $"הקובץ '{name}' גדול מ-10MB";

        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return $"סוג הקובץ '{name}' אינו נתמך. מותר: jpg, jpeg, png, webp";

        if (!AllowedContentTypes.Contains(file.ContentType?.ToLowerInvariant()))
            return $"סוג התוכן המוצהר של '{name}' אינו נתמך";

        // הבדיקה שקובעת: חתימת הבתים בתחילת הקובץ.
        // הסיומת ו-Content-Type נשלטים על ידי הלקוח ואי אפשר לסמוך עליהם —
        // בלי הבדיקה הזו קובץ הרצה בשם .jpg היה עובר.
        var format = DetectImageFormat(await ReadSignatureAsync(file));

        if (format is null)
            return $"התוכן של '{name}' אינו תמונה תקינה";

        if (!MatchesExtension(format, extension))
            return $"התוכן של '{name}' אינו תואם את סיומת הקובץ";

        return null;
    }

    private static async Task<byte[]> ReadSignatureAsync(IFormFile file)
    {
        // 12 בתים מספיקים לכל שלוש החתימות; WebP הוא הארוך שבהן.
        await using var stream = file.OpenReadStream();
        var buffer = new byte[12];
        var read = await stream.ReadAsync(buffer);

        return read < buffer.Length ? buffer[..read] : buffer;
    }

    private static string? DetectImageFormat(byte[] header)
    {
        if (StartsWith(header, 0xFF, 0xD8, 0xFF))
            return "jpeg";

        if (StartsWith(header, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
            return "png";

        // WebP: "RIFF" בבתים 0-3 ו-"WEBP" בבתים 8-11.
        if (header.Length >= 12
            && StartsWith(header, 0x52, 0x49, 0x46, 0x46)
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return "webp";
        }

        return null;
    }

    private static bool StartsWith(byte[] header, params byte[] signature)
    {
        if (header.Length < signature.Length)
            return false;

        for (var i = 0; i < signature.Length; i++)
        {
            if (header[i] != signature[i])
                return false;
        }

        return true;
    }

    private static bool MatchesExtension(string format, string extension) => format switch
    {
        "jpeg" => extension is ".jpg" or ".jpeg",
        "png" => extension is ".png",
        "webp" => extension is ".webp",
        _ => false
    };
}
