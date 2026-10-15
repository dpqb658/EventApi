namespace EventApi.Constants;

/// <summary>
/// Класс с сообщениями об ошибках.
/// </summary>
public static class ErrorMessages
{
    /// <summary>
    /// Сообщение о ненайденном мероприятии.
    /// </summary>
    public const string EventNotFound = "Мероприятие с идентификатором «{0}» не найдено.";

    /// <summary>
    /// Сообщение о ненайденном бронировании.
    /// </summary>
    public const string BookingNotFound = "Бронь с идентификатором «{0}» не найдена.";

    /// <summary>
    /// Сообщение об ошибке сервиса обработки задач бронирования.
    /// </summary>
    public const string BookingProcessingServiceError = "В сервисе обработки задач бронирования произошла ошибка.";

    /// <summary>
    /// Сообщение об ошибке при обработке записи бронирования.
    /// </summary>
    public const string ProcessBookingError = "Ошибка обработки записи бронирования «{BookingId}».";

    /// <summary>
    /// Сообщение об непредвиденной ошибке.
    /// </summary>
    public const string InternalServerError = "Произошла непредвиденная ошибка.";

}