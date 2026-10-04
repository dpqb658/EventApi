using EventApi.Constants;
using EventApi.DTOs;
using EventApi.Exceptions;
using EventApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Services;

/// <summary>
/// Сервис для управления мероприятиями.
/// </summary>
public class EventService : IEventService
{
    private readonly List<Event> _events = [];

    private readonly Lock _lock = new();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public Event Create(string title, string? description, DateTime startAt, DateTime endAt)
    {
        ValidateDates(startAt, endAt);

        var eventItem = new Event
        {
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

    /// <inheritdoc/>
    public Event Update(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {
        ValidateDates(startAt, endAt);

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

    /// <inheritdoc/>
    public void Delete(Guid id)
    {
        lock (_lock)
        {
            var eventItem = _events.FirstOrDefault(x => x.Id == id)
                ?? throw new NotFoundException(string.Format(ErrorMessages.EventNotFound, id));
            _events.Remove(eventItem);
        }
    }

    /// <inheritdoc/>
    public PaginatedResult<Event> GetEventList(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        ValidatePage(page, pageSize);

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