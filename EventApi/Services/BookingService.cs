using EventApi.Constants;
using EventApi.DataAccess.Repositories;
using EventApi.Exceptions;
using EventApi.Models;

namespace EventApi.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService(IEventRepository eventRepository, IBookingRepository bookingRepository) : IBookingService
{
    private static readonly SemaphoreSlim bookingLock = new(1, 1);
    private readonly IEventRepository _eventRepository = eventRepository;
    private readonly IBookingRepository _bookingRepository = bookingRepository;

    /// <inheritdoc/>
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await bookingLock.WaitAsync(ct);
        try
        {
            var eventItem = await _eventRepository.GetByIdAsync(eventId, ct)
                ?? throw new NotFoundException(string.Format(ErrorMessages.EventNotFound, eventId));

            if (!eventItem.TryReserveSeats())
            {
                throw new NoAvailableSeatsException();
            }

            try
            {
                var booking = Booking.Create(eventId);
                await _bookingRepository.AddAsync(booking, ct);
                await _bookingRepository.SaveChangesAsync(ct);
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

        var booking = await _bookingRepository.GetByIdAsync(bookingId, ct);

        return booking is null
            ? throw new NotFoundException(string.Format(ErrorMessages.BookingNotFound, bookingId))
            : booking;
    }
}
