using CareHub.Api.Data;
using CareHub.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Api.Controllers;

[ApiController]
[Route("api/medicationorders")]
[Authorize]
public sealed class MedicationOrdersController : ControllerBase
{
    private readonly CareHubDbContext _db;

    public MedicationOrdersController(CareHubDbContext db) => _db = db;

    private static readonly string[] AllowedStatuses = { "Requested", "Ordered", "Received", "Cancelled" };

    // GET api/medicationorders
    [HttpGet]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.Admin}")]
    public async Task<ActionResult<List<MedicationOrder>>> GetAll(CancellationToken ct)
    {
        var list = await _db.MedicationOrders.AsNoTracking()
            .OrderByDescending(o => o.RequestedAt)
            .ToListAsync(ct);

        return Ok(list);
    }

    // GET api/medicationorders/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.Admin}")]
    public async Task<ActionResult<MedicationOrder>> GetById(Guid id, CancellationToken ct)
    {
        var order = await _db.MedicationOrders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null ? NotFound() : Ok(order);
    }

    // GET api/medicationorders/by-medication/{medicationId}
    [HttpGet("by-medication/{medicationId:guid}")]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.Admin}")]
    public async Task<ActionResult<List<MedicationOrder>>> GetByMedication(Guid medicationId, CancellationToken ct)
    {
        var list = await _db.MedicationOrders.AsNoTracking()
            .Where(o => o.MedicationId == medicationId)
            .OrderByDescending(o => o.RequestedAt)
            .ToListAsync(ct);

        return Ok(list);
    }

    // POST api/medicationorders
    [HttpPost]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.Admin}")]
    public async Task<ActionResult<MedicationOrder>> Create(
        [FromBody] CreateMedicationOrderRequest request,
        CancellationToken ct)
    {
        if (!await _db.Medications.AnyAsync(m => m.Id == request.MedicationId, ct))
            return NotFound($"Medication {request.MedicationId} not found.");

        var order = new MedicationOrder
        {
            MedicationId = request.MedicationId,
            RequestedQuantity = request.RequestedQuantity,
            Status = "Requested",
            RequestedAt = DateTimeOffset.UtcNow,
            RequestedBy = request.RequestedBy ?? "Staff",
            MedicationName = request.MedicationName,
            Notes = request.Notes,
        };

        _db.MedicationOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // PUT api/medicationorders/{id}/status
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.Admin}")]
    public async Task<ActionResult<MedicationOrder>> UpdateStatus(
        Guid id,
        [FromBody] UpdateMedicationOrderStatusRequest request,
        CancellationToken ct)
    {
        if (!AllowedStatuses.Contains(request.Status))
            return BadRequest($"Invalid status '{request.Status}'. Allowed: {string.Join(", ", AllowedStatuses)}.");

        var order = await _db.MedicationOrders.FindAsync(new object[] { id }, ct);
        if (order is null)
            return NotFound();

        // Validate transitions
        var isAllowed = order.Status switch
        {
            "Requested" => request.Status is "Ordered" or "Cancelled",
            "Ordered" => request.Status is "Received" or "Cancelled",
            _ => false
        };

        if (!isAllowed)
            return BadRequest($"Cannot transition from '{order.Status}' to '{request.Status}'.");

        order.Status = request.Status;

        switch (request.Status)
        {
            case "Ordered":
                order.OrderedAt ??= DateTimeOffset.UtcNow;
                order.OrderedBy ??= request.UpdatedBy;
                break;
            case "Received":
                order.ReceivedAt ??= DateTimeOffset.UtcNow;
                order.ReceivedBy ??= request.UpdatedBy;
                order.ReceivedExpiryDate = request.ExpiryDate;
                break;
            case "Cancelled":
                order.CancelledAt ??= DateTimeOffset.UtcNow;
                order.CancelledBy ??= request.UpdatedBy;
                break;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(order);
    }

    // DELETE api/medicationorders/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Nurse},{Roles.Admin}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var order = await _db.MedicationOrders.FindAsync(new object[] { id }, ct);
        if (order is null)
            return NotFound();

        _db.MedicationOrders.Remove(order);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}

// ──────────────────── DTOs ────────────────────

public record CreateMedicationOrderRequest
{
    public Guid MedicationId { get; init; }
    public int RequestedQuantity { get; init; }
    public string? RequestedBy { get; init; }
    public string? MedicationName { get; init; }
    public string? Notes { get; init; }
}

public record UpdateMedicationOrderStatusRequest
{
    public string Status { get; init; } = string.Empty;
    public string? UpdatedBy { get; init; }
    public DateTimeOffset? ExpiryDate { get; init; }
}
