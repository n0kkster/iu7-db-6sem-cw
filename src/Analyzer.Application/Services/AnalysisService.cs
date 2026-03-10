namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces;

public class AnalysisService(IGraphRepository repository) : IAnalysisService
{
    readonly IGraphRepository _repository = repository;

    public async Task<List<Guid>> GetImpactedComponentsAsync(Guid failedComponentId)
    {
        var components = await _repository.GetImpactedComponentsAsync(failedComponentId);
        return components.Select(component => component.Id).ToList();
    }
}