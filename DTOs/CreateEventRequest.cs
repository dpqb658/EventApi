using System.ComponentModel.DataAnnotations;

namespace EventApi.DTOs;

/// <summary>
/// Запрос на создание нового мероприятия.
/// </summary>
public class CreateEventRequest : IValidatableObject
{
    /// <summary>
    /// Заголовок
    /// </summary>
    [Required(ErrorMessage = "Параметр Title обязателен для заполнения.")]
    [StringLength(250, ErrorMessage = "Параметр Title не должен превышать 250 символов.")]
    public string? Title { get; set; }

    /// <summary>
    /// Описание
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата начала
    /// </summary>
    [Required(ErrorMessage = "Параметр StartAt обязателен для заполнения.")]
    public DateTime? StartAt { get; set; }

    /// <summary>
    /// Дата завершения
    /// </summary>
    [Required(ErrorMessage = "Параметр EndAt обязателен для заполнения.")]
    public DateTime? EndAt { get; set; }

    /// <summary>
    /// Валидация даты завершения
    /// </summary>
    /// <param name="validationContext"></param>
    /// <returns></returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt.HasValue && EndAt.HasValue && EndAt.Value <= StartAt.Value)
        {
            yield return new ValidationResult(
                "Параметр EndAt должен быть позднее, чем StartAt.",
                [nameof(EndAt)]);
        }
    }
}