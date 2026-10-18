using EventApi.Constants;
using EventApi.Exceptions;
using EventApi.Services;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace EventApi.Tests;

public class EventServiceTests
{
    private static readonly DateTime BaseDate = DateTime.UtcNow;

    private static EventService CreateService() => new();

    /// <summary>
    /// Проверяет создание мероприятия.
    /// </summary>
    [Fact]
    public void Create_ShouldCreateEvent()
    {
        var service = CreateService();
        var result = service.Create(
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
    public void GetEventList_ShouldReturnAllEvents_WhenNoFiltersAreSpecified()
    {
        var service = CreateService();
        service.Create(
            "Заголовок 1",
            null,
            BaseDate,
            BaseDate.AddDays(1),
            10
        );
        service.Create(
            "Заголовок 2",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(3),
            10
        );
        var result = service.GetEventList(null, null, null);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    /// <summary>
    /// Проверяет получение мероприятия по Id.
    /// </summary>
    [Fact]
    public void GetById_ShouldReturnExistingEvent()
    {
        var service = CreateService();
        var created = service.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            10
        );
        var result = service.GetById(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Заголовок", result.Title);
    }

    /// <summary>
    /// Проверяет, что есть ошибка, если мероприятие не найдено по Id.
    /// </summary>
    [Fact]
    public void GetById_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        var service = CreateService();

        Assert.Throws<NotFoundException>(() => service.GetById(Guid.NewGuid()));
    }

    /// <summary>
    /// Проверяет обновление мероприятия.
    /// </summary>
    [Fact]
    public void Update_ShouldUpdateExistingEvent()
    {
        var service = CreateService();
        var created = service.Create(
            "Заголовок 1",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            10
        );
        var result = service.Update(
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
    public void Update_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        var service = CreateService();

        Assert.Throws<NotFoundException>(() =>
            service.Update(
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
    public void Create_ShouldThrowValidationException_WhenDatesAreInvalid()
    {
        var service = CreateService();

        var exception = Assert.Throws<ValidationException>(() =>
            service.Create(
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
    public void Create_ShouldThrowValidationException_WhenTotalSeatsAreInvalid()
    {
        var service = CreateService();

        var exception = Assert.Throws<ValidationException>(() =>
            service.Create(
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
    public void Update_ShouldThrowValidationException_WhenDatesAreInvalid()
    {
        var service = CreateService();
        var created = service.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            10
        );

        var exception = Assert.Throws<ValidationException>(() =>
            service.Update(
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
    public void Delete_ShouldRemoveExistingEvent()
    {
        var service = CreateService();
        var created = service.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            10
        );

        service.Delete(created.Id);

        Assert.Throws<NotFoundException>(() => service.GetById(created.Id));
        Assert.Empty(service.GetEventList(null, null, null).Items);
    }

    /// <summary>
    /// Проверяет, что есть ошибка при удалении, если мероприятие не найдено по Id.
    /// </summary>
    [Fact]
    public void Delete_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        var service = CreateService();

        Assert.Throws<NotFoundException>(() => service.Delete(Guid.NewGuid()));
    }

    /// <summary>
    /// Проверяет, что есть ошибка валидации, если номер страницы превышает максимальное значение.
    /// </summary>
    [Fact]
    public void GetEventList_ShouldThrowValidationException_WhenPageIsTooLarge()
    {
        var service = CreateService();
        var exception = Assert.Throws<ValidationException>(() =>
            service.GetEventList(
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
    public void GetEventList_ShouldFilterByTitleCaseInsensitively()
    {
        var service = CreateService();
        service.Create(
            "Тестовый заголовок 1",
            null,
            BaseDate,
            BaseDate.AddDays(1),
            10
        );
        service.Create(
            "Тестовый заголовок 2",
            null,
            BaseDate,
            BaseDate.AddDays(2),
            10
        );
        service.Create(
            "Заголовок 3",
            null,
            BaseDate,
            BaseDate.AddDays(3),
            10
        );
        var result = service.GetEventList(
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
    public void GetEventList_ShouldFilterByFromDate()
    {
        var service = CreateService();
        service.Create(
            "Тестовый заголовок 1",
            null,
            BaseDate,
            BaseDate.AddDays(1),
            10
        );
        service.Create(
             "Тестовый заголовок 2",
             null,
             BaseDate.AddDays(1),
             BaseDate.AddDays(2),
            10
        );
        service.Create(
            "Заголовок 3",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(3),
            10
        );
        var result = service.GetEventList(
            null,
            BaseDate.AddDays(1),
            null
        );

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, x => x.Title == "Тестовый заголовок 1");
    }

    /// <summary>
    /// Проверяет фильтрацию по дате завершения.
    /// </summary>
    [Fact]
    public void GetEventList_ShouldFilterByToDate()
    {
        var service = CreateService();
        service.Create(
            "Тестовый заголовок 1",
            null,
            BaseDate,
            BaseDate.AddDays(1),
            10
        );
        service.Create(
            "Тестовый заголовок 2",
            null,
            BaseDate.AddDays(1),
            BaseDate.AddDays(2),
            10
        );
        service.Create(
            "Заголовок 3",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(3),
            10
        );
        var result = service.GetEventList(
            null,
            null,
            BaseDate.AddDays(2)
        );

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, x => x.Title == "Заголовок 3");
    }

    /// <summary>
    /// Проверяет пагинацию результатов.
    /// </summary>
    [Fact]
    public void GetEventList_ShouldPaginate()
    {
        var service = CreateService();

        for (var i = 1; i <= 25; i++)
        {
            service.Create(
                $"Заголовок {i}",
                null,
                BaseDate.AddDays(i),
                BaseDate.AddDays(i).AddHours(1),
                10
            );
        }

        var result = service.GetEventList(
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
    public void GetEventList_ShouldCombineAllFilters()
    {
        var service = CreateService();

        service.Create(
            "Тестовый заголовок 1",
            null,
            BaseDate.AddDays(1),
            BaseDate.AddDays(1).AddHours(1),
            10
        );
        service.Create(
            "Тестовый заголовок 2",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(2).AddHours(2),
            10
        );
        service.Create(
            "Тестовый заголовок 3",
            null,
            BaseDate.AddDays(3),
            BaseDate.AddDays(3).AddHours(3),
            10
        );
        service.Create(
            "Тестовый заголовок 4",
            null,
            BaseDate.AddDays(-2),
            BaseDate.AddDays(-2).AddHours(2),
            10
        );
        service.Create(
            "Заголовок 4",
            null,
            BaseDate.AddDays(-1),
            BaseDate.AddDays(-1).AddHours(1),
            10
        );
        var result = service.GetEventList(
            "Тестовый заголовок",
            BaseDate,
            BaseDate.AddDays(2).AddHours(2),
            page: 1,
            pageSize: 10
        );

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(["Тестовый заголовок 1", "Тестовый заголовок 2"], result.Items.Select(x => x.Title));
    }
}
