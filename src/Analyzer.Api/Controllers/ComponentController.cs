namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Mvc;
using Serilog;

[ApiController]
[Route("api/v1/components")]
public class ComponentController(IGraphService graphService) : ControllerBase
{
    readonly IGraphService _graphService = graphService;

    [HttpGet]
    public async Task<IActionResult> GetAllComponentsBySystemId([FromQuery] Guid systemId)
    {
        try
        {
            var componentDtos = await _graphService.GetComponentsBySystemIdAsync(systemId);

            return Ok(componentDtos);
        }
        catch (KeyNotFoundException)
        {
            return Problem(detail: "Ошибка получения компонентов из базы данных", statusCode: 500);
        }
        catch
        {
            return Problem(detail: "Неизвестная ошибка", statusCode: 500);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateComponent([FromBody] CreateComponentDto dto)
    {
        var guid = await _graphService.CreateComponentAsync(dto);

        return Ok(guid);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetComponentDetails(Guid id)
    {
        try
        {
            var componentDto = await _graphService.GetComponentDetailsAsync(id);

            return Ok(componentDto);
        }
        catch (KeyNotFoundException)
        {
            return Problem(detail: "Ошибка получения компонента из базы данных", statusCode: 500);
        }
        catch
        {
            return Problem(detail: "Неизвестная ошибка", statusCode: 500);
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateComponent([FromBody] ComponentDto dto)
    {
        try
        {
            await _graphService.UpdateComponentAsync(dto);

            return Ok();
        }
        catch (Exception e)
        {
            Log.Error($"Ошибка обновления компонента: {e.Message}");
            return Problem(detail: $"Ошибка обновления компонента: {e.Message}", statusCode: 500);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComponent(Guid id)
    {
        try
        {
            await _graphService.DeleteComponentAsync(id);

            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return Problem(detail: "Ошибка удаления компонента из базы данных", statusCode: 500);
        }
        catch
        {
            return Problem(detail: "Неизвестная ошибка", statusCode: 500);
        }
    }
}