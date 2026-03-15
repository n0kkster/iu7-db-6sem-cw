namespace Analyzer.Application.Interfaces.Services;

public interface IAnalysisService
{
    Task<IReadOnlyCollection<Guid>> GetImpactedComponentsAsync(Guid failedComponentId);
}