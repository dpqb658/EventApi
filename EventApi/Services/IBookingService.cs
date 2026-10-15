using EventApi.Models;

namespace EventApi.Services;

/// <summary>
/// Интерфейс для управления бронированиями.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Метод создаёт бронь для мероприятия.
    /// </summary>
    /// <param name="eventId">Уникальный идентификатор мероприятия.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Метод возвращает бронь по идентификатору.
    /// </summary>
    /// <param name="bookingId">Уникальный идентификатор.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);

    /// <summary>
    /// Метод обновляет статус бронирования.
    /// </summary>
    /// <param name="booking">Запись бронирования.</param>
    /// <param name="newStatus">Новый статус.</param>
    /// <param name="ct">Токен отмены.</param>
    void ChangeStatusBooking(Booking booking, BookingStatus newStatus, CancellationToken ct = default);

    /// <summary>
    /// Метод возвращает коллекцию бронирований, ожидающих обработки.
    /// </summary>
    IReadOnlyCollection<Booking> GetPendingBookings();
}