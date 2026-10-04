using EventApi.Constants;
using EventApi.Exceptions;
using EventApi.Models;

namespace EventApi.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService(IEventService eventService) : IBookingService
{
    private readonly IEventService _eventService = eventService;
    private readonly List<Booking> _bookings = [];
    private readonly Lock _lock = new();

    /// <inheritdoc/>
    public Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _eventService.GetById(eventId);

        var booking = new Booking { EventId = eventId };

        lock (_lock)
        {
            _bookings.Add(booking);
        }

        return Task.FromResult(booking);
    }

    /// <inheritdoc/>
    public Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var booking = _bookings.FirstOrDefault(x => x.Id == bookingId);

            return Task.FromResult(booking is null
                ? throw new NotFoundException(string.Format(ErrorMessages.BookingNotFound, bookingId))
                : booking
            );
        }
    }

    /// <inheritdoc/>
    public void ChangeStatusBooking(Booking booking, BookingStatus newStatus, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (_lock)
        {
            booking.ChangeStatus(newStatus);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<Booking> GetPendingBookings()
    {
        lock (_lock)
        {
            return _bookings
                .Where(x => x.Status == BookingStatus.Pending)
                .ToList()
                .AsReadOnly();
        }
    }
}