using EventApi.DTOs;
using EventApi.Models;
using EventApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventApi.Controllers;

/// <summary>
/// Контроллер для управления бронированиями.
/// </summary>
[ApiController]
[Route("/")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    private readonly IBookingService _bookingService = bookingService;

    /// <summary>
    /// Метод создаёт бронь мероприятия и возвращает её для последующей проверки статуса.
    /// </summary>
    [HttpPost("events/{id:guid}/book")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> Create(Guid id, CancellationToken ct)
    {
        var booking = await _bookingService.CreateBookingAsync(id, ct);
        var response = MapToResponse(booking);

        return AcceptedAtAction(
            nameof(GetById),
            new { id = booking.Id },
            response
        );
    }

    /// <summary>
    /// Метод возвращает текущее состояние брони.
    /// </summary>
    [HttpGet("bookings/{id:guid}")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> GetById(Guid id, CancellationToken ct)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id, ct);

        return Ok(MapToResponse(booking));
    }

    /// <summary>
    /// Преобразует Booking в BookingResponse для ответа.
    /// </summary>
    /// <param name="booking">Бронь.</param>
    /// <returns></returns>
    private static BookingResponse MapToResponse(Booking booking) => new()
    {
        Id = booking.Id,
        EventId = booking.EventId,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };
}