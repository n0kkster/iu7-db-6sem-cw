namespace Analyzer.Application.Interfaces;

using Analyzer.Shared.DTO;

public interface IGraphService
{
    Task<Guid> CreateComponentAsync(CreateComponentDto dto);
}