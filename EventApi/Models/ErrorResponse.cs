namespace EventApi.Models;

/// <summary>
/// Класс, содержащий информацию об ошибке.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Код ответа
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Тип ошибки
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// Сообщение
    /// </summary>
    public string? Message { get; set; }
}