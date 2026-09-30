using Microsoft.AspNetCore.Mvc;
using TreeEditor.Contracts;
using TreeEditor.Services;

namespace TreeEditor.Controllers;

[ApiController]
[Route("api")]
public sealed class TreeController : ControllerBase
{
    private readonly TreeService _service;

    public TreeController(TreeService service) => _service = service;

    [HttpGet("tree")]
    public async Task<ActionResult<TreePage>> GetTree([FromQuery] long? parentId, [FromQuery] int offset = 0) =>
        Ok(await _service.GetChildrenAsync(parentId, offset));

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await _service.ResetAsync();
        return Ok();
    }
}
