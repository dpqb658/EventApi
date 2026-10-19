using EventApi.Constants;
using EventApi.Exceptions;
using EventApi.Models;
using EventApi.Repositories;

namespace EventApi.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService(IEventService eventService, IBookingStore? bookingStore = null) : IBookingService
{
    private readonly IEventService _eventService = eventService;
    private readonly IBookingStore _bookingStore = bookingStore ?? new InMemoryBookingStore();

    /// <inheritdoc/>
    public Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var eventItem = _eventService.GetById(eventId);
        if (!eventItem.TryReserveSeats())
        {
            throw new NoAvailableSeatsException();
        }

        try
        {
            var booking = new Booking { EventId = eventId };
            _bookingStore.Add(booking);
            return Task.FromResult(booking);
        }
        catch
        {
            eventItem.ReleaseSeats();
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var booking = _bookingStore.GetById(bookingId);

        return Task.FromResult(booking is null
            ? throw new NotFoundException(string.Format(ErrorMessages.BookingNotFound, bookingId))
            : booking
        );
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<Booking> GetPendingBookings() => _bookingStore.GetPending();
}
