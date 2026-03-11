namespace Analyzer.Application.Interfaces;

public interface IAnalysisService
{
    Task<List<Guid>> GetImpactedComponentsAsync(Guid failedComponentId);
}