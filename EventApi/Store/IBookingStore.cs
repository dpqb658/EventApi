using EventApi.Models;

namespace EventApi.Repositories;

/// <summary>
/// Интерфейс хранилища бронирований.
/// </summary>
public interface IBookingStore
{
    /// <summary>
    /// Метод добавляет новую бронь.
    /// </summary>
    /// <param name="booking">Бронь для добавления.</param>
    void Add(Booking booking);

    /// <summary>
    /// Метод возвращает бронь по идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    Booking? GetById(Guid id);

    /// <summary>
    /// Метод обновляет существующую бронь.
    /// </summary>
    /// <param name="booking">Бронь для обновления.</param>
    void Update(Booking booking);

    /// <summary>
    /// Метод возвращает все записи, ожидающие бронирования.
    /// </summary>
    IReadOnlyCollection<Booking> GetPending();
}
