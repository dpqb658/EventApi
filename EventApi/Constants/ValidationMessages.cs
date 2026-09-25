namespace EventApi.Constants;

/// <summary>
/// Класс с сообщениями об ошибках валидации.
/// </summary>
public static class ValidationMessages
{
    /// <summary>
    /// Сообщение об обязательности заголовка.
    /// </summary>
    public const string TitleRequired = "Параметр Title обязателен для заполнения.";

    /// <summary>
    /// Сообщение о максимальной длине заголовка.
    /// </summary>
    public const string TitleTooLong = "Параметр Title не должен превышать 250 символов.";

    /// <summary>
    /// Сообщение об обязательности для даты начала.
    /// </summary>
    public const string StartAtRequired = "Параметр StartAt обязателен для заполнения.";

    /// <summary>
    /// Сообщение об обязательности для даты окончания.
    /// </summary>
    public const string EndAtRequired = "Параметр EndAt обязателен для заполнения.";

    /// <summary>
    /// Сообщение о некорректной дате завершения.
    /// </summary>
    public const string EndAtMustBeAfterStartAt = "Параметр EndAt должен быть позднее, чем StartAt.";

    /// <summary>
    /// Сообщение о некорректном номере страницы.
    /// </summary>
    public const string PageMin = "Параметр page должен быть больше или равен 1.";

    /// <summary>
    /// Сообщение о некорректном размере страницы.
    /// </summary>
    public const string PageSizeMin = "Параметр pageSize должен быть больше или равен 1.";
}