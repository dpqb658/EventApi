using EventApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EventApi.DataAccess;

/// <summary>
/// Контекст базы данных приложения.
/// </summary>
/// <param name="options">Параметры контекста.</param>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Мероприятия.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>
    /// Бронирования.
    /// </summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
