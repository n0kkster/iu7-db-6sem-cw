namespace Analyzer.Application.Interfaces.Services;

public interface IAnalysisService
{
    Task<List<Guid>> GetImpactedComponentsAsync(Guid failedComponentId);
}