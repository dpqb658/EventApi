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
    /// Метод получает список мероприятий.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<PaginatedResult<EventResponse>> GetAll([FromQuery] EventFilterParameters parameters)
    {
        var events = _eventService.GetEventList(
            parameters.Title,
            parameters.From,
            parameters.To,
            parameters.Page,
            parameters.PageSize);

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
    /// Метод получает мероприятие по ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<EventResponse> GetById(Guid id)
    {
        var eventItem = _eventService.GetById(id);

        return Ok(MapToResponse(eventItem));
    }

    /// <summary>
    /// Метод создаёт новое мероприятие.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<EventResponse> Create([FromBody] CreateEventRequest request)
    {
        var eventItem = _eventService.Create(
            request.Title!,
            request.Description,
            request.StartAt!.Value,
            request.EndAt!.Value);

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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<EventResponse> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var eventItem = _eventService.Update(
            id,
            request.Title!,
            request.Description,
            request.StartAt!.Value,
            request.EndAt!.Value);

        return Ok(MapToResponse(eventItem));
    }

    /// <summary>
    /// Метод удаляет мероприятие.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        _eventService.Delete(id);
        return NoContent();
    }

    /// <summary>
    /// Преобразует Event в EventResponse для ответа.
    /// </summary>
    /// <param name="eventItem"></param>
    /// <returns></returns>
    private static EventResponse MapToResponse(Event eventItem) => new()
    {
        Id = eventItem.Id,
        Title = eventItem.Title,
        Description = eventItem.Description,
        StartAt = eventItem.StartAt,
        EndAt = eventItem.EndAt
    };
}