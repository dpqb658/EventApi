namespace EventApi.Constants;

/// <summary>
/// Класс с URI ошибок.
/// </summary>
public static class ProblemDetailsTypes
{
    /// <summary>
    /// URI ошибки валидации.
    /// </summary>
    public const string Validation = "/problems/validation";

    /// <summary>
    /// URI ошибки ресурс не найден.
    /// </summary>
    public const string NotFound = "/problems/not-found";

    /// <summary>
    /// URI об отсутствии свободных мест на мероприятии.
    /// </summary>
    public const string NoAvailableSeats = "/problems/no-available-seats";

    /// <summary>
    /// URI непредвиденной ошибки.
    /// </summary>
    public const string InternalServerError = "/problems/internal-server-error";
}
