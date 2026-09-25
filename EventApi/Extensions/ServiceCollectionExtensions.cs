using EventApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventApi.Extensions;

/// <summary>
/// Обработчик исключений валидации.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Возвращает единообразный ответ при ошибках валидации.
    /// </summary>
    /// <param name="services"></param>
    public static IServiceCollection AddValidationErrorHandling(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var messages = context.ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage)
                        ? "Некорректное значение."
                        : x.ErrorMessage);

                var error = new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ErrorType = "ValidationError",
                    Message = string.Join(" ", messages)
                };

                return new BadRequestObjectResult(error);
            };
        });

        return services;
    }
}