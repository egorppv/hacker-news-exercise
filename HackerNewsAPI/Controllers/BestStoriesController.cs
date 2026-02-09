using HackerNewsAPI.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using HackerNewsAPI.Models;

namespace HackerNewsAPI.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class BestStoriesController(IMemoryCache cache) : ControllerBase
{
    [HttpGet("{n}")]
    public IActionResult GetTop(int n)
    {
        if (n <= 0) return BadRequest(new { error = "n must be > 0" });

        if (!cache.TryGetValue<List<StoryModel>>(CommonConstants.CacheKey, out var stories) || stories == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Service unavailable" });
        }

        // already sorted
        var result = stories.Take(n).ToList();
        return Ok(result);
    }
}
