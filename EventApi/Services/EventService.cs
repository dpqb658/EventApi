using EventApi.Constants;
using EventApi.DataAccess;
using EventApi.DTOs;
using EventApi.Exceptions;
using EventApi.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Services;

/// <summary>
/// Сервис для управления мероприятиями.
/// </summary>
public class EventService(AppDbContext context) : IEventService
{
    private readonly AppDbContext _context = context;

    /// <inheritdoc/>
    public async Task<Event> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
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

        await _context.Events.AddAsync(eventItem, ct);
        await _context.SaveChangesAsync(ct);
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

        await _context.SaveChangesAsync(ct);
        return eventItem;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var eventItem = await GetByIdAsync(id, ct);

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult<Event>> GetEventListAsync(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        ValidatePage(page, pageSize);

        IQueryable<Event> query = _context.Events;

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.StartAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.EndAt <= to.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(e => e.StartAt)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

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
