namespace EventApi.Models;

/// <summary>
/// Класс, содержащий информацию о бронировании мероприятия.
/// </summary>
public class Booking
{
    private readonly Lock _lock = new();

    /// <summary>
    /// Конструктор для EF Core.
    /// </summary>
    private Booking() { }

    /// <summary>
    /// Уникальный идентификатор.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор мероприятия.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Мероприятие.
    /// </summary>
    public Event Event { get; private set; } = null!;

    /// <summary>
    /// Текущий статус брони.
    /// </summary>
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;

    /// <summary>
    /// Дата и время создания брони.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата и время обработки брони.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Метод создаёт новую бронь для мероприятия.
    /// </summary>
    /// <param name="eventId">Идентификатор мероприятия.</param>
    public static Booking Create(Guid eventId)
    {
        return new Booking { EventId = eventId };
    }

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
