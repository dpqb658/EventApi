namespace EventApi.Models;

/// <summary>
/// Класс, содержащий информацию о бронировании мероприятия.
/// </summary>
public class Booking
{
    private readonly Lock _lock = new();

    /// <summary>
    /// Уникальный идентификатор.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор мероприятия.
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Текущий статус брони.
    /// </summary>
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;

    /// <summary>
    /// Дата и время создания брони.
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Дата и время обработки брони.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Обновляет статус бронирования и дату обработки брони.
    /// </summary>
    /// <param name="newStatus">Новый статус бронирования.</param>
    public void ChangeStatus(BookingStatus newStatus)
    {
        lock (_lock)
        {
            Status = newStatus;
            ProcessedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Подтверждает бронь.
    /// </summary>
    public void Confirm() => ChangeStatus(BookingStatus.Confirmed);

    /// <summary>
    /// Отклоняет бронь.
    /// </summary>
    public void Reject() => ChangeStatus(BookingStatus.Rejected);
}
