namespace Analyzer.Application.Interfaces;

using Analyzer.Shared.DTO;

public interface IAnalysisService
{
    Task<List<Guid>> GetImpactedComponentsAsync(Guid failedComponentId);
}