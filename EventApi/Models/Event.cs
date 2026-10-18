using EventApi.Constants;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Models;

/// <summary>
/// Класс, содержащий информацию о мероприятии.
/// </summary>
public class Event
{
    private readonly Lock _lock = new();

    /// <summary>
    /// Уникальный идентификатор.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Заголовок.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата начала.
    /// </summary>
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата завершения.
    /// </summary>
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Общее количество мест.
    /// </summary>
    public int TotalSeats { get; private set; }

    /// <summary>
    /// Количество свободных мест.
    /// </summary>
    public int AvailableSeats { get; private set; }

    /// <summary>
    /// Метод создаёт новое мероприятие.
    /// </summary>
    /// <param name="title">Заголовок.</param>
    /// <param name="description">Описание.</param>
    /// <param name="startAt">Дата начала.</param>
    /// <param name="endAt">Дата завершения.</param>
    /// <param name="totalSeats">Общее количество мест.</param>
    public static Event Create(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException(ValidationMessages.TitleRequired);
        }

        if (endAt <= startAt)
        {
            throw new ValidationException(ValidationMessages.EndAtMustBeAfterStartAt);
        }

        if (totalSeats <= 0)
        {
            throw new ValidationException(ValidationMessages.TotalSeatsMin);
        }

        return new Event
        {
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }

    /// <summary>
    /// Метод бронирует место.
    /// </summary>
    /// <param name="count">Количество броней.</param>
    /// <returns>Возвращает успешно или нет.</returns>
    public bool TryReserveSeats(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        lock (_lock)
        {
            if (AvailableSeats < count)
            {
                return false;
            }

            AvailableSeats -= count;
            return true;
        }
    }

    /// <summary>
    /// Метод освобождает место.
    /// </summary>
    /// <param name="count">Количество броней.</param>
    /// <returns>Возвращает успешно или нет.</returns>
    public bool ReleaseSeats(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        lock (_lock)
        {
            if (AvailableSeats + count > TotalSeats)
            {
                return false;
            }

            AvailableSeats += count;
            return true;
        }
    }
}
