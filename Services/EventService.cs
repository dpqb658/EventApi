using EventApi.Models;

namespace EventApi.Services;

/// <summary>
/// Сервис для управления мероприятиями.
/// </summary>
public class EventService : IEventService
{
    private readonly List<Event> _events = [];

    private readonly Lock _lock = new();

    /// <summary>
    /// Метод получает список всех мероприятий.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<Event> GetAll()
    {
        lock (_lock)
        {
            return _events
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Метод получает мероприятие по ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Event? GetById(Guid id)
    {
        lock (_lock)
        {
            var eventItem = _events.FirstOrDefault(x => x.Id == id);
            return eventItem ?? null;
        }
    }

    /// <summary>
    /// Метод создаёт новое мероприятие.
    /// </summary>
    /// <param name="title">Заголовок</param>
    /// <param name="description">Описание</param>
    /// <param name="startAt">Дата начала</param>
    /// <param name="endAt">Дата завершения</param>
    /// <returns></returns>
    public Event Create(string title, string? description, DateTime startAt, DateTime endAt)
    {
        var eventItem = new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt
        };

        lock (_lock)
        {
            _events.Add(eventItem);
        }

        return eventItem;
    }

    /// <summary>
    /// Метод обновляет мероприятие.
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="title">Заголовок</param>
    /// <param name="description">Описание</param>
    /// <param name="startAt">Дата начала</param>
    /// <param name="endAt">Дата завершения</param>
    /// <returns></returns>
    public Event? Update(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {
        lock (_lock)
        {
            var eventItem = _events.FirstOrDefault(x => x.Id == id);
            if (eventItem == null) return null;

            eventItem.Title = title;
            eventItem.Description = description;
            eventItem.StartAt = startAt;
            eventItem.EndAt = endAt;

            return eventItem;
        }
    }

    /// <summary>
    /// Метод удаляет мероприятие.
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <returns></returns>
    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            var eventItem = _events.FirstOrDefault(x => x.Id == id);
            if (eventItem == null) return false;

            _events.Remove(eventItem);

            return true;
        }
    }
}