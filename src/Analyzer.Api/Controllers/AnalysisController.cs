namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/analysis")]
public class AnalysisController(IAnalysisService analysisService) : ControllerBase
{
    readonly IAnalysisService _analysisService = analysisService;

    [HttpGet("simulate/{failedComponentId}")]
    public async Task<IActionResult> GetImpactedComponents(Guid failedComponentId)
    {
        try
        {
            var componentDtos = await _analysisService.GetImpactedComponentsAsync(failedComponentId);

            return Ok(componentDtos);
        }
        catch (KeyNotFoundException)
        {
            return Problem(detail: "Ошибка получения критического пути", statusCode: 500);
        }
        catch
        {
            return Problem(detail: "Неизвестная ошибка", statusCode: 500);
        }
    }
}