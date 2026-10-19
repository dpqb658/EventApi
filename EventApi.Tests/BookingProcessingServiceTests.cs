using EventApi.Background;
using EventApi.Models;
using EventApi.Repositories;
using EventApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EventApi.Tests;

public class BookingProcessingServiceTests
{
    /// <summary>
    /// Проверяет, что сервис обработки очереди бронирований работает.
    /// </summary>
    [Fact]
    public async Task ProcessPendingBooking_ShouldConfirmBooking_AndSetProcessedAt()
    {
        var eventStore = new InMemoryEventStore();
        var eventService = new EventService(eventStore);
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            10
        );
        var booking = await bookingService.CreateBookingAsync(eventItem.Id);

        var service = new BookingProcessingService(
            NullLogger<BookingProcessingService>.Instance,
            bookingService,
            eventStore
        );

        await service.StartAsync(CancellationToken.None);

        try
        {
            var processed = await PollUntilAsync(
                () => bookingService.GetBookingByIdAsync(booking.Id),
                b => b.Status != BookingStatus.Pending, TimeSpan.FromSeconds(10)
            );

            Assert.NotNull(processed);
            Assert.Equal(BookingStatus.Confirmed, processed.Status);
            Assert.NotNull(processed.ProcessedAt);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
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
}
