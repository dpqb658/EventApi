using EventApi.Exceptions;
using EventApi.Models;
using EventApi.Services;
using Xunit;

namespace EventApi.Tests;

public class BookingServiceTests
{
    private static readonly DateTime BaseDate = DateTime.UtcNow;

    /// <summary>
    /// Проверяет создание бронирования для существующего мероприятия.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldCreatePendingBooking_ForExistingEvent()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1)
        );
        var result = await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(eventItem.Id, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Null(result.ProcessedAt);
        Assert.NotEqual(default, result.CreatedAt);
    }

    /// <summary>
    /// Проверяет, что у созданных бронирований для одного мероприятия уникальные Id.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldCreateUniqueIds_ForMultipleBookings()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1)
        );
        var booking1 = await bookingService.CreateBookingAsync(eventItem.Id);
        var booking2 = await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.NotEqual(booking1.Id, booking2.Id);
    }

    /// <summary>
    /// Проверяет получение бронирования по Id.
    /// </summary>
    [Fact]
    public async Task GetBookingById_ShouldReturnExistingBooking()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1)
        );
        var created = await bookingService.CreateBookingAsync(eventItem.Id);
        var result = await bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    /// <summary>
    /// Проверяет изменение статуса бронирования по Id.
    /// </summary>
    [Fact]
    public async Task GetBookingById_ShouldReflectStatusChange()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1)
        );
        var created = await bookingService.CreateBookingAsync(eventItem.Id);
        bookingService.ChangeStatusBooking(created, BookingStatus.Confirmed);
        var result = await bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    /// <summary>
    /// Проверяет, что есть ошибка при попытке создать бронирование для несуществующего мероприятия.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        var bookingService = new BookingService(new EventService());

        await Assert.ThrowsAsync<NotFoundException>(() => bookingService.CreateBookingAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Проверяет, что есть ошибка при попытке создать бронирование для удалённого мероприятия.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldThrowNotFoundException_WhenEventWasDeleted()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1)
        );
        eventService.Delete(eventItem.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => bookingService.CreateBookingAsync(eventItem.Id));
    }

    /// <summary>
    /// Проверяет, что есть ошибка при попытке получить несуществующее бронирование.
    /// </summary>
    [Fact]
    public async Task GetBookingById_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        var bookingService = new BookingService(new EventService());

        await Assert.ThrowsAsync<NotFoundException>(() => bookingService.GetBookingByIdAsync(Guid.NewGuid()));
    }
}