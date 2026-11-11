using EventApi.DataAccess.Repositories;
using EventApi.Models;
using Xunit;

namespace EventApi.IntegrationTests;

[Collection("Database")]
public class BookingRepositoryTests(DatabaseHelper helper)
{
    /// <summary>
    /// Проверяет сохранение брони через репозиторий.
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldPersistBooking()
    {
        // Arrange
        await helper.ResetDatabaseAsync();

        // Act
        var (eventItem, booking) = await helper.CreateEventWithBookingAsync();

        // Assert
        await using var verifyContext = helper.CreateContext();
        var loaded = await new BookingRepository(verifyContext).GetByIdAsync(booking.Id);

        Assert.NotNull(loaded);
        Assert.Equal(booking.Id, loaded.Id);
        Assert.Equal(eventItem.Id, loaded.EventId);
        Assert.Equal(BookingStatus.Pending, loaded.Status);
        Assert.Null(loaded.ProcessedAt);
        Assert.NotEqual(default, loaded.CreatedAt);
    }

    /// <summary>
    /// Проверяет получение брони по идентификатору.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnExistingBooking()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        var (eventItem, booking) = await helper.CreateEventWithBookingAsync();

        // Act
        await using var actContext = helper.CreateContext();
        var loaded = await new BookingRepository(actContext).GetByIdAsync(booking.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(booking.Id, loaded.Id);
        Assert.Equal(eventItem.Id, loaded.EventId);
        Assert.Equal(BookingStatus.Pending, loaded.Status);
    }

    /// <summary>
    /// Проверяет, что возвращается null, если бронь не существует.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenBookingDoesNotExist()
    {
        // Arrange
        await helper.ResetDatabaseAsync();

        // Act
        await using var context = helper.CreateContext();
        var loaded = await new BookingRepository(context).GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(loaded);
    }

    /// <summary>
    /// Проверяет, что смена статуса сохраняется в базе данных.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_ShouldPersistStatusChange()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        var (_, booking) = await helper.CreateEventWithBookingAsync();

        await using var actContext = helper.CreateContext();
        var repository = new BookingRepository(actContext);
        var tracked = await repository.GetByIdAsync(booking.Id);
        Assert.NotNull(tracked);

        // Act
        tracked.Confirm();
        await repository.SaveChangesAsync();

        // Assert
        await using var verifyContext = helper.CreateContext();
        var loaded = await new BookingRepository(verifyContext).GetByIdAsync(booking.Id);
        Assert.NotNull(loaded);
        Assert.Equal(BookingStatus.Confirmed, loaded.Status);
        Assert.NotNull(loaded.ProcessedAt);
    }

    /// <summary>
    /// Проверяет, что метод GetPendingBookingIdsAsync возвращает
    /// идентификаторы броней только в статусе Pending.
    /// </summary>
    [Fact]
    public async Task GetPendingBookingIdsAsync_ShouldReturnOnlyPendingIds()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        var (_, pending) = await helper.CreateEventWithBookingAsync();
        var (_, confirmed) = await helper.CreateEventWithBookingAsync();
        var (_, rejected) = await helper.CreateEventWithBookingAsync();

        await using var arrangeContext = helper.CreateContext();
        var arrangeRepository = new BookingRepository(arrangeContext);
        (await arrangeRepository.GetByIdAsync(confirmed.Id))!.Confirm();
        (await arrangeRepository.GetByIdAsync(rejected.Id))!.Reject();
        await arrangeRepository.SaveChangesAsync();

        // Act
        await using var actContext = helper.CreateContext();
        var pendingIds = await new BookingRepository(actContext).GetPendingBookingIdsAsync();

        // Assert
        var pendingId = Assert.Single(pendingIds);
        Assert.Equal(pending.Id, pendingId);
    }

    /// <summary>
    /// Проверяет, что метод GetPendingBookingIdsAsync возвращает
    /// пустой список, когда броней в статусе Pending нет.
    /// </summary>
    [Fact]
    public async Task GetPendingBookingIdsAsync_ShouldReturnEmpty_WhenNoPendingBookings()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        var (_, confirmed) = await helper.CreateEventWithBookingAsync();
        var (_, rejected) = await helper.CreateEventWithBookingAsync();

        await using var arrangeContext = helper.CreateContext();
        var arrangeRepository = new BookingRepository(arrangeContext);
        (await arrangeRepository.GetByIdAsync(confirmed.Id))!.Confirm();
        (await arrangeRepository.GetByIdAsync(rejected.Id))!.Reject();
        await arrangeRepository.SaveChangesAsync();

        // Act
        await using var actContext = helper.CreateContext();
        var pendingIds = await new BookingRepository(actContext).GetPendingBookingIdsAsync();

        // Assert
        Assert.Empty(pendingIds);
    }
}
