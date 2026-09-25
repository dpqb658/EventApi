using EventApi.Exceptions;
using EventApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EventApi.Middleware;

/// <summary>
/// Глобальный обработчик необработанных исключений.
/// </summary>
public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger = logger;

    /// <summary>
    /// Перехватывает глобальные исключения.
    /// </summary>
    /// <param name="httpContext">Контекст текущего HTTP-запроса.</param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleException(httpContext, ex);
        }
    }

    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        var statusCode = MapStatusCode(ex);
        var message = MapErrorMessage(ex);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                ex,
                "Необработанное исключение. Method={Method}, Path={Path}, RequestId={RequestId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.Request.Headers["x-request-id"]);
        }
        else
        {
            _logger.LogWarning(
                ex,
                "Ошибка обработки запроса. Method={Method}, Path={Path}, Message={Message}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                ex.Message);
        }

        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning("HTTP-ответ уже был отправлен, невозможно изменить статус ошибки.");
            return;
        }

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var error = new ErrorResponse
        {
            StatusCode = statusCode,
            ErrorType = ex.GetType().Name,
            Message = message
        };

        await httpContext.Response.WriteAsJsonAsync(error);
    }

    private static int MapStatusCode(Exception ex)
        => ex switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string MapErrorMessage(Exception ex)
        => ex switch
        {
            ValidationException => ex.Message,
            NotFoundException => ex.Message,
            _ => "Произошла непредвиденная ошибка."
        };
}