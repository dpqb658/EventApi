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

    /// <summary>
    /// Сообщение об отсутствии свободных мест на мероприятии.
    /// </summary>
    public const string NoAvailableSeatsError = "No available seats for this event";

    /// <summary>
    /// Сообщение об ошибке при отклонении бронирования.
    /// </summary>
    public const string BookingRejectError = "Не удалось отклонить бронирование «{BookingId}».";
}
