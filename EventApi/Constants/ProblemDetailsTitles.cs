namespace EventApi.Constants;

/// <summary>
/// Класс с кратким описанием ошибок.
/// </summary>
public static class ProblemDetailsTitles
{
    /// <summary>
    /// Краткое описание ошибки валидации.
    /// </summary>
    public const string Validation = "Ошибка валидации.";

    /// <summary>
    /// Краткое описание ошибки ресурс не найден.
    /// </summary>
    public const string NotFound = "Ресурс не найден.";

    /// <summary>
    /// Краткое описание ошибки об отсутствии свободных мест на мероприятии.
    /// </summary>
    public const string NoAvailableSeats = "No available seats for this event";
}
