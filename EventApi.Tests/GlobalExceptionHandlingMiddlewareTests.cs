using EventApi.Constants;
using EventApi.Exceptions;
using EventApi.Middleware;
using EventApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Xunit;

namespace EventApi.Tests;

public class GlobalExceptionHandlingMiddlewareTests
{
    /// <summary>
    /// Проверяет, что при возникновении исключения валидации middleware
    /// возвращает код ответа: 400, тип и сообщение исключения.
    /// </summary>
    [Fact]
    public async Task Invoke_ShouldReturn400_WhenValidationExceptionIsThrown()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new ValidationException(ValidationMessages.TitleRequired));

        await middleware.InvokeAsync(context);

        var response = await ReadResponse(context, GetOptions());
        Assert.Equal(nameof(ValidationException), response.Type);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(ValidationMessages.TitleRequired, response.Detail);
    }

    /// <summary>
    /// Проверяет, что при возникновении исключения, когда запрошенная запись не найдена,
    /// middleware возвращает код ответа: 404, тип и сообщение исключения.
    /// </summary>
    [Fact]
    public async Task Invoke_ShouldReturn404_WhenNotFoundExceptionIsThrown()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new NotFoundException(ErrorMessages.EventNotFound));

        await middleware.InvokeAsync(context);

        var response = await ReadResponse(context, GetOptions());
        Assert.Equal(nameof(NotFoundException), response.Type);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal(ErrorMessages.EventNotFound, response.Detail);
    }

    /// <summary>
    /// Проверяет, что при возникновении необработанного исключения middleware
    /// возвращает код ответа: 500, тип и сообщение исключения.
    /// </summary>
    [Fact]
    public async Task Invoke_ShouldReturn500AndNeutralErrorType_WhenUnhandledExceptionIsThrown()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(ErrorMessages.InternalServerError));

        await middleware.InvokeAsync(context);

        var response = await ReadResponse(context, GetOptions());
        Assert.Equal(HttpStatusCode.InternalServerError.ToString(), response.Type);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(ErrorMessages.InternalServerError, response.Detail);
    }

    /// <summary>
    /// Создаёт экземпляр глобального обработчика необработанных исключений.
    /// </summary>
    private static GlobalExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

    /// <summary>
    /// Создаёт HTTP контекст с тестовым запросом и потоком для записи ответа.
    /// </summary>
    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Method = "GET";
        context.Request.Path = "/test";
        return context;
    }

    /// <summary>
    /// Возвращает настройки сериализации JSON.
    /// </summary>
    private static JsonSerializerOptions GetOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Читает и десериализует JSON-ответ middleware в объект ошибки.
    /// </summary>
    /// <param name="context">HTTP контекст.</param>
    /// <param name="options">Настройки сериализации JSON.</param>
    private static async Task<ProblemDetails> ReadResponse(DefaultHttpContext context, JsonSerializerOptions options)
    {
        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        return Assert.IsType<ProblemDetails>(JsonSerializer.Deserialize<ProblemDetails>(json, options));
    }
}