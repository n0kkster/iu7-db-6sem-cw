namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces.Services;
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

    [HttpPost]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkDto linkDto)
    {
        try
        {
            var guid = await _graphService.CreateLinkAsync(linkDto);

            return Ok(guid);
        }
        catch
        {
            return Problem(detail: "Ошибка создания связи", statusCode: 500);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLink(Guid id)
    {
        try
        {
            await _graphService.DeleteLinkAsync(id);

            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return Problem(detail: "Ошибка удаления связи из базы данных", statusCode: 500);
        }
        catch
        {
            return Problem(detail: "Неизвестная ошибка", statusCode: 500);
        }
    }
}