using EventApi.Constants;

namespace EventApi.Exceptions;

/// <summary>
/// Исключение, когда нет свободных мест.
/// </summary>
/// <param name="message">Сообщение об ошибке.</param>
public sealed class NoAvailableSeatsException(string message = ErrorMessages.NoAvailableSeatsError) : Exception(message)
{
}