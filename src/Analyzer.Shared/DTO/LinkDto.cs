namespace Analyzer.Shared.DTO;

using Analyzer.Domain.Enums;

public record LinkDto(Guid SourceId, Guid TargetId, LinkSeverity Severity, ProtocolType Protocol);