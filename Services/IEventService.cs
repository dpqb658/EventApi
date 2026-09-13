using EventApi.Models;

namespace EventApi.Services;
/// <summary>
/// Интерфейс для управления мероприятием.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Метод возвращает список всех мероприятий.
    /// </summary>
    /// <returns></returns>
    IReadOnlyCollection<Event> GetAll();

    /// <summary>
    /// Метод получает мероприятие по ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Event? GetById(Guid id);

    /// <summary>
    /// Метод создаёт новое мероприятие.
    /// </summary>
    /// <param name="title">Заголовок</param>
    /// <param name="description">Описание</param>
    /// <param name="startAt">Дата начала</param>
    /// <param name="endAt">Дата завершения</param>
    /// <returns></returns>
    Event Create(string title, string? description, DateTime startAt, DateTime endAt);

    /// <summary>
    /// Метод обновляет мероприятие.
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="title">Заголовок</param>
    /// <param name="description">Описание</param>
    /// <param name="startAt">Дата начала</param>
    /// <param name="endAt">Дата завершения</param>
    /// <returns></returns>
    Event? Update(Guid id, string title, string? description, DateTime startAt, DateTime endAt);

    /// <summary>
    /// Метод удаляет мероприятие.
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <returns></returns>
    bool Delete(Guid id);
}