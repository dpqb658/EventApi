namespace EventApi.DTOs;

/// <summary>
/// Класс, содержащий информацию об мероприятии для ответа.
/// </summary>
public class EventResponse
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