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
    Event GetById(Guid id);

    /// <summary>
    /// Метод создаёт новое мероприятие.
    /// </summary>
    /// <param name="title">Заголовок.</param>
    /// <param name="description">Описание.</param>
    /// <param name="startAt">Дата начала.</param>
    /// <param name="endAt">Дата завершения.</param>
    Event Create(string title, string? description, DateTime startAt, DateTime endAt);

    /// <summary>
    /// Метод обновляет мероприятие.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    /// <param name="title">Заголовок.</param>
    /// <param name="description">Описание.</param>
    /// <param name="startAt">Дата начала.</param>
    /// <param name="endAt">Дата завершения.</param>
    Event Update(Guid id, string title, string? description, DateTime startAt, DateTime endAt);

    /// <summary>
    /// Метод удаляет мероприятие.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    void Delete(Guid id);

    /// <summary>
    /// Метод получает список мероприятий по фильтру.
    /// </summary>
    /// <param name="title">Заголовок.</param>
    /// <param name="from">Дата начала.</param>
    /// <param name="to">Дата завершения.</param>
    /// <param name="page">Номер страницы.</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    PaginatedResult<Event> GetEventList(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10);
}