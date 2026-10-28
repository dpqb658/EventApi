using EventApi.Constants;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace EventApi.Extensions;

/// <summary>
/// Обработчик исключений валидации.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Возвращает единообразный ответ при ошибках валидации.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    public static IServiceCollection AddValidationErrorHandling(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var messages = context.ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage)
                        ? ValidationMessages.InvalidValue
                        : x.ErrorMessage);

                var error = new ProblemDetails
                {
                    Type = ProblemDetailsTypes.Validation,
                    Title = ProblemDetailsTitles.Validation,
                    Status = StatusCodes.Status400BadRequest,
                    Detail = string.Join(" ", messages)
                };

                return new BadRequestObjectResult(error)
                {
                    ContentTypes = { MediaTypeNames.Application.ProblemJson }
                };
            };
        });

        return services;
    }
}
