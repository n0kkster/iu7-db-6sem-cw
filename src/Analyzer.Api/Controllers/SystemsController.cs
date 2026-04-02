namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

[Authorize]
[ApiController]
[Route("api/v1/systems")]
public class SystemsController(ISystemService systemsService) : ControllerBase
{
    readonly ISystemService _systemsService = systemsService;

    private record SystemStorage(IReadOnlyCollection<ComponentDto> Components, 
                                 IReadOnlyCollection<LinkDto> Links);

    [HttpGet]    
    public async Task<IActionResult> ListSystemsByTeam([FromQuery] Guid teamId)
    {
        var systems = await _systemsService.GetSystemsByTeamIdAsync(teamId);
        return Ok(systems);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportSystem([FromQuery] Guid systemId)
    {
        var (components, links) = await _systemsService.ExportSystem(systemId);
        var jsonString = JsonSerializer.Serialize(new SystemStorage(components, links));
        var bytes = Encoding.UTF8.GetBytes(jsonString);
        var fileName = $"system-backup-{DateTime.Now:yyyy-MM-dd_HH-mm}.json";

        return File(bytes, "application/json", fileName);
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportSystem([FromForm] IFormFile file)
    {
        try
        {
            if (file is null || file.Length == 0)
                return Problem("Ошибка получения файла", statusCode: 500);

            using var stream = file.OpenReadStream();
            var result = await JsonSerializer.DeserializeAsync<SystemStorage>(stream);

            if (result is null)
                return Problem("Ошибка десереализации файла", statusCode: 500);

            var (components, links) = result;

            return Ok(Guid.NewGuid());
        }
        catch
        {
            return Problem(detail: "Неизвестная ошибка", statusCode: 500);
        }
    }
}