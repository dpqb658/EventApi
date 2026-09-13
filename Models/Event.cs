namespace EventApi.Models;

/// <summary>
/// Класс, содержащий информацию о мероприятии.
/// </summary>
public class Event
{
    /// <summary>
    /// Уникальный идентификатор
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Заголовок
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата начала
    /// </summary>

    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата завершения
    /// </summary>
    public DateTime EndAt { get; set; }
}