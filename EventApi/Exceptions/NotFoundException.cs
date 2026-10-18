namespace EventApi.Exceptions;

/// <summary>
/// Исключение, когда запрошенная запись не найдена.
/// </summary>
/// <param name="message">Сообщение об ошибке.</param>
public class NotFoundException(string message) : Exception(message)
{
}