using EventApi.DTOs;
using EventApi.Models;

namespace EventApi.Services;

/// <summary>
/// Интерфейс для управления мероприятием.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Метод получает мероприятие по ID.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<Event> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Метод создаёт новое мероприятие.
    /// </summary>
    /// <param name="title">Заголовок.</param>
    /// <param name="description">Описание.</param>
    /// <param name="startAt">Дата начала.</param>
    /// <param name="endAt">Дата завершения.</param>
    /// <param name="totalSeats">Общее количество мест.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<Event> CreateAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats, CancellationToken ct = default);

    /// <summary>
    /// Метод обновляет мероприятие.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    /// <param name="title">Заголовок.</param>
    /// <param name="description">Описание.</param>
    /// <param name="startAt">Дата начала.</param>
    /// <param name="endAt">Дата завершения.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<Event> UpdateAsync(Guid id, string title, string? description, DateTime startAt, DateTime endAt, CancellationToken ct = default);

    /// <summary>
    /// Метод удаляет мероприятие.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    /// <param name="ct">Токен отмены.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Метод получает список мероприятий по фильтру.
    /// </summary>
    /// <param name="title">Заголовок.</param>
    /// <param name="from">Дата начала.</param>
    /// <param name="to">Дата завершения.</param>
    /// <param name="page">Номер страницы.</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<PaginatedResult<Event>> GetEventListAsync(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10, CancellationToken ct = default);
}
