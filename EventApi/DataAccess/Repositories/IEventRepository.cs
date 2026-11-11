using EventApi.DTOs;
using EventApi.Models;

namespace EventApi.DataAccess.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с мероприятиями.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Метод получает мероприятие по ID.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Метод получает список мероприятий по фильтру.
    /// </summary>
    /// <param name="title">Заголовок.</param>
    /// <param name="from">Дата начала.</param>
    /// <param name="to">Дата завершения.</param>
    /// <param name="page">Номер страницы.</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<PaginatedResult<Event>> GetListAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Метод добавляет мероприятие.
    /// </summary>
    /// <param name="eventItem">Мероприятие.</param>
    /// <param name="ct">Токен отмены.</param>
    Task AddAsync(Event eventItem, CancellationToken ct = default);

    /// <summary>
    /// Метод удаляет мероприятие.
    /// </summary>
    /// <param name="eventItem">Мероприятие.</param>
    /// <param name="ct">Токен отмены.</param>
    Task DeleteAsync(Event eventItem, CancellationToken ct = default);

    /// <summary>
    /// Метод сохраняет изменения в базе данных.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
