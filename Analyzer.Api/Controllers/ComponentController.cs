namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces;
using Analyzer.Shared.DTO;
using Analyzer.Domain.Enums;
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

    [HttpGet("create")]
    public async Task<IActionResult> CreateService()
    {
        CreateComponentDto dto = new(ComponentType.Microservice, "Test");
        Console.WriteLine("CreateService called!");
        var guid = await _graphService.CreateComponentAsync(dto);
        Console.WriteLine("Created service!");

        return Ok(guid);
    }
}