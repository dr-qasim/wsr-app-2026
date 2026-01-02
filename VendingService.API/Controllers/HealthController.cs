using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Data;

namespace VendingService.API.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(VendingServiceDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        return Ok(new
        {
            Status = canConnect ? "Ok" : "DbUnavailable",
            TimeUtc = DateTimeOffset.UtcNow
        });
    }
}

