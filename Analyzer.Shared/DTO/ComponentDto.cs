namespace Analyzer.Shared.DTO;

using Analyzer.Domain.Enums;

public record CreateComponentDto(ComponentType Type, string Name, string Description) { }
public record ComponentDto(Guid Id, ComponentType Type, string Name, string Description) { }