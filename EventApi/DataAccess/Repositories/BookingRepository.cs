using EventApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EventApi.DataAccess.Repositories;

/// <summary>
/// Репозиторий для работы с бронированиями.
/// </summary>
/// <param name="context">Контекст базы данных приложения.</param>
public sealed class BookingRepository(AppDbContext context) : IBookingRepository
{
    private readonly AppDbContext _context = context;

    /// <summary>
    /// Метод возвращает бронь по идентификатору.
    /// </summary>
    /// <param name="bookingId">Уникальный идентификатор брони.</param>
    /// <param name="ct">Токен отмены.</param>
    public async Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);
    }

    /// <summary>
    /// Метод возвращает идентификаторы броней, ожидающих обработки.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    public async Task<List<Guid>> GetPendingBookingIdsAsync(CancellationToken ct = default)
    {
        return await _context.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Метод добавляет бронирование.
    /// </summary>
    /// <param name="booking">Бронирование.</param>
    /// <param name="ct">Токен отмены.</param>
    public async Task AddAsync(Booking booking, CancellationToken ct = default)
    {
        await _context.Bookings.AddAsync(booking, ct);
    }

    /// <summary>
    /// Метод сохраняет изменения в базе данных.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }
}
