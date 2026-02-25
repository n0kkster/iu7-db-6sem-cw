namespace Analyzer.Domain.Entities;

using Analyzer.Domain.Enums;

public class Link
{
    public Guid DependsOn { get; init; }
    public LinkSeverity Severity { get; set; }
}