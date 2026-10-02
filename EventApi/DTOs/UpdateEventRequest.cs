using EventApi.Constants;
using System.ComponentModel.DataAnnotations;

namespace EventApi.DTOs;

/// <summary>
/// Запрос на обновление мероприятия.
/// </summary>
public class UpdateEventRequest : IValidatableObject
{
    /// <summary>
    /// Заголовок
    /// </summary>    
    [Required(ErrorMessage = ValidationMessages.TitleRequired)]
    [StringLength(250, ErrorMessage = ValidationMessages.TitleTooLong)]
    public string? Title { get; set; }
    
    /// <summary>
    /// Описание
    /// </summary>  
    public string? Description { get; set; }

    /// <summary>
    /// Дата начала
    /// </summary>    
    [Required(ErrorMessage = ValidationMessages.StartAtRequired)]
    public DateTime? StartAt { get; set; }

    /// <summary>
    /// Дата завершения
    /// </summary>    
    [Required(ErrorMessage = ValidationMessages.EndAtRequired)]
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
                ValidationMessages.EndAtMustBeAfterStartAt, [nameof(EndAt)]
            );
        }
    }
}