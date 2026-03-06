namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Mvc;
using Serilog;

[ApiController]
[Route("api/[controller]")]
public class ComponentController(IGraphService graphService) : ControllerBase
{
    readonly IGraphService _graphService = graphService;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return Ok("Component index page");
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateComponent([FromBody] CreateComponentDto dto)
    {
        var guid = await _graphService.CreateComponentAsync(dto);

        return Ok(guid);
    }

    [HttpGet("get")]
    public async Task<IActionResult> GetAllComponents()
    {
        try
        {
            var componentDtos = await _graphService.GetAllComponentsAsync();

            return Ok(componentDtos);
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

    [HttpGet("get/{id}")]
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

    [HttpPost("update")]
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