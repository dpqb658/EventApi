using EventApi.Models;

namespace EventApi.Repositories;

/// <summary>
/// Интерфейс хранилища мероприятий.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Метод добавляет новое мероприятие.
    /// </summary>
    /// <param name="eventItem">Мероприятие для добавления.</param>
    void Add(Event eventItem);

    /// <summary>
    /// Метод возвращает мероприятие по идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    Event? GetById(Guid id);

    /// <summary>
    /// Метод обновляет мероприятие.
    /// </summary>
    /// <param name="eventItem">Мероприятие для обновления.</param>
    void Update(Event eventItem);

    /// <summary>
    /// Метод удаляет мероприятие.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    bool Delete(Guid id);

    /// <summary>
    /// Метод возвращает все записи.
    /// </summary>
    IReadOnlyCollection<Event> GetAll();
}
