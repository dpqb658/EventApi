using EventApi.Background;
using EventApi.DataAccess;
using EventApi.DataAccess.Repositories;
using EventApi.Models;
using EventApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EventApi.Tests;

public class BookingProcessingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;
    private readonly BookingProcessingService _processingService;

    public BookingProcessingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
        _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();
        _processingService = new BookingProcessingService(
            NullLogger<BookingProcessingService>.Instance,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>()
        );
    }

    /// <summary>
    /// Создаёт мероприятие и одну бронь к нему.
    /// </summary>
    private async Task<Booking> CreatePendingBookingAsync()
    {
        var eventItem = await _eventService.CreateAsync(
            "Заголовок",
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            10
        );
        return await _bookingService.CreateBookingAsync(eventItem.Id);
    }

    /// <summary>
    /// Проверяет, что сервис обработки очереди бронирований работает.
    /// </summary>
    [Fact]
    public async Task ProcessPendingBooking_ShouldConfirmBooking_AndSetProcessedAt()
    {
        await CreatePendingBookingAsync();

        await _processingService.StartAsync(CancellationToken.None);

        try
        {
            var processed = await PollUntilAsync(
                GetBookingAsync,
                b => b is not null && b.Status != BookingStatus.Pending,
                TimeSpan.FromSeconds(10)
            );

            Assert.NotNull(processed);
            Assert.Equal(BookingStatus.Confirmed, processed!.Status);
            Assert.NotNull(processed.ProcessedAt);
        }
        finally
        {
            await _processingService.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Проверяет, что бронь отклоняется, если мероприятие отсутствует при обработке.
    /// </summary>
    [Fact]
    public async Task ProcessPendingBooking_ShouldRejectBooking_WhenEventIsMissing()
    {
        var booking = await CreatePendingBookingAsync();

        using (var deleteScope = _serviceProvider.CreateScope())
        {
            var eventService = deleteScope.ServiceProvider.GetRequiredService<IEventService>();
            await eventService.DeleteAsync(booking.EventId);
        }

        await _processingService.StartAsync(CancellationToken.None);

        try
        {
            var processed = await PollUntilAsync(
                GetBookingAsync,
                b => b is not null && b.Status != BookingStatus.Pending,
                TimeSpan.FromSeconds(10)
            );

            Assert.NotNull(processed);
            Assert.Equal(BookingStatus.Rejected, processed!.Status);
            Assert.NotNull(processed.ProcessedAt);
        }
        finally
        {
            await _processingService.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Читает бронь из базы в отдельном scope.
    /// </summary>
    private async Task<Booking?> GetBookingAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Bookings.AsNoTracking().FirstOrDefaultAsync();
    }

    /// <summary>
    /// Периодически вызывает "getValue", пока не выполнится
    /// "condition", либо не истечёт "timeout".
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
    /// <param name="getValue">Асинхронная функция получения значения.</param>
    /// <param name="condition">Условие, при котором ожидание считается успешным.</param>
    /// <param name="timeout">Максимальное время ожидания.</param>
    /// <returns>Значение, удовлетворяющее условию.</returns>
    public static async Task<T> PollUntilAsync<T>(Func<Task<T>> getValue, Func<T, bool> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        var interval = TimeSpan.FromMilliseconds(200);

        while (DateTime.UtcNow - start < timeout)
        {
            var value = await getValue();
            if (condition(value))
            {
                return value;
            }

            await Task.Delay(interval);
        }

        throw new TimeoutException();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}
