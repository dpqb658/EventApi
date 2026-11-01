using EventApi.DataAccess;
using EventApi.Exceptions;
using EventApi.Models;
using EventApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventApi.Tests;

public class BookingServiceTests : IDisposable
{
    private static readonly DateTime BaseDate = DateTime.UtcNow;
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
        _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Создаёт мероприятие с заданным количеством мест в отдельном scope.
    /// </summary>
    private async Task<Event> CreateEventAsync(int totalSeats)
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        return await eventService.CreateAsync(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(1),
            totalSeats
        );
    }

    /// <summary>
    /// Возвращает количество свободных мест мероприятия.
    /// </summary>
    private async Task<int> GetAvailableSeatsAsync(Guid eventId)
    {
        using var scope = _serviceProvider.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var eventItem = await eventService.GetByIdAsync(eventId);
        return eventItem.AvailableSeats;
    }

    /// <summary>
    /// Проверяет создание бронирования для существующего мероприятия.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldCreatePendingBooking_ForExistingEvent()
    {
        var eventItem = await CreateEventAsync(10);
        var result = await _bookingService.CreateBookingAsync(eventItem.Id);

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
        var eventItem = await CreateEventAsync(2);
        var booking1 = await _bookingService.CreateBookingAsync(eventItem.Id);
        var booking2 = await _bookingService.CreateBookingAsync(eventItem.Id);

        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.Equal(0, await GetAvailableSeatsAsync(eventItem.Id));
    }

    /// <summary>
    /// Проверяет, что есть ошибка при попытке создать бронирование для несуществующего мероприятия.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Проверяет, что есть ошибка при попытке создать бронирование для удалённого мероприятия.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldThrowNotFoundException_WhenEventWasDeleted()
    {
        var eventItem = await CreateEventAsync(10);
        await _eventService.DeleteAsync(eventItem.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(eventItem.Id));
    }

    /// <summary>
    /// Проверяет, что одно бронирование уменьшает количество доступных мест на 1.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldDecreaseAvailableSeats_ByOne()
    {
        var eventItem = await CreateEventAsync(10);
        var booking = await _bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(9, await GetAvailableSeatsAsync(eventItem.Id));
        Assert.NotNull(booking);
        Assert.NotEqual(Guid.Empty, booking.Id);
    }

    /// <summary>
    /// Проверяет, что бронирование при отсутствии мест выбрасывает NoAvailableSeatsException.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldThrowNoAvailableSeatsException_WhenCapacityReached()
    {
        var eventItem = await CreateEventAsync(1);
        await _bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(0, await GetAvailableSeatsAsync(eventItem.Id));
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _bookingService.CreateBookingAsync(eventItem.Id));
    }

    /// <summary>
    /// Проверяет, что при отсутствии свободных мест все конкурентные попытки
    /// создать бронирование завершаются исключением NoAvailableSeatsException.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldThrowNoAvailableSeatsException_WhenAllSeatsAreTakenConcurrently()
    {
        var eventItem = await CreateEventAsync(1);
        await _bookingService.CreateBookingAsync(eventItem.Id);

        const int concurrentRequests = 10;

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                await Assert.ThrowsAsync<NoAvailableSeatsException>(
                    () => bookingService.CreateBookingAsync(eventItem.Id)
                );
            }))
            .ToList();

        await Task.WhenAll(tasks);

        Assert.Equal(0, await GetAvailableSeatsAsync(eventItem.Id));
    }

    /// <summary>
    /// Проверяет защиту от овербукинга при конкурентном создании.
    /// Из 20 запросов на событие с 5 местами успешно выполняются 5,
    /// остальные завершаются NoAvailableSeatsException.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldPreventOverbooking_WhenRequestsAreConcurrent()
    {
        var eventItem = await CreateEventAsync(5);

        const int concurrentRequests = 20;
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
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
            }))
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
        Assert.Equal(0, await GetAvailableSeatsAsync(eventItem.Id));
    }

    /// <summary>
    /// Проверяет, что при конкурентном создании все записи создаются с уникальным Id.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldGenerateUniqueIds_WhenRequestsAreConcurrent()
    {
        const int concurrentRequests = 10;
        var eventItem = await CreateEventAsync(concurrentRequests);

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                return await bookingService.CreateBookingAsync(eventItem.Id);
            }))
            .ToArray();

        var bookings = await Task.WhenAll(tasks);
        var uniqueIDs = bookings
            .Select(booking => booking.Id)
            .Distinct()
            .Count();

        Assert.Equal(concurrentRequests, uniqueIDs);
        Assert.Equal(concurrentRequests, bookings.Length);
        Assert.Equal(0, await GetAvailableSeatsAsync(eventItem.Id));
    }

    /// <summary>
    /// Проверяет получение бронирования по Id.
    /// </summary>
    [Fact]
    public async Task GetBookingById_ShouldReturnExistingBooking()
    {
        var eventItem = await CreateEventAsync(10);
        var created = await _bookingService.CreateBookingAsync(eventItem.Id);

        using var readScope = _serviceProvider.CreateScope();
        var readBookingService = readScope.ServiceProvider.GetRequiredService<IBookingService>();
        var result = await readBookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal(eventItem.Id, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    /// <summary>
    /// Проверяет изменение статуса бронирования по Id.
    /// </summary>
    [Fact]
    public async Task GetBookingById_ShouldReflectStatusChange()
    {
        var eventItem = await CreateEventAsync(10);
        var created = await _bookingService.CreateBookingAsync(eventItem.Id);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var booking = await context.Bookings.SingleAsync(b => b.Id == created.Id);
            booking.Confirm();
            await context.SaveChangesAsync();
        }

        using var readScope = _serviceProvider.CreateScope();
        var readBookingService = readScope.ServiceProvider.GetRequiredService<IBookingService>();
        var result = await readBookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    /// <summary>
    /// Проверяет, что есть ошибка при попытке получить несуществующее бронирование.
    /// </summary>
    [Fact]
    public async Task GetBookingById_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.GetBookingByIdAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Проверяет, что при подтверждении бронирования меняется статус и заполняется дата.
    /// </summary>
    [Fact]
    public void Booking_Confirm_ShouldSetStatusAndProcessedAt()
    {
        var booking = Booking.Create(Guid.NewGuid());
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
        var booking = Booking.Create(Guid.NewGuid());
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
        var eventItem = await CreateEventAsync(1);

        var created = await _bookingService.CreateBookingAsync(eventItem.Id);

        Assert.Equal(0, await GetAvailableSeatsAsync(eventItem.Id));

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var booking = await context.Bookings.SingleAsync(b => b.Id == created.Id);
            var eventEntity = await context.Events.SingleAsync(e => e.Id == eventItem.Id);
            booking.Reject();
            eventEntity.ReleaseSeats();
            await context.SaveChangesAsync();
        }

        using var readScope = _serviceProvider.CreateScope();
        var readBookingService = readScope.ServiceProvider.GetRequiredService<IBookingService>();
        var result = await readBookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Rejected, result.Status);
        Assert.NotNull(result.ProcessedAt);
        Assert.Equal(1, await GetAvailableSeatsAsync(eventItem.Id));
    }

    /// <summary>
    /// Проверяет, что при отмене бронирования количество доступных мест
    /// восстанавливается и можно снова создать новую бронь.
    /// </summary>
    [Fact]
    public async Task CreateBooking_ShouldSucceed_AfterPreviousBookingIsRejected()
    {
        var eventItem = await CreateEventAsync(1);

        var rejectedBooking = await _bookingService.CreateBookingAsync(eventItem.Id);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rejectedInDb = await context.Bookings.SingleAsync(b => b.Id == rejectedBooking.Id);
            var eventEntity = await context.Events.SingleAsync(e => e.Id == eventItem.Id);
            rejectedInDb.Reject();
            eventEntity.ReleaseSeats();
            await context.SaveChangesAsync();
        }

        Booking newBooking;
        using (var newScope = _serviceProvider.CreateScope())
        {
            var bookingService = newScope.ServiceProvider.GetRequiredService<IBookingService>();
            newBooking = await bookingService.CreateBookingAsync(eventItem.Id);
        }

        using var readScope = _serviceProvider.CreateScope();
        var readBookingService = readScope.ServiceProvider.GetRequiredService<IBookingService>();
        var rejected = await readBookingService.GetBookingByIdAsync(rejectedBooking.Id);

        Assert.Equal(BookingStatus.Rejected, rejected.Status);
        Assert.Equal(BookingStatus.Pending, newBooking.Status);
        Assert.NotEqual(rejectedBooking.Id, newBooking.Id);
        Assert.Equal(0, await GetAvailableSeatsAsync(eventItem.Id));
    }
}
