using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TreeEditor.Contracts;
using TreeEditor.Services;

namespace TreeEditor.Controllers;

[ApiController]
[Route("api/cache")]
public sealed class CacheController : ControllerBase
{
    private readonly TreeService _service;

    public CacheController(TreeService service) => _service = service;

    [HttpPost("load/{id:long}")]
    public async Task<ActionResult<LoadedItem>> Load(long id)
    {
        var node = await _service.LoadAsync(id);
        return node is null ? NotFound() : Ok(node);
    }

    [HttpPost("apply")]
    public async Task<ActionResult<ApplyResult>> Apply(ApplyRequest request)
    {
        try
        {
            return Ok(await _service.ApplyAsync(request));
        }
        catch (Exception error) when (error is InvalidOperationException or DbUpdateException)
        {
            return BadRequest(new { error = error.Message });
        }
    }
}
