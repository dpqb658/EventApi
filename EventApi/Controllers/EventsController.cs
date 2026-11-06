using EventApi.DTOs;
using EventApi.Models;
using EventApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventApi.Controllers;

/// <summary>
/// Контроллер для управления мероприятиями.
/// </summary>
/// <param name="eventService"></param>
[ApiController]
[Route("events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;

    /// <summary>
    /// Метод возвращает список мероприятий.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<EventResponse>>> GetAll([FromQuery] EventFilterParameters parameters, CancellationToken ct)
    {
        var events = await _eventService.GetEventListAsync(
            parameters.Title,
            parameters.From,
            parameters.To,
            parameters.Page,
            parameters.PageSize,
            ct);

        return Ok(new PaginatedResult<EventResponse>
        {
            Items = events.Items.Select(MapToResponse).ToList().AsReadOnly(),
            Page = events.Page,
            PageSize = events.PageSize,
            TotalCount = events.TotalCount,
            TotalPages = events.TotalPages
        });
    }

    /// <summary>
    /// Метод возвращает мероприятие по ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(Guid id, CancellationToken ct)
    {
        var eventItem = await _eventService.GetByIdAsync(id, ct);

        return Ok(MapToResponse(eventItem));
    }

    /// <summary>
    /// Метод создаёт новое мероприятие.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponse>> Create([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var eventItem = await _eventService.CreateAsync(
            request.Title!,
            request.Description,
            request.StartAt!.Value,
            request.EndAt!.Value,
            request.TotalSeats!.Value,
            ct
        );
        var response = MapToResponse(eventItem);

        return CreatedAtAction(
            nameof(GetById),
            new { id = eventItem.Id },
            response
        );
    }

    /// <summary>
    /// Метод обновляет мероприятие.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> Update(Guid id, [FromBody] UpdateEventRequest request, CancellationToken ct)
    {
        var eventItem = await _eventService.UpdateAsync(
            id,
            request.Title!,
            request.Description,
            request.StartAt!.Value,
            request.EndAt!.Value,
            ct
        );
        return Ok(MapToResponse(eventItem));
    }

    /// <summary>
    /// Метод удаляет мероприятие.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _eventService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Преобразует Event в EventResponse для ответа.
    /// </summary>
    /// <param name="eventItem">Мероприятие.</param>
    /// <returns></returns>
    private static EventResponse MapToResponse(Event eventItem) => new()
    {
        Id = eventItem.Id,
        Title = eventItem.Title,
        Description = eventItem.Description,
        StartAt = eventItem.StartAt,
        EndAt = eventItem.EndAt,
        TotalSeats = eventItem.TotalSeats,
        AvailableSeats = eventItem.AvailableSeats
    };
}