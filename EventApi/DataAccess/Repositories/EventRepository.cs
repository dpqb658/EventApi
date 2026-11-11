using EventApi.DTOs;
using EventApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EventApi.DataAccess.Repositories;

/// <summary>
/// Репозиторий для работы с мероприятиями.
/// </summary>
/// <param name="context">Контекст базы данных приложения.</param>
public sealed class EventRepository(AppDbContext context) : IEventRepository
{
    private readonly AppDbContext _context = context;

    /// <summary>
    /// Метод получает мероприятие по ID.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    /// <param name="ct">Токен отмены.</param>
    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    /// <summary>
    /// Метод получает список мероприятий по фильтру с разбивкой на страницы.
    /// </summary>
    /// <param name="title">Заголовок.</param>
    /// <param name="from">Дата начала.</param>
    /// <param name="to">Дата завершения.</param>
    /// <param name="page">Номер страницы.</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <param name="ct">Токен отмены.</param>
    public async Task<PaginatedResult<Event>> GetListAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
    {
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
    /// Метод добавляет мероприятие.
    /// </summary>
    /// <param name="eventItem">Мероприятие.</param>
    /// <param name="ct">Токен отмены.</param>
    public async Task AddAsync(Event eventItem, CancellationToken ct = default)
    {
        await _context.Events.AddAsync(eventItem, ct);
    }

    /// <summary>
    /// Метод удаляет мероприятие.
    /// </summary>
    /// <param name="eventItem">Мероприятие.</param>
    /// <param name="ct">Токен отмены.</param>
    public Task DeleteAsync(Event eventItem, CancellationToken ct = default)
    {
        _context.Events.Remove(eventItem);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Метод сохраняет изменения в базе данных.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }
}
