namespace Analyzer.Shared.DTO;

using Analyzer.Domain.Enums;

public record CreateComponentDto(ComponentType Type, string Name) { }