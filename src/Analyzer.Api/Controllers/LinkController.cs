namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/links")]
public class LinkController(IGraphService graphService) : ControllerBase
{
    readonly IGraphService _graphService = graphService;

    [HttpGet]
    public async Task<IActionResult> GetAllLinks()
    {
        try
        {
            var componentDtos = await _graphService.GetAllLinksAsync();

            return Ok(componentDtos);
        }
        catch (KeyNotFoundException)
        {
            return Problem(detail: "Ошибка получения связей из базы данных", statusCode: 500);
        }
        catch
        {
            return Problem(detail: "Неизвестная ошибка", statusCode: 500);
        }
    }
}