using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Application.Services;

namespace ShipmentPlatform.Api.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentsController(IShipmentService shipmentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShipmentResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var shipments = await shipmentService.GetAllAsync(cancellationToken);
        return Ok(shipments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShipmentResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await shipmentService.GetByIdAsync(id, cancellationToken);
        return shipment is null ? NotFound() : Ok(shipment);
    }

    [AllowAnonymous]
    [HttpGet("tracking/{trackingCode}")]
    public async Task<ActionResult<ShipmentResponse>> GetByTrackingCode(
        string trackingCode,
        CancellationToken cancellationToken)
    {
        var shipment = await shipmentService.GetByTrackingCodeAsync(trackingCode, cancellationToken);
        return shipment is null ? NotFound() : Ok(shipment);
    }

    [HttpPost]
    public async Task<ActionResult<ShipmentResponse>> Create(
        [FromBody] CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var shipment = await shipmentService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, shipment);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ShipmentResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateShipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var shipment = await shipmentService.UpdateStatusAsync(id, request.Status, cancellationToken);
        return shipment is null ? NotFound() : Ok(shipment);
    }
}
