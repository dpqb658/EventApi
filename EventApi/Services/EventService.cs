using EventApi.Constants;
using EventApi.DTOs;
using EventApi.Exceptions;
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
    /// Метод получает список мероприятий.
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
    public Event GetById(Guid id)
    {
        lock (_lock)
        {
            var eventItem = _events.FirstOrDefault(x => x.Id == id);

            return eventItem is null
                ? throw new NotFoundException(string.Format(ErrorMessages.EventNotFound, id))
                : eventItem;
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
    public Event Update(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {
        lock (_lock)
        {
            var eventItem = _events.FirstOrDefault(x => x.Id == id)
                ?? throw new NotFoundException(string.Format(ErrorMessages.EventNotFound, id));
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
    public void Delete(Guid id)
    {
        lock (_lock)
        {
            var eventItem = _events.FirstOrDefault(x => x.Id == id)
                ?? throw new NotFoundException(string.Format(ErrorMessages.EventNotFound, id));
            _events.Remove(eventItem);
        }
    }

    /// <summary>
    /// Метод получает список мероприятий по фильтру.
    /// </summary>
    /// <param name="title">Заголовок</param>
    /// <param name="from">Дата начала</param>
    /// <param name="to">Дата завершения</param>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Количество элементов на странице</param>
    /// <returns></returns>
    public PaginatedResult<Event> GetEventList(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        lock (_lock)
        {
            IEnumerable<Event> query = _events;

            if (!string.IsNullOrWhiteSpace(title))
            {
                query = query.Where(x => x.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.StartAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.EndAt <= to.Value);
            }

            var totalCount = query.Count();

            var items = query
                .OrderBy(x => x.StartAt)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PaginatedResult<Event>
            {
                Items = items.AsReadOnly(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
    }
}