using EventApi.Constants;
using EventApi.DataAccess.Repositories;
using EventApi.DTOs;
using EventApi.Exceptions;
using EventApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Services;

/// <summary>
/// Сервис для управления мероприятиями.
/// </summary>
public class EventService(IEventRepository eventRepository) : IEventService
{
    private readonly IEventRepository _eventRepository = eventRepository;

    /// <inheritdoc/>
    public async Task<Event> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var eventItem = await _eventRepository.GetByIdAsync(id, ct);
        return eventItem is null
            ? throw new NotFoundException(string.Format(ErrorMessages.EventNotFound, id))
            : eventItem;
    }

    /// <inheritdoc/>
    public async Task<Event> CreateAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats, CancellationToken ct = default)
    {
        var eventItem = Event.Create(
            title,
            description,
            startAt,
            endAt,
            totalSeats
        );

        await _eventRepository.AddAsync(eventItem, ct);
        await _eventRepository.SaveChangesAsync(ct);
        return eventItem;
    }

    /// <inheritdoc/>
    public async Task<Event> UpdateAsync(Guid id, string title, string? description, DateTime startAt, DateTime endAt, CancellationToken ct = default)
    {
        ValidateDates(startAt, endAt);

        var eventItem = await GetByIdAsync(id, ct);
        eventItem.Title = title;
        eventItem.Description = description;
        eventItem.StartAt = startAt;
        eventItem.EndAt = endAt;

        await _eventRepository.SaveChangesAsync(ct);
        return eventItem;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var eventItem = await GetByIdAsync(id, ct);

        await _eventRepository.DeleteAsync(eventItem, ct);
        await _eventRepository.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult<Event>> GetEventListAsync(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        ValidatePage(page, pageSize);

        return await _eventRepository.GetListAsync(title, from, to, page, pageSize, ct);
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
