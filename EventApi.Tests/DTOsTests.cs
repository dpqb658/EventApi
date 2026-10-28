using EventApi.Constants;
using EventApi.DTOs;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace EventApi.Tests;

public class DTOsTests
{
    private static readonly DateTime BaseDate = DateTime.UtcNow;

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
        if (dto is CreateEventRequest createRequest)
        {
            createRequest.TotalSeats = 10;
        }

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
        if (dto is CreateEventRequest createRequest)
        {
            createRequest.TotalSeats = 10;
        }

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
        if (dto is CreateEventRequest createRequest)
        {
            createRequest.TotalSeats = 10;
        }

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
        if (dto is CreateEventRequest createRequest)
        {
            createRequest.TotalSeats = 10;
        }

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
    /// Проверяет, что есть ошибка валидации, если общее количество мест не заполнено.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenTotalSeatsIsEmpty()
    {
        CreateEventRequest dto = new()
        {
            Title = "Заголовок",
            StartAt = BaseDate,
            EndAt = BaseDate.AddHours(1)
        };

        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            results,
            validateAllProperties: true
        );

        var result = results.Single();

        Assert.Equal(ValidationMessages.TotalSeatsRequired, result.ErrorMessage);
        Assert.Contains(nameof(CreateEventRequest.TotalSeats), result.MemberNames);
    }

    /// <summary>
    /// Проверяет, что есть ошибка валидации, если общее количество мест меньше 1.
    /// </summary>
    [Fact]
    public void Validate_ShouldReturnError_WhenTotalSeatsIsTooSmall()
    {
        CreateEventRequest dto = new()
        {
            Title = "Заголовок",
            StartAt = BaseDate,
            EndAt = BaseDate.AddHours(1),
            TotalSeats = 0
        };

        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            results,
            validateAllProperties: true
        );

        var result = results.Single();

        Assert.Equal(ValidationMessages.TotalSeatsMin, result.ErrorMessage);
        Assert.Contains(nameof(CreateEventRequest.TotalSeats), result.MemberNames);
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
}
