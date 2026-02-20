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
    public async Task<IActionResult> CreateService([FromBody] CreateComponentDto dto)
    {
        Console.WriteLine("CreateService called!");
        var guid = await _graphService.CreateComponentAsync(dto);
        Console.WriteLine("Created service!");

        return Ok(guid);
    }
}