using EventApi.Models;

namespace EventApi.DTOs;

/// <summary>
/// Класс, содержащий информацию о бронировании для ответа.
/// </summary>
public class BookingResponse
{
    /// <summary>
    /// Уникальный идентификатор.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор мероприятия.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Статус.
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Дата и время создания.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время обработки.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}