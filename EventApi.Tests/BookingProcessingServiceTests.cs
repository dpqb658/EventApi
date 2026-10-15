using EventApi.Background;
using EventApi.Models;
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
        var eventService = new EventService();
        var bookingService = new BookingService(eventService);
        var eventItem = eventService.Create(
            "Заголовок",
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1)
        );
        var booking = await bookingService.CreateBookingAsync(eventItem.Id);

        var service = new BookingProcessingService(
            NullLogger<BookingProcessingService>.Instance,
            bookingService
        );

        await service.StartAsync(CancellationToken.None);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            var processed = await bookingService.GetBookingByIdAsync(booking.Id);

            Assert.NotNull(processed);
            Assert.Equal(BookingStatus.Confirmed, processed.Status);
            Assert.NotNull(processed.ProcessedAt);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }
}