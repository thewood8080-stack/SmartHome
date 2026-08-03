using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHomeApi.Data;
using SmartHomeApi.DTOs;
using SmartHomeApi.Services;

namespace SmartHomeApi.Controllers;

/// <summary>נתוני Application state — למנהל בלבד.</summary>
[ApiController]
[Route("api/stats")]
[Authorize(Roles = DbSeeder.ManagerRole)]
[Produces("application/json")]
public class StatsController : ControllerBase
{
    private readonly IAppStatsService _stats;

    public StatsController(IAppStatsService stats)
    {
        _stats = stats;
    }

    [HttpGet]
    [ProducesResponseType(typeof(StatsDto), StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new StatsDto(
        _stats.TotalVisitors,
        _stats.ConnectedUsers,
        _stats.SinceUtc));
}
