using EventApi.Constants;
using EventApi.Models;
using EventApi.Services;

namespace EventApi.Background;

/// <summary>
/// Сервис обрабатывающий очередь бронирований в фоне.
/// </summary>
public class BookingProcessingService(ILogger<BookingProcessingService> logger, IBookingService bookingService) : BackgroundService
{
    private readonly ILogger<BookingProcessingService> _logger = logger;
    private readonly IBookingService _bookingService = bookingService;

    /// <summary>
    /// Запускает сервис обработки очереди в фоне.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(InfoMessages.BookingServiceStart);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingBookings = _bookingService.GetPendingBookings();
                var processedCount = 0;

                foreach (var booking in pendingBookings)
                {
                    if (await ProcessBookingAsync(booking, stoppingToken))
                    {
                        processedCount++;
                    }
                }

                _logger.LogInformation(InfoMessages.BookingsProcessed, processedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorMessages.BookingProcessingServiceError);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _logger.LogInformation(InfoMessages.BookingServiceStop);
    }

    /// <summary>
    /// Метод обрабатывает запись бронирования и меняет статус.
    /// </summary>
    /// <param name="booking">Запись бронирования.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns></returns>
    private async Task<bool> ProcessBookingAsync(Booking booking, CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);

            _bookingService.ChangeStatusBooking(booking, BookingStatus.Confirmed, ct);
            _logger.LogInformation(InfoMessages.BookingConfirmed, booking.Id);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorMessages.ProcessBookingError, booking.Id);
            return false;
        }
    }
}