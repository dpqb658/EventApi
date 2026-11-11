using EventApi.Models;

namespace EventApi.DataAccess.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с бронированиями.
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Метод возвращает бронь по идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Метод возвращает идентификаторы броней, ожидающих обработки.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    Task<List<Guid>> GetPendingBookingIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// Метод добавляет бронирование.
    /// </summary>
    /// <param name="booking">Бронирование.</param>
    /// <param name="ct">Токен отмены.</param>
    Task AddAsync(Booking booking, CancellationToken ct = default);

    /// <summary>
    /// Метод сохраняет изменения в базе данных.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
