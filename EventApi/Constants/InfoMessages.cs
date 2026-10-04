namespace EventApi.Constants;

/// <summary>
/// Класс с информационными сообщениями.
/// </summary>
public static class InfoMessages
{
    /// <summary>
    /// Сообщение о запуске сервиса для обработки задач бронирования в фоне.
    /// </summary>
    public const string BookingServiceStart = "Сервис «BookingProcessingService» запущен.";

    /// <summary>
    /// Сообщение об остановке сервиса для обработки задач бронирования в фоне.
    /// </summary>
    public const string BookingServiceStop = "Сервис «BookingProcessingService» остановлен.";
}