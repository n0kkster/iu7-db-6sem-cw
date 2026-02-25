namespace Analyzer.Shared.DTO;

using Analyzer.Domain.Enums;
using Analyzer.Domain.Entities;

public record CreateComponentDto(ComponentType Type, string Name) { }
public record ComponentDto(ComponentType Type, string Name, List<Link> Links) { }