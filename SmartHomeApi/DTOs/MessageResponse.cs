namespace SmartHomeApi.DTOs;

/// <summary>
/// פורמט אחיד לכל תשובת שגיאה. ה-client קורא את הטקסט מ-response.data.message,
/// ולכן כל שגיאה חייבת לצאת בצורה הזו ובעברית.
/// </summary>
/// <param name="Message">הודעה למשתמש, בעברית.</param>
public record MessageResponse(string Message);
