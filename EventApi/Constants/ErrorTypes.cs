namespace EventApi.Constants;

/// <summary>
/// Типы ошибок API.
/// </summary>
public static class ErrorTypes
{
    /// <summary>
    /// Ошибка валидации.
    /// </summary>
    public const string ValidationError = "ValidationError";

    /// <summary>
    /// Внутренняя ошибка сервера.
    /// </summary>
    public const string InternalServerError = "InternalServerError";
}
