using API_PI_Clubes.Application.DTOs;
using API_PI_Clubes.Application.Interfaces.IServices;
using API_PI_Clubes.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_PI_Clubes.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingController : ControllerBase
{
    private readonly ISettingService _service;
    public SettingController(ISettingService service) => _service = service;

    [HttpGet("Backoffice")]
    [Authorize(Roles = "Root")]
    public async Task<ActionResult<List<ResponseSettingBackofficeDTO>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllForBackofficeAsync(ct));

    [HttpPut("Backoffice")]
    [Authorize(Roles = "Root")]
    public async Task<IActionResult> Save([FromBody] SaveSettingDTO dto, CancellationToken ct)
    {
        await _service.SaveForBackofficeAsync(dto, User.GetUserId(), ct);
        return NoContent();
    }
}