namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Mvc;

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
}