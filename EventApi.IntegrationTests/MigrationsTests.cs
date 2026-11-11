using EventApi.DataAccess.Repositories;
using EventApi.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace EventApi.IntegrationTests;

[Collection("Database")]
public class MigrationsTests(DatabaseHelper helper)
{
    /// <summary>
    /// Проверяет, что миграции создают таблицы events,
    /// bookings и __EFMigrationsHistory.
    /// </summary>
    [Fact]
    public async Task Migrate_ShouldCreateTables_EventsAndBookings()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        await using var context = helper.CreateContext();

        // Act
        var tableNames = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT table_name
                  FROM information_schema.tables
                 WHERE table_schema = 'public'
                """
            )
            .ToListAsync();

        // Assert
        Assert.Contains("events", tableNames);
        Assert.Contains("bookings", tableNames);
        Assert.Contains("__EFMigrationsHistory", tableNames);
    }

    /// <summary>
    /// Проверяет, что база данных не даёт создать бронь
    /// с несуществующим идентификатором мероприятия.
    /// </summary>
    [Fact]
    public async Task Migrate_ShouldEnforceForeignKey_FromBookingsToEvents()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        await using var context = helper.CreateContext();
        var bookingRepository = new BookingRepository(context);
        await bookingRepository.AddAsync(Booking.Create(Guid.NewGuid()));

        // Act
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => bookingRepository.SaveChangesAsync());

        // Assert
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresException.SqlState);
    }

    /// <summary>
    /// Проверяет каскадное удаление броней при удалении мероприятия.
    /// </summary>
    [Fact]
    public async Task Migrate_ShouldCascadeDeleteBookings_WhenEventDeleted()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        var (eventItem, booking) = await helper.CreateEventWithBookingAsync();

        await using var actContext = helper.CreateContext();
        var eventRepository = new EventRepository(actContext);
        var eventToDelete = await eventRepository.GetByIdAsync(eventItem.Id);
        Assert.NotNull(eventToDelete);

        // Act
        await eventRepository.DeleteAsync(eventToDelete);
        await eventRepository.SaveChangesAsync();

        // Assert
        await using var verifyContext = helper.CreateContext();
        var bookings = await new BookingRepository(verifyContext).GetByIdAsync(booking.Id);
        Assert.Null(bookings);
    }

    /// <summary>
    /// Проверяет идемпотентность миграций при повторном применении.
    /// </summary>
    [Fact]
    public async Task Migrate_ShouldBeIdempotent_WhenAppliedTwice()
    {
        // Arrange
        await helper.ResetDatabaseAsync();
        await using var context = helper.CreateContext();

        // Act
        await context.Database.MigrateAsync();
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        // Assert
        Assert.Empty(pendingMigrations);
    }
}
