using EventApi.Constants;
using EventApi.DTOs;
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
            BaseDate.AddHours(2)
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
            BaseDate.AddDays(1)
        );
        service.Create(
            "Заголовок 2",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(3)
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
            BaseDate.AddHours(1)
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
            BaseDate.AddHours(1)
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
                BaseDate
            )
        );

        Assert.Equal(ValidationMessages.EndAtMustBeAfterStartAt, exception.Message);
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
            BaseDate.AddHours(1)
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
            BaseDate.AddHours(1)
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
    /// Проверяет, что есть ошибка валидации при пустом заголовке.
    /// </summary>
    /// <param name="isCreate">Признак создания или обновления записи</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_ShouldReturnError_WhenTitleIsEmpty(bool isCreate)
    {
        EventRequestBase dto = isCreate
            ? new CreateEventRequest()
            : new UpdateEventRequest();

        dto.Title = null;
        dto.StartAt = BaseDate;
        dto.EndAt = BaseDate.AddHours(1);

        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            results,
            validateAllProperties: true
        );

        var result = results.Single();

        Assert.Equal(ValidationMessages.TitleRequired, result.ErrorMessage);
        Assert.Contains(nameof(EventRequestBase.Title), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что есть ошибка валидации при длине заголовка больше 250 символов.
    /// </summary>
    /// <param name="isCreate">Признак создания или обновления записи</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_ShouldReturnError_WhenTitleIsTooLong(bool isCreate)
    {
        EventRequestBase dto = isCreate
            ? new CreateEventRequest()
            : new UpdateEventRequest();

        dto.Title = new string('a', 251);
        dto.StartAt = BaseDate;
        dto.EndAt = BaseDate.AddHours(1);

        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            results,
            validateAllProperties: true
        );

        var result = results.Single();

        Assert.Equal(ValidationMessages.TitleTooLong, result.ErrorMessage);
        Assert.Contains(nameof(EventRequestBase.Title), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что есть ошибка валидации при пустой дате начала.
    /// </summary>
    /// <param name="isCreate">Признак создания или обновления записи</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_ShouldReturnError_WhenStartAtIsEmpty(bool isCreate)
    {
        EventRequestBase dto = isCreate
            ? new CreateEventRequest()
            : new UpdateEventRequest();

        dto.Title = "Заголовок";
        dto.EndAt = BaseDate;

        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            results,
            validateAllProperties: true
        );

        var result = results.Single();

        Assert.Equal(ValidationMessages.StartAtRequired, result.ErrorMessage);
        Assert.Contains(nameof(EventRequestBase.StartAt), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что есть ошибка валидации при пустой дате завершения.
    /// </summary>
    /// <param name="isCreate">Признак создания или обновления записи</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_ShouldReturnError_WhenEndAtIsEmpty(bool isCreate)
    {
        EventRequestBase dto = isCreate
            ? new CreateEventRequest()
            : new UpdateEventRequest();

        dto.Title = "Заголовок";
        dto.StartAt = BaseDate;

        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            results,
            validateAllProperties: true
        );

        var result = results.Single();

        Assert.Equal(ValidationMessages.EndAtRequired, result.ErrorMessage);
        Assert.Contains(nameof(EventRequestBase.EndAt), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что есть ошибка валидации при создании или обновлении, если дата начала позднее даты завершения.
    /// </summary>
    /// <param name="isCreate">Признак создания или обновления записи</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_ShouldReturnError_WhenEndAtIsBeforeStartAt(bool isCreate)
    {
        EventRequestBase dto = isCreate
            ? new CreateEventRequest()
            : new UpdateEventRequest();

        dto.StartAt = BaseDate.AddHours(2);
        dto.EndAt = BaseDate;

        var result = dto
            .Validate(new ValidationContext(dto))
            .Single();

        Assert.Equal(ValidationMessages.EndAtMustBeAfterStartAt, result.ErrorMessage);
        Assert.Contains(nameof(EventRequestBase.EndAt), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что есть ошибка валидации, если номер страницы меньше 1.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenPageIsTooSmall()
    {
        var dto = new EventFilterParameters { Page = 0 };

        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            results,
            validateAllProperties: true
        );

        var result = results.Single();

        Assert.Equal(ValidationMessages.PageMin, result.ErrorMessage);
        Assert.Contains(nameof(EventFilterParameters.Page), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что есть ошибка валидации, если номер страницы превышает максимальное значение.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenPageIsTooLarge()
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
    /// Проверяет, что есть ошибка валидации, если количество элементов на странице меньше 1.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenPageSizeIsTooSmall()
    {
        var dto = new EventFilterParameters { PageSize = 0 };

        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            results,
            validateAllProperties: true
        );

        var result = results.Single();

        Assert.Equal(ValidationMessages.PageSizeRange, result.ErrorMessage);
        Assert.Contains(nameof(EventFilterParameters.PageSize), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что есть ошибка валидации, если количество элементов на странице больше 100.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenPageSizeIsTooLarge()
    {
        var dto = new EventFilterParameters { PageSize = 101 };

        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            results,
            validateAllProperties: true
        );

        var result = results.Single();

        Assert.Equal(ValidationMessages.PageSizeRange, result.ErrorMessage);
        Assert.Contains(nameof(EventFilterParameters.PageSize), result.MemberNames);
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
            BaseDate.AddDays(1)
        );
        service.Create(
            "Тестовый заголовок 2",
            null,
            BaseDate,
            BaseDate.AddDays(2)
        );
        service.Create(
            "Заголовок 3",
            null,
            BaseDate,
            BaseDate.AddDays(3)
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
            BaseDate.AddDays(1)
        );
        service.Create(
             "Тестовый заголовок 2",
             null,
             BaseDate.AddDays(1),
             BaseDate.AddDays(2)
        );
        service.Create(
            "Заголовок 3",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(3)
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
            BaseDate.AddDays(1)
        );
        service.Create(
            "Тестовый заголовок 2",
            null,
            BaseDate.AddDays(1),
            BaseDate.AddDays(2)
        );
        service.Create(
            "Заголовок 3",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(3)
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
                BaseDate.AddDays(i).AddHours(1)
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
            BaseDate.AddDays(1).AddHours(1)
        );
        service.Create(
            "Тестовый заголовок 2",
            null,
            BaseDate.AddDays(2),
            BaseDate.AddDays(2).AddHours(2)
        );
        service.Create(
            "Тестовый заголовок 3",
            null,
            BaseDate.AddDays(3),
            BaseDate.AddDays(3).AddHours(3)
        );
        service.Create(
            "Тестовый заголовок 4",
            null,
            BaseDate.AddDays(-2),
            BaseDate.AddDays(-2).AddHours(2)
        );
        service.Create(
            "Заголовок 4",
            null,
            BaseDate.AddDays(-1),
            BaseDate.AddDays(-1).AddHours(1)
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