using EventApi.Constants;
using EventApi.DataAccess;
using EventApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EventApi.Background;

/// <summary>
/// Сервис, параллельно обрабатывающий очередь бронирований в фоне.
/// </summary>
public class BookingProcessingService(ILogger<BookingProcessingService> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    private readonly ILogger<BookingProcessingService> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /// <summary>
    /// Максимальное число броней, обрабатываемых одновременно.
    /// </summary>
    public const int MaxConcurrentBookings = 8;
    private readonly SemaphoreSlim _processingSemaphore = new(MaxConcurrentBookings, MaxConcurrentBookings);

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
                var pendingBookings = await GetPendingBookingIdsAsync(stoppingToken);
                var processedCount = 0;

                var tasks = pendingBookings.Select(async bookingId =>
                {
                    await ProcessBookingAsync(bookingId, stoppingToken);
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
    /// Метод возвращает идентификаторы броней, ожидающих обработки.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены.</param>
    private async Task<List<Guid>> GetPendingBookingIdsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await context.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(stoppingToken);
    }

    /// <summary>
    /// Метод обрабатывает запись бронирования и меняет статус.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования.</param>
    /// <param name="stoppingToken">Токен отмены.</param>
    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);
                if (booking is null || booking.Status != BookingStatus.Pending)
                {
                    return;
                }

                var eventItem = await context.Events.FirstOrDefaultAsync(e => e.Id == booking.EventId, stoppingToken);
                if (eventItem is null)
                {
                    booking.Reject();
                    _logger.LogWarning(InfoMessages.BookingReject, booking.Id);
                }
                else
                {
                    booking.Confirm();
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(InfoMessages.BookingConfirmed, booking.Id);
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorMessages.ProcessBookingError, bookingId);
            await RejectBookingAsync(bookingId);
        }
    }

    /// <summary>
    /// Метод отклоняет бронирование и освобождает зарезервированные места.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования.</param>
    private async Task RejectBookingAsync(Guid bookingId)
    {
        try
        {
            await _processingSemaphore.WaitAsync();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
                if (booking is null)
                {
                    return;
                }
                booking.Reject();

                var eventItem = await context.Events.FirstOrDefaultAsync(e => e.Id == booking.EventId);
                eventItem?.ReleaseSeats();

                await context.SaveChangesAsync();
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (Exception rejectionError)
        {
            _logger.LogError(rejectionError, ErrorMessages.BookingRejectError, bookingId);
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
