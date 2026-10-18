using EventApi.Constants;
using EventApi.DTOs;
using EventApi.Exceptions;
using EventApi.Models;
using EventApi.Repositories;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Services;

/// <summary>
/// Сервис для управления мероприятиями.
/// </summary>
public class EventService(IEventStore? eventStore = null) : IEventService
{
    private readonly IEventStore _eventStore = eventStore ?? new InMemoryEventStore();

    /// <inheritdoc/>
    public Event GetById(Guid id)
    {
        var eventItem = _eventStore.GetById(id);
        return eventItem is null
            ? throw new NotFoundException(string.Format(ErrorMessages.EventNotFound, id))
            : eventItem;
    }

    /// <inheritdoc/>
    public Event Create(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        var eventItem = Event.Create(
            title,
            description,
            startAt,
            endAt,
            totalSeats
        );

        _eventStore.Add(eventItem);
        return eventItem;
    }

    /// <inheritdoc/>
    public Event Update(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {
        ValidateDates(startAt, endAt);

        var eventItem = GetById(id);
        eventItem.Title = title;
        eventItem.Description = description;
        eventItem.StartAt = startAt;
        eventItem.EndAt = endAt;

        return eventItem;
    }

    /// <inheritdoc/>
    public void Delete(Guid id)
    {
        if (!_eventStore.Delete(id))
        {
            throw new NotFoundException(string.Format(ErrorMessages.EventNotFound, id));
        }
    }

    /// <inheritdoc/>
    public PaginatedResult<Event> GetEventList(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        ValidatePage(page, pageSize);

        IEnumerable<Event> query = _eventStore.GetAll();

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

    /// <summary>
    /// Валидация даты завершения.
    /// </summary>
    /// <param name="startAt">Дата и время начала.</param>
    /// <param name="endAt">Дата и время завершения.</param>
    /// <exception cref="ValidationException"></exception>
    private static void ValidateDates(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
        {
            throw new ValidationException(ValidationMessages.EndAtMustBeAfterStartAt);
        }
    }

    /// <summary>
    /// Валидация номера страницы.
    /// </summary>
    /// <param name="page">Номер страницы.</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <exception cref="ValidationException"></exception>
    private static void ValidatePage(int page, int pageSize)
    {
        if (page > int.MaxValue / pageSize)
        {
            throw new ValidationException(ValidationMessages.PageMax);
        }
    }
}
