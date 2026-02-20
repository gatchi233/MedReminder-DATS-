using MedReminder.Api.Data;
using MedReminder.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedReminder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ResidentsController : ControllerBase
{
    private readonly CareHubDbContext _db;
    public ResidentsController(CareHubDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Resident>>> GetAll(CancellationToken ct)
        => await _db.Residents.AsNoTracking().OrderBy(r => r.LastName).ToListAsync(ct);
}