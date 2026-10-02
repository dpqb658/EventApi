namespace EventApi.Exceptions;

/// <summary>
/// Исключение, когда запрошенная запись не найдена.
/// </summary>
public class NotFoundException(string message) : Exception(message)
{
}