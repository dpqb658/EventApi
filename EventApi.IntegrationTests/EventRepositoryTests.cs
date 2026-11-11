using EventApi.DataAccess.Repositories;
using EventApi.Models;
using Xunit;

namespace EventApi.IntegrationTests;

[Collection("Database")]
public class EventRepositoryTests(DatabaseHelper helper)
{
    /// <summary>
    /// Проверяет сохранение мероприятия через репозиторий.
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldPersistEvent()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        await using var context = helper.CreateContext();
        var eventItem = Event.Create(
            "Заголовок",
            "Описание",
            helper.BaseDate,
            helper.BaseDate.AddHours(2),
            10
        );
        var repository = new EventRepository(context);

        // Act
        await repository.AddAsync(eventItem);
        await repository.SaveChangesAsync();

        // Assert
        await using var verifyContext = helper.CreateContext();
        var loaded = await new EventRepository(verifyContext).GetByIdAsync(eventItem.Id);

        Assert.NotNull(loaded);
        Assert.Equal(eventItem.Id, loaded.Id);
        Assert.Equal("Заголовок", loaded.Title);
        Assert.Equal("Описание", loaded.Description);
        Assert.Equal(helper.BaseDate, loaded.StartAt);
        Assert.Equal(helper.BaseDate.AddHours(2), loaded.EndAt);
        Assert.Equal(10, loaded.TotalSeats);
        Assert.Equal(loaded.TotalSeats, loaded.AvailableSeats);
    }

    /// <summary>
    /// Проверяет получение мероприятия по идентификатору.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnExistingEvent()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        var (eventItem, _) = await helper.CreateEventWithBookingAsync();

        // Act
        await using var actContext = helper.CreateContext();
        var loaded = await new EventRepository(actContext).GetByIdAsync(eventItem.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(eventItem.Id, loaded.Id);
        Assert.Equal("Заголовок", loaded.Title);
    }

    /// <summary>
    /// Проверяет, что возвращается null, если мероприятие не существует.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEventDoesNotExist()
    {
        // Arrange
        await helper.ResetDatabaseAsync();

        // Act
        await using var context = helper.CreateContext();
        var loaded = await new EventRepository(context).GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(loaded);
    }

    /// <summary>
    /// Проверяет обновление мероприятия через репозиторий.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_ShouldPersistTrackedChanges()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        var (eventItem, _) = await helper.CreateEventWithBookingAsync();

        await using var actContext = helper.CreateContext();
        var actRepository = new EventRepository(actContext);
        var tracked = await actRepository.GetByIdAsync(eventItem.Id);
        Assert.NotNull(tracked);

        // Act
        tracked.Title = "Новый заголовок";
        await actRepository.SaveChangesAsync();

        // Assert
        await using var verifyContext = helper.CreateContext();
        var loaded = await new EventRepository(verifyContext).GetByIdAsync(eventItem.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Новый заголовок", loaded.Title);
    }

    /// <summary>
    /// Проверяет удаление существующего мероприятия через репозиторий.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldRemoveExistingEvent()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        var (eventItem, _) = await helper.CreateEventWithBookingAsync();

        // Act
        await using var context = helper.CreateContext();
        var repository = new EventRepository(context);
        await repository.DeleteAsync(eventItem);
        await repository.SaveChangesAsync();

        // Assert
        await using var verifyContext = helper.CreateContext();
        var loaded = await new EventRepository(verifyContext).GetByIdAsync(eventItem.Id);
        Assert.Null(loaded);
    }

    /// <summary>
    /// Проверяет получение всех мероприятий.
    /// </summary>
    [Fact]
    public async Task GetListAsync_ShouldReturnAllEvents_WhenNoFiltersAreSpecified()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        await helper.CreateEventWithBookingAsync();
        await helper.CreateEventWithBookingAsync();
        await helper.CreateEventWithBookingAsync();

        // Act
        await using var actContext = helper.CreateContext();
        var result = await new EventRepository(actContext).GetListAsync(
            null,
            null,
            null,
            page: 1,
            pageSize: 10
        );

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.TotalPages);
    }

    /// <summary>
    /// Проверяет фильтрацию по заголовку без учета регистра.
    /// </summary>
    [Fact]
    public async Task GetListAsync_ShouldFilterByTitleCaseInsensitively()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        await using var arrangeContext = helper.CreateContext();
        var arrangeRepository = new EventRepository(arrangeContext);
        await arrangeRepository.AddAsync(
            Event.Create(
                "Тестовый заголовок 1",
                null,
                helper.BaseDate.AddDays(1),
                helper.BaseDate.AddDays(1).AddHours(1),
                10
            )
        );
        await arrangeRepository.AddAsync(
            Event.Create(
                "Тестовый заголовок 2",
                null,
                helper.BaseDate.AddDays(2),
                helper.BaseDate.AddDays(2).AddHours(1),
                10
            )
        );
        await arrangeRepository.AddAsync(
            Event.Create(
                "Заголовок 3",
                null,
                helper.BaseDate.AddDays(3),
                helper.BaseDate.AddDays(3).AddHours(1),
                10
            )
        );
        await arrangeRepository.SaveChangesAsync();

        // Act
        await using var actContext = helper.CreateContext();
        var result = await new EventRepository(actContext).GetListAsync(
            "ТЕСТОВЫЙ ЗАГОЛОВОК",
            null,
            null,
            page: 1,
            pageSize: 10
        );

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    /// <summary>
    /// Проверяет фильтрацию по дате начала.
    /// </summary>
    [Fact]
    public async Task GetListAsync_ShouldFilterByFromDate()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        await using var arrangeContext = helper.CreateContext();
        var arrangeRepository = new EventRepository(arrangeContext);
        await arrangeRepository.AddAsync(
            Event.Create(
                "Тестовый заголовок 1",
                null,
                helper.BaseDate.AddDays(-1),
                helper.BaseDate.AddDays(-1).AddHours(1),
                10
            )
        );
        await arrangeRepository.AddAsync(
            Event.Create(
                "Тестовый заголовок 2",
                null,
                helper.BaseDate.AddDays(1),
                helper.BaseDate.AddDays(1).AddHours(1),
                10
            )
        );
        await arrangeRepository.SaveChangesAsync();

        // Act
        await using var actContext = helper.CreateContext();
        var result = await new EventRepository(actContext).GetListAsync(
            null,
            helper.BaseDate,
            null,
            page: 1,
            pageSize: 10
        );

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.DoesNotContain(result.Items, e => e.Title == "Заголовок 1");
    }

    /// <summary>
    /// Проверяет фильтрацию по дате завершения.
    /// </summary>
    [Fact]
    public async Task GetListAsync_ShouldFilterByToDate()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        await using var arrangeContext = helper.CreateContext();
        var arrangeRepository = new EventRepository(arrangeContext);
        await arrangeRepository.AddAsync(
            Event.Create(
                "Тестовый заголовок 1",
                null,
                helper.BaseDate.AddDays(1),
                helper.BaseDate.AddDays(1).AddHours(1),
                10
            )
        );
        await arrangeRepository.AddAsync(
            Event.Create(
                "Тестовый заголовок 2",
                null,
                helper.BaseDate.AddDays(3),
                helper.BaseDate.AddDays(3).AddHours(1),
                10
            )
        );
        await arrangeRepository.SaveChangesAsync();

        // Act
        await using var actContext = helper.CreateContext();
        var result = await new EventRepository(actContext).GetListAsync(
            null,
            null,
            helper.BaseDate.AddDays(2),
            page: 1,
            pageSize: 10
        );

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.DoesNotContain(result.Items, e => e.Title == "Заголовок 2");
    }

    /// <summary>
    /// Проверяет пагинацию результатов.
    /// </summary>
    [Fact]
    public async Task GetListAsync_ShouldPaginate()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        await using var arrangeContext = helper.CreateContext();
        var arrangeRepository = new EventRepository(arrangeContext);
        for (var i = 1; i <= 25; i++)
        {
            await arrangeRepository.AddAsync(
                Event.Create(
                    $"Заголовок {i}",
                    null,
                    helper.BaseDate.AddDays(i),
                    helper.BaseDate.AddDays(i).AddHours(1),
                    10
                )
            );
        }

        await arrangeRepository.SaveChangesAsync();

        // Act
        await using var actContext = helper.CreateContext();
        var result = await new EventRepository(actContext).GetListAsync(
            null,
            null,
            null,
            page: 2,
            pageSize: 10
        );

        // Assert
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
    public async Task GetListAsync_ShouldCombineAllFilters()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        await using var arrangeContext = helper.CreateContext();
        var arrangeRepository = new EventRepository(arrangeContext);
        await arrangeRepository.AddAsync(
            Event.Create(
                "Тестовый заголовок 1",
                null,
                helper.BaseDate.AddDays(1),
                helper.BaseDate.AddDays(1).AddHours(1),
                10
            )
        );
        await arrangeRepository.AddAsync(
            Event.Create(
                "Тестовый заголовок 2",
                null,
                helper.BaseDate.AddDays(2),
                helper.BaseDate.AddDays(2).AddHours(2),
                10
            )
        );
        await arrangeRepository.AddAsync(
            Event.Create(
                "Тестовый заголовок 3",
                null,
                helper.BaseDate.AddDays(3),
                helper.BaseDate.AddDays(3).AddHours(3),
                10
            )
        );
        await arrangeRepository.AddAsync(
            Event.Create(
                "Тестовый заголовок 4",
                null,
                helper.BaseDate.AddDays(-2),
                helper.BaseDate.AddDays(-2).AddHours(2),
                10
            )
        );
        await arrangeRepository.AddAsync(
            Event.Create(
                "Заголовок 4",
                null,
                helper.BaseDate.AddDays(-1),
                helper.BaseDate.AddDays(-1).AddHours(1),
                10
            )
        );
        await arrangeRepository.SaveChangesAsync();

        // Act
        await using var actContext = helper.CreateContext();
        var result = await new EventRepository(actContext).GetListAsync(
            "Тестовый заголовок",
            helper.BaseDate,
            helper.BaseDate.AddDays(2).AddHours(2),
            page: 1,
            pageSize: 10
        );

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(["Тестовый заголовок 1", "Тестовый заголовок 2"], result.Items.Select(e => e.Title));
    }
}
