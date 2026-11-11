using EventApi.DataAccess;
using EventApi.DataAccess.Repositories;
using EventApi.Models;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventApi.IntegrationTests;

public class DatabaseHelper : IAsyncLifetime
{
    public readonly DateTime BaseDate = DatabaseHelper.TruncateToMicroseconds(DateTime.UtcNow);

    /// <summary>
    /// Создаёт тестовый контейнер PostgreSQL для интеграционных тестов.
    /// </summary>
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("eventapi_test")
        .Build();

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Метод создаёт новый контекст базы данных.
    /// </summary>
    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Метод приводит очищает таблицы events и bookings.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE"
        );
    }

    /// <summary>
    /// Метод обрезает дату до микросекунд.
    /// </summary>
    /// <param name="value">Исходная дата.</param>
    public static DateTime TruncateToMicroseconds(DateTime date)
    {
        return new DateTime(
            date.Ticks - date.Ticks % 10,
            date.Kind
        );
    }

    /// <summary>
    /// Метод создаёт мероприятие с одной бронью и сохраняет их в базе данных.
    /// </summary>
    /// <param name="seats">Общее количество мест мероприятия.</param>
    /// <returns>Возвращает мероприятие и бронь.</returns>
    public async Task<(Event EventItem, Booking Booking)> CreateEventWithBookingAsync(int seats = 10)
    {
        await using var db = CreateContext();
        var eventRepository = new EventRepository(db);
        var bookingRepository = new BookingRepository(db);

        var eventItem = Event.Create(
            "Заголовок",
            null,
            BaseDate,
            BaseDate.AddHours(2),
            seats
        );
        await eventRepository.AddAsync(eventItem);

        var booking = Booking.Create(eventItem.Id);
        await bookingRepository.AddAsync(booking);
        await eventRepository.SaveChangesAsync();

        return (eventItem, booking);
    }
}
