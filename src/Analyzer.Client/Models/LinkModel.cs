using Analyzer.Domain.Enums;
using Blazor.Diagrams.Core.Models;

namespace Analyzer.Client.Models;

public class LinkModel(PortModel sourcePort, PortModel targetPort) 
    : Blazor.Diagrams.Core.Models.LinkModel(sourcePort, targetPort)
{
    public Guid LinkId { get; set; } 
    public LinkSeverity Severity { get; set; }
    public ProtocolType Protocol { get; set; }
}