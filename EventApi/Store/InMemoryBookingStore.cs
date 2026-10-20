using EventApi.Models;

namespace EventApi.Repositories;

/// <summary>
/// Потокобезопасное хранилище броней в оперативной памяти.
/// </summary>
public sealed class InMemoryBookingStore : IBookingStore
{
    private readonly List<Booking> _bookings = [];
    private readonly Lock _lock = new();

    /// <inheritdoc/>
    public void Add(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        lock (_lock)
        {
            _bookings.Add(booking);
        }
    }

    /// <inheritdoc/>
    public Booking? GetById(Guid id)
    {
        lock (_lock)
        {
            return _bookings.FirstOrDefault(x => x.Id == id);
        }
    }

    /// <inheritdoc/>
    public void Update(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        lock (_lock)
        {
            var index = _bookings.FindIndex(x => x.Id == booking.Id);
            if (index >= 0)
            {
                _bookings[index] = booking;
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<Booking> GetPending()
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
