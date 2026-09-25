using EventApi.Constants;
using System.ComponentModel.DataAnnotations;

namespace EventApi.DTOs;

/// <summary>
/// Параметры для фильтрации и пагинации мероприятий.
/// </summary>
public class EventFilterParameters
{
    /// <summary>
    /// Поиск по заголовку
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Мероприятия, которые начинаются не раньше указанной даты
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// Мероприятия, которые заканчиваются не позже указанной даты
    /// </summary>
    public DateTime? To { get; set; }

    /// <summary>
    /// Номер страницы. По умолчанию 1
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = ValidationMessages.PageMin)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Количество элементов на странице. По умолчанию 10
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = ValidationMessages.PageSizeMin)]
    public int PageSize { get; set; } = 10;
}