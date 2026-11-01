using System.Collections.Concurrent;
using EventApi.Constants;
using EventApi.DataAccess;
using EventApi.Exceptions;
using EventApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EventApi.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService(AppDbContext context) : IBookingService
{
    private static readonly SemaphoreSlim bookingLock = new(1, 1);
    private readonly AppDbContext _context = context;

    /// <inheritdoc/>
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await bookingLock.WaitAsync(ct);
        try
        {
            var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct)
                ?? throw new NotFoundException(string.Format(ErrorMessages.EventNotFound, eventId));

            if (!eventItem.TryReserveSeats())
            {
                throw new NoAvailableSeatsException();
            }

            try
            {
                var booking = Booking.Create(eventId);
                await _context.Bookings.AddAsync(booking, ct);
                await _context.SaveChangesAsync(ct);
                return booking;
            }
            catch (Exception ex)
            {
                eventItem.ReleaseSeats();
                throw new InvalidOperationException(string.Format(ErrorMessages.BookingSaveError, eventId), ex);
            }
        }
        finally
        {
            bookingLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        return booking is null
            ? throw new NotFoundException(string.Format(ErrorMessages.BookingNotFound, bookingId))
            : booking;
    }
}
