using EventApi.Models;

namespace EventApi.Repositories;

/// <summary>
/// Потокобезопасное хранилище мероприятий в оперативной памяти.
/// </summary>
public sealed class InMemoryEventStore : IEventStore
{
    private readonly List<Event> _events = [];
    private readonly Lock _lock = new();

    /// <inheritdoc/>
    public void Add(Event eventItem)
    {
        ArgumentNullException.ThrowIfNull(eventItem);

        lock (_lock)
        {
            _events.Add(eventItem);
        }
    }

    /// <inheritdoc/>
    public Event? GetById(Guid id)
    {
        lock (_lock)
        {
            return _events.FirstOrDefault(x => x.Id == id);
        }
    }

    /// <inheritdoc/>
    public void Update(Event eventItem)
    {
        ArgumentNullException.ThrowIfNull(eventItem);

        lock (_lock)
        {
            var index = _events.FindIndex(x => x.Id == eventItem.Id);
            if (index >= 0)
            {
                _events[index] = eventItem;
            }
        }
    }

    /// <inheritdoc/>
    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            var eventItem = _events.FirstOrDefault(x => x.Id == id);
            return eventItem is not null && _events.Remove(eventItem);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<Event> GetAll()
    {
        lock (_lock)
        {
            return _events
                .ToList()
                .AsReadOnly();
        }
    }
}
