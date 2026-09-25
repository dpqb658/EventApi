namespace EventApi.DTOs;

/// <summary>
/// Класс, содержащий результат с разбивкой на страницы.
/// </summary>
/// <typeparam name="T">Тип элемента</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Элементы текущей страницы
    /// </summary>
    public IReadOnlyCollection<T> Items { get; set; } = [];

    /// <summary>
    /// Номер страницы
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Количество элементов на странице
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Количество элементов
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Количество страниц
    /// </summary>
    public int TotalPages { get; set; }
}