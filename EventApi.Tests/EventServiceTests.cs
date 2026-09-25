using EventApi.Constants;
using EventApi.DTOs;
using EventApi.Exceptions;
using EventApi.Services;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace EventApi.Tests;

public class EventServiceTests
{
    private static readonly DateTime BaseDate = DateTime.Now;

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
    /// Проверяет получение всех мероприятий.
    /// </summary>
    [Fact]
    public void GetAll_ShouldReturnAllEvents()
    {
        var service = CreateService();
        service.Create("Заголовок 1", null, BaseDate, BaseDate.AddDays(1));
        service.Create("Заголовок 2", null, BaseDate.AddDays(2), BaseDate.AddDays(3));
        var result = service.GetAll();

        Assert.Equal(2, result.Count);
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

        Assert.Throws<NotFoundException>(() => service.Update(
            Guid.NewGuid(), "Заголовок", null, BaseDate, BaseDate.AddHours(1)
        ));
    }

    /// <summary>
    /// Проверяет удаление мероприятия.
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
        Assert.Empty(service.GetAll());
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
    /// Проверяет, что заголовок заполен.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenTitleIsEmpty()
    {
        var dto = new CreateEventRequest
        {
            Title = null,
            StartAt = BaseDate,
            EndAt = BaseDate.AddHours(1),
        };

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            dto, new ValidationContext(dto), results, validateAllProperties: true
        );
        var result = results.Single();
        Assert.Equal(ValidationMessages.TitleRequired, result.ErrorMessage);
        Assert.Contains(nameof(CreateEventRequest.Title), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что заголовок не превышать 250 символов.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenTitleIsTooLong()
    {
        var dto = new CreateEventRequest
        {
            Title = new string('a', 251),
            StartAt = BaseDate,
            EndAt = BaseDate.AddHours(1),
        };

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            dto, new ValidationContext(dto), results, validateAllProperties: true
        );
        var result = results.Single();

        Assert.Equal(ValidationMessages.TitleTooLong, result.ErrorMessage);
        Assert.Contains(nameof(CreateEventRequest.Title), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что дата начала заполнена.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenStartAtIsEmpty()
    {
        var dto = new CreateEventRequest
        {
            Title = "Заголовок",
            EndAt = BaseDate
        };

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            dto, new ValidationContext(dto), results, validateAllProperties: true
        );
        var result = results.Single();
        Assert.Equal(ValidationMessages.StartAtRequired, result.ErrorMessage);
        Assert.Contains(nameof(CreateEventRequest.StartAt), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что дата завершения заполнена.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenEndAtIsEmpty()
    {
        var dto = new CreateEventRequest
        {
            Title = "Заголовок",
            StartAt = BaseDate
        };

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            dto, new ValidationContext(dto), results, validateAllProperties: true
        );
        var result = results.Single();
        Assert.Equal(ValidationMessages.EndAtRequired, result.ErrorMessage);
        Assert.Contains(nameof(CreateEventRequest.EndAt), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что дата завершения должна быть позднее даты начала.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenEndAtIsBeforeStartAt()
    {
        var dto = new CreateEventRequest
        {
            StartAt = BaseDate.AddHours(2),
            EndAt = BaseDate
        };

        var result = dto
            .Validate(new ValidationContext(dto))
            .Single();

        Assert.Equal(ValidationMessages.EndAtMustBeAfterStartAt, result.ErrorMessage);
        Assert.Contains(nameof(CreateEventRequest.EndAt), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что номер страницы не может быть 0.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenPageIsInvalid()
    {
        var dto = new EventFilterParameters { Page = 0 };

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            dto, new ValidationContext(dto), results, validateAllProperties: true
        );
        var result = results.Single();

        Assert.Equal(ValidationMessages.PageMin, result.ErrorMessage);
        Assert.Contains(nameof(EventFilterParameters.Page), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что количество элементов на странице не может быть 0.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenPageSizeIsInvalid()
    {
        var dto = new EventFilterParameters { PageSize = 0 };

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            dto, new ValidationContext(dto), results, validateAllProperties: true
        );
        var result = results.Single();

        Assert.Equal(ValidationMessages.PageSizeMin, result.ErrorMessage);
        Assert.Contains(nameof(EventFilterParameters.PageSize), result.MemberNames);
    }

    /// <summary>
    /// Проверяет фильтрацию по заголовку.
    /// </summary>
    [Fact]
    public void GetEventList_ShouldFilterByTitleCaseInsensitively()
    {
        var service = CreateService();
        service.Create("Тестовый заголовок 1", null, BaseDate, BaseDate.AddDays(1));
        service.Create("Тестовый заголовок 2", null, BaseDate, BaseDate.AddDays(2));
        service.Create("Заголовок 3", null, BaseDate, BaseDate.AddDays(3));
        var result = service.GetEventList("Тестовый заголовок", null, null);

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
        service.Create("Тестовый заголовок 1", null, BaseDate, BaseDate.AddDays(1));
        service.Create("Тестовый заголовок 2", null, BaseDate.AddDays(1), BaseDate.AddDays(2));
        service.Create("Заголовок 3", null, BaseDate.AddDays(2), BaseDate.AddDays(3));
        var result = service.GetEventList(null, BaseDate.AddDays(1), null);

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
        service.Create("Тестовый заголовок 1", null, BaseDate, BaseDate.AddDays(1));
        service.Create("Тестовый заголовок 2", null, BaseDate.AddDays(1), BaseDate.AddDays(2));
        service.Create("Заголовок 3", null, BaseDate.AddDays(2), BaseDate.AddDays(3));
        var result = service.GetEventList(null, null, BaseDate.AddDays(2));

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, x => x.Title == "Заголовок 3");
    }

    /// <summary>
    /// Проверяет, что выборка по страницам работает.
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

        var result = service.GetEventList(null, null, null, page: 2, pageSize: 10);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal("Заголовок 11", result.Items.First().Title);
    }

    /// <summary>
    /// Проверяет использование всех фильтров.
    /// </summary>
    [Fact]
    public void GetEventList_ShouldCombineAllFilters()
    {
        var service = CreateService();

        service.Create("Тестовый заголовок 1", null, BaseDate.AddDays(1), BaseDate.AddDays(1));
        service.Create("Тестовый заголовок 2", null, BaseDate.AddDays(2), BaseDate.AddDays(2).AddHours(1));
        service.Create("Тестовый заголовок 3", null, BaseDate.AddDays(3), BaseDate.AddDays(3).AddHours(2));
        service.Create("Тестовый заголовок 4", null, BaseDate.AddDays(-2), BaseDate.AddDays(-2).AddHours(1));
        service.Create("Заголовок 4", null, BaseDate.AddDays(-1), BaseDate.AddDays(-1));

        var result = service.GetEventList(
            "Тестовый заголовок", BaseDate, BaseDate.AddDays(2).AddHours(2), page: 1, pageSize: 10
        );

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, x => Assert.Contains(
            "Заголовок", x.Title, StringComparison.OrdinalIgnoreCase
        ));
    }
}
