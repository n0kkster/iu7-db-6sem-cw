namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/v1/systems")]
public class SystemsController(ISystemsService systemsService) : ControllerBase
{
    readonly ISystemsService _systemsService = systemsService;

    private record SystemStorage(List<ComponentDto> Components, List<LinkDto> Links);

    [HttpGet("export")]
    public async Task<IActionResult> ExportSystem()
    {
        try
        {
            var (components, links) = await _systemsService.ExportSystem();
            var jsonString = JsonSerializer.Serialize(new SystemStorage(components, links));
            var bytes = Encoding.UTF8.GetBytes(jsonString);
            var fileName = $"system-backup-{DateTime.Now:yyyy-MM-dd_HH-mm}.json";

            return File(bytes, "application/json", fileName);
        }
        catch
        {
            return Problem(detail: "Неизвестная ошибка", statusCode: 500);
        }
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

            var guid = await _systemsService.ImportSystem(components, links);

            return Ok(guid);
        }
        catch
        {
            return Problem(detail: "Неизвестная ошибка", statusCode: 500);
        }
    }
}