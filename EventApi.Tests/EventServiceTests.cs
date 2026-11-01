using EventApi.Constants;
using EventApi.DataAccess;
using EventApi.Exceptions;
using EventApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace EventApi.Tests;

public class EventServiceTests : IDisposable
{
    private static readonly DateTime BaseDate = DateTime.UtcNow;
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IEventService _service;

    public EventServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _service = _scope.ServiceProvider.GetRequiredService<IEventService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Проверяет создание мероприятия.
    /// </summary>
    [Fact]
    public async Task Create_ShouldCreateEvent()
    {
        var result = await _service.CreateAsync(
            "Заголовок",
            "Описание",
            BaseDate,
            BaseDate.AddHours(2),
            10
        );

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Заголовок", result.Title);
        Assert.Equal("Описание", result.Description);
        Assert.Equal(BaseDate, result.StartAt);
        Assert.Equal(BaseDate.AddHours(2), result.EndAt);
    }

    /// <summary>
    /// Проверяет получение всех мероприятий без фильтров.
    /// </summary>
    [Fact]
    public async Task GetEventList_ShouldReturnAllEvents_WhenNoFiltersAreSpecified()
    {
        await _service.CreateAsync(
            "Заголовок 1",
            null,
            BaseDate,
            BaseDate.AddDays(1),
            10
        );
        await _service.CreateAsync(
            "Заголовок 2",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(3),
            10
        );
        var result = await _service.GetEventListAsync(null, null, null);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    /// <summary>
    /// Проверяет получение мероприятия по Id.
    /// </summary>
    [Fact]
    public async Task GetById_ShouldReturnExistingEvent()
    {
        var created = await _service.CreateAsync(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            10
        );
        var result = await _service.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Заголовок", result.Title);
    }

    /// <summary>
    /// Проверяет, что есть ошибка, если мероприятие не найдено по Id.
    /// </summary>
    [Fact]
    public async Task GetById_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Проверяет обновление мероприятия.
    /// </summary>
    [Fact]
    public async Task Update_ShouldUpdateExistingEvent()
    {
        var created = await _service.CreateAsync(
            "Заголовок 1",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            10
        );
        var result = await _service.UpdateAsync(
            created.Id,
            "Заголовок 2",
            "Описание 2",
            BaseDate.AddHours(1),
            BaseDate.AddHours(2)
        );

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Заголовок 2", result.Title);
        Assert.Equal("Описание 2", result.Description);
        Assert.Equal(BaseDate.AddHours(1), result.StartAt);
        Assert.Equal(BaseDate.AddHours(2), result.EndAt);
    }

    /// <summary>
    /// Проверяет, что есть ошибка при обновлении, если мероприятие не найдено по Id.
    /// </summary>
    [Fact]
    public async Task Update_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateAsync(
                Guid.NewGuid(),
                "Заголовок",
                null,
                BaseDate,
                BaseDate.AddHours(1)
            )
        );
    }

    /// <summary>
    /// Проверяет, что есть ошибка при создании мероприятия с некорректными датами.
    /// </summary>
    [Fact]
    public async Task Create_ShouldThrowValidationException_WhenDatesAreInvalid()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(
                "Заголовок",
                null,
                BaseDate.AddHours(2),
                BaseDate,
                10
            )
        );

        Assert.Equal(ValidationMessages.EndAtMustBeAfterStartAt, exception.Message);
    }

    /// <summary>
    /// Проверяет, что есть ошибка валидации, если общее количество мест меньше 1.
    /// </summary>
    [Fact]
    public async Task Create_ShouldThrowValidationException_WhenTotalSeatsAreInvalid()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(
                "Заголовок",
                null,
                BaseDate,
                BaseDate.AddHours(2),
                0
            )
        );

        Assert.Equal(ValidationMessages.TotalSeatsMin, exception.Message);
    }

    /// <summary>
    /// Проверяет, что есть ошибка при обновлении мероприятия с некорректными датами.
    /// </summary>
    [Fact]
    public async Task Update_ShouldThrowValidationException_WhenDatesAreInvalid()
    {
        var created = await _service.CreateAsync(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            10
        );

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(
                created.Id,
                "Заголовок",
                null,
                BaseDate.AddHours(2),
                BaseDate
            )
        );

        Assert.Equal(ValidationMessages.EndAtMustBeAfterStartAt, exception.Message);
    }

    /// <summary>
    /// Проверяет удаление существующего мероприятия.
    /// </summary>
    [Fact]
    public async Task Delete_ShouldRemoveExistingEvent()
    {
        var created = await _service.CreateAsync(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            10
        );

        await _service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(created.Id));
        Assert.Empty((await _service.GetEventListAsync(null, null, null)).Items);
    }

    /// <summary>
    /// Проверяет, что есть ошибка при удалении, если мероприятие не найдено по Id.
    /// </summary>
    [Fact]
    public async Task Delete_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Проверяет, что есть ошибка валидации, если номер страницы превышает максимальное значение.
    /// </summary>
    [Fact]
    public async Task GetEventList_ShouldThrowValidationException_WhenPageIsTooLarge()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GetEventListAsync(
                null,
                null,
                null,
                int.MaxValue,
                int.MaxValue
            )
        );

        Assert.Equal(ValidationMessages.PageMax, exception.Message);
    }

    /// <summary>
    /// Проверяет фильтрацию по заголовку без учета регистра.
    /// </summary>
    [Fact]
    public async Task GetEventList_ShouldFilterByTitleCaseInsensitively()
    {
        await _service.CreateAsync(
            "Тестовый заголовок 1",
            null,
            BaseDate,
            BaseDate.AddDays(1),
            10
        );
        await _service.CreateAsync(
            "Тестовый заголовок 2",
            null,
            BaseDate,
            BaseDate.AddDays(2),
            10
        );
        await _service.CreateAsync(
            "Заголовок 3",
            null,
            BaseDate,
            BaseDate.AddDays(3),
            10
        );
        var result = await _service.GetEventListAsync(
            "ТЕСТОВЫЙ ЗАГОЛОВОК",
            null,
            null
        );

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    /// <summary>
    /// Проверяет фильтрацию по дате начала.
    /// </summary>
    [Fact]
    public async Task GetEventList_ShouldFilterByFromDate()
    {
        await _service.CreateAsync(
            "Тестовый заголовок 1",
            null,
            BaseDate,
            BaseDate.AddDays(1),
            10
        );
        await _service.CreateAsync(
            "Тестовый заголовок 2",
            null,
            BaseDate.AddDays(1),
            BaseDate.AddDays(2),
            10
        );
        await _service.CreateAsync(
            "Заголовок 3",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(3),
            10
        );
        var result = await _service.GetEventListAsync(
            null,
            BaseDate.AddDays(1),
            null
        );

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, e => e.Title == "Тестовый заголовок 1");
    }

    /// <summary>
    /// Проверяет фильтрацию по дате завершения.
    /// </summary>
    [Fact]
    public async Task GetEventList_ShouldFilterByToDate()
    {
        await _service.CreateAsync(
            "Тестовый заголовок 1",
            null,
            BaseDate,
            BaseDate.AddDays(1),
            10
        );
        await _service.CreateAsync(
            "Тестовый заголовок 2",
            null,
            BaseDate.AddDays(1),
            BaseDate.AddDays(2),
            10
        );
        await _service.CreateAsync(
            "Заголовок 3",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(3),
            10
        );
        var result = await _service.GetEventListAsync(
            null,
            null,
            BaseDate.AddDays(2)
        );

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, e => e.Title == "Заголовок 3");
    }

    /// <summary>
    /// Проверяет пагинацию результатов.
    /// </summary>
    [Fact]
    public async Task GetEventList_ShouldPaginate()
    {
        for (var i = 1; i <= 25; i++)
        {
            await _service.CreateAsync(
                $"Заголовок {i}",
                null,
                BaseDate.AddDays(i),
                BaseDate.AddDays(i).AddHours(1),
                10
            );
        }

        var result = await _service.GetEventListAsync(
            null,
            null,
            null,
            page: 2,
            pageSize: 10
        );

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal("Заголовок 11", result.Items.First().Title);
    }

    /// <summary>
    /// Проверяет совместное применение всех фильтров.
    /// </summary>
    [Fact]
    public async Task GetEventList_ShouldCombineAllFilters()
    {
        await _service.CreateAsync(
            "Тестовый заголовок 1",
            null,
            BaseDate.AddDays(1),
            BaseDate.AddDays(1).AddHours(1),
            10
        );
        await _service.CreateAsync(
            "Тестовый заголовок 2",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(2).AddHours(2),
            10
        );
        await _service.CreateAsync(
            "Тестовый заголовок 3",
            null,
            BaseDate.AddDays(3),
            BaseDate.AddDays(3).AddHours(3),
            10
        );
        await _service.CreateAsync(
            "Тестовый заголовок 4",
            null,
            BaseDate.AddDays(-2),
            BaseDate.AddDays(-2).AddHours(2),
            10
        );
        await _service.CreateAsync(
            "Заголовок 4",
            null,
            BaseDate.AddDays(-1),
            BaseDate.AddDays(-1).AddHours(1),
            10
        );
        var result = await _service.GetEventListAsync(
            "Тестовый заголовок",
            BaseDate,
            BaseDate.AddDays(2).AddHours(2),
            page: 1,
            pageSize: 10
        );

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(["Тестовый заголовок 1", "Тестовый заголовок 2"], result.Items.Select(e => e.Title));
    }
}
