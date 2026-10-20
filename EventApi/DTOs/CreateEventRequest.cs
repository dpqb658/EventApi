using EventApi.Constants;
using System.ComponentModel.DataAnnotations;

namespace EventApi.DTOs;

/// <summary>
/// Запрос на создание нового мероприятия.
/// </summary>
public class CreateEventRequest : EventRequestBase
{
    /// <summary>
    /// Общее количество мест.
    /// </summary>
    [Required(ErrorMessage = ValidationMessages.TotalSeatsRequired)]
    [Range(1, int.MaxValue, ErrorMessage = ValidationMessages.TotalSeatsMin)]
    public int? TotalSeats { get; set; }
}