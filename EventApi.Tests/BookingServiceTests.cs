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
            BaseDate.AddHours(1),
            10
        );
        var result = await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(eventItem.Id, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Null(result.ProcessedAt);
        Assert.NotEqual(default, result.CreatedAt);
    }

    /// <summary>
    /// Проверяет, что у созданных бронирований для одного мероприятия уникальные Id и лимит исчерпан.
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
            BaseDate.AddHours(1),
            2
        );
        var booking1 = await bookingService.CreateBookingAsync(eventItem.Id);
        var booking2 = await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.Equal(0, eventItem.AvailableSeats);
    }

    /// <summary>
    /// Проверяет, что есть ошибка при попытке создать бронирование для несуществующего мероприятия.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

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
            BaseDate.AddHours(1),
            10
        );
        eventService.Delete(eventItem.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => bookingService.CreateBookingAsync(eventItem.Id));
    }

    /// <summary>
    /// Проверяет, что одно бронирование уменьшает количество доступных мест на 1.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldDecreaseAvailableSeats_ByOne()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            10
        );
        var initialSeats = eventItem.AvailableSeats;

        var booking = await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(initialSeats - 1, eventItem.AvailableSeats);
        Assert.NotNull(booking);
        Assert.NotEqual(Guid.Empty, booking.Id);
    }

    /// <summary>
    /// Проверяет, что бронирование при отсутствии мест выбрасывает NoAvailableSeatsException.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldThrowNoAvailableSeatsException_WhenCapacityReached()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            1
        );

        await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(0, eventItem.AvailableSeats);
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => bookingService.CreateBookingAsync(eventItem.Id));
    }

    /// <summary>
    /// Проверяет, что при отсутствии свободных мест все конкурентные попытки
    /// создать бронирование завершаются исключением NoAvailableSeatsException.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldThrowNoAvailableSeatsException_WhenAllSeatsAreTakenConcurrently()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            1
        );

        await bookingService.CreateBookingAsync(eventItem.Id);

        const int concurrentRequests = 10;

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async _ =>
            {
                await Assert.ThrowsAsync<NoAvailableSeatsException>(
                    () => bookingService.CreateBookingAsync(eventItem.Id)
                );
            })
            .ToList();

        await Task.WhenAll(tasks);

        Assert.Equal(0, eventItem.AvailableSeats);
    }

    /// <summary>
    /// Проверяет защиту от овербукинга при конкурентном создании.
    /// Из 20 запросов на событие с 5 местами успешно выполняются 5,
    /// остальные завершаются NoAvailableSeatsException.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldPreventOverbooking_WhenRequestsAreConcurrent()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            5
        );

        const int concurrentRequests = 20;
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async _ =>
            {
                try
                {
                    var booking = await bookingService.CreateBookingAsync(eventItem.Id);

                    return (
                        Booking: (Booking?)booking,
                        Exception: (Exception?)null
                    );
                }
                catch (NoAvailableSeatsException ex)
                {
                    return (
                        Booking: null,
                        Exception: (Exception?)ex
                    );
                }
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        var successfulBookings = results
            .Where(result => result.Booking is not null)
            .Select(result => result.Booking!)
            .ToList();

        var rejectedRequests = results
            .Count(result => result.Exception is NoAvailableSeatsException);

        Assert.Equal(5, successfulBookings.Count);
        Assert.Equal(15, rejectedRequests);
        Assert.Equal(0, eventItem.AvailableSeats);
    }

    /// <summary>
    /// Проверяет, что при конкурентном создании все записи создаются с уникальным Id.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldGenerateUniqueIds_WhenRequestsAreConcurrent()
    {
        const int concurrentRequests = 10;
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);

        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            concurrentRequests
        );

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => bookingService.CreateBookingAsync(eventItem.Id))
            .ToArray();

        var bookings = await Task.WhenAll(tasks);
        var uniqueIDs = bookings
            .Select(booking => booking.Id)
            .Distinct()
            .Count();

        Assert.Equal(concurrentRequests, uniqueIDs);
        Assert.Equal(concurrentRequests, bookings.Length);
        Assert.Equal(0, eventItem.AvailableSeats);
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
            BaseDate.AddHours(1),
            10
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
            BaseDate.AddHours(1),
            10
        );

        var created = await bookingService.CreateBookingAsync(eventItem.Id);
        created.Confirm();
        var result = await bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
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

    /// <summary>
    /// Проверяет, что при подтверждении бронирования меняется статус и заполняется дата.
    /// </summary>
    [Fact]
    public void Booking_Confirm_ShouldSetStatusAndProcessedAt()
    {
        var booking = new Booking();
        booking.Confirm();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    /// <summary>
    /// Проверяет, что при отмене бронирования меняется статус и заполняется дата.
    /// </summary>
    [Fact]
    public void Booking_Reject_ShouldSetStatusAndProcessedAt()
    {
        var booking = new Booking();
        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    /// <summary>
    /// Проверяет, что при отмене бронирования количество доступных мест восстанавливается.
    /// </summary>
    [Fact]
    public async Task RejectBooking_ShouldReleaseSeats_WhenBookingIsRejected()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            1
        );

        var booking = await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(0, eventItem.AvailableSeats);

        booking.Reject();
        eventItem.ReleaseSeats();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.Equal(1, eventItem.AvailableSeats);
    }

    /// <summary>
    /// Проверяет, что при отмене бронирования количество доступных мест
    /// восстанавливается и можно снова создать новую бронь.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldSucceed_AfterPreviousBookingIsRejected()
    {
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            1
        );

        var rejectedBooking = await bookingService.CreateBookingAsync(eventItem.Id);

        rejectedBooking.Reject();
        eventItem.ReleaseSeats();

        var newBooking = await bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(BookingStatus.Rejected, rejectedBooking.Status);
        Assert.Equal(BookingStatus.Pending, newBooking.Status);
        Assert.NotEqual(rejectedBooking.Id, newBooking.Id);
        Assert.Equal(0, eventItem.AvailableSeats);
    }
}
