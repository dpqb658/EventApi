using EventApi.Constants;
using EventApi.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Text.Json;

namespace EventApi.Middleware;

/// <summary>
/// Глобальный обработчик необработанных исключений.
/// </summary>
public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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
        var (statusCode, type, title, message) = MapException(ex);

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

        var error = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = message
        };

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            error,
            options: JsonOptions,
            contentType: MediaTypeNames.Application.ProblemJson,
            cancellationToken: httpContext.RequestAborted
        );
    }

    private static (int StatusCode, string Type, string Title, string Detail) MapException(Exception ex)
        => ex switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                ProblemDetailsTypes.Validation,
                ProblemDetailsTitles.Validation,
                ex.Message
            ),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                ProblemDetailsTypes.NotFound,
                ProblemDetailsTitles.NotFound,
                ex.Message
            ),
            NoAvailableSeatsException => (
                StatusCodes.Status409Conflict,
                ProblemDetailsTypes.NoAvailableSeats,
                ProblemDetailsTitles.NoAvailableSeats,
                ex.Message
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                ProblemDetailsTypes.InternalServerError,
                ErrorMessages.InternalServerError,
                ErrorMessages.InternalServerError
            )
        };
}
