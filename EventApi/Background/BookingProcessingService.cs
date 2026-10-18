using EventApi.Constants;
using EventApi.Models;
using EventApi.Repositories;
using EventApi.Services;

namespace EventApi.Background;

/// <summary>
/// Сервис, параллельно обрабатывающий очередь бронирований в фоне.
/// </summary>
public class BookingProcessingService(ILogger<BookingProcessingService> logger, IBookingService bookingService, IEventStore eventStore) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    private readonly ILogger<BookingProcessingService> _logger = logger;
    private readonly IBookingService _bookingService = bookingService;
    private readonly IEventStore _eventStore = eventStore;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

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

                var tasks = pendingBookings.Select(async booking =>
                {
                    await ProcessBookingAsync(booking, stoppingToken);
                    Interlocked.Increment(ref processedCount);
                });

                await Task.WhenAll(tasks);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(InfoMessages.BookingsProcessed, processedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ErrorMessages.BookingProcessingServiceError);
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(InfoMessages.BookingServiceStop);
    }

    /// <summary>
    /// Метод обрабатывает запись бронирования и меняет статус.
    /// </summary>
    /// <param name="booking">Запись бронирования.</param>
    /// <param name="stoppingToken">Токен отмены.</param>
    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);
            try
            {
                var eventItem = _eventStore.GetById(booking.EventId);
                if (eventItem is null)
                {
                    booking.Reject();
                    _logger.LogWarning(InfoMessages.BookingReject, booking.Id);
                    return;
                }

                booking.Confirm();
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(InfoMessages.BookingConfirmed, booking.Id);
                }
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorMessages.ProcessBookingError, booking.Id);

            try
            {
                await _processingSemaphore.WaitAsync(CancellationToken.None);
                try
                {
                    var eventItem = _eventStore.GetById(booking.EventId);
                    eventItem?.ReleaseSeats();
                    booking.Reject();
                }
                finally
                {
                    _processingSemaphore.Release();
                }
            }
            catch (Exception rejectionError)
            {
                _logger.LogError(rejectionError, ErrorMessages.BookingRejectError, booking.Id);
            }
        }
    }

    /// <summary>
    /// Освобождает управляемые ресурсы, используемые сервисом.
    /// </summary>
    public override void Dispose()
    {
        _processingSemaphore.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
