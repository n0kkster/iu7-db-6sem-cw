namespace Analyzer.Client.Models;

using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Geometry;
using Analyzer.Domain.Enums;

public class ComponentModel : NodeModel
{
    public Guid ComponentId { get; set; }
    public string Name { get; set; }
    public ComponentType Type { get; set; }
    public string Status { get; set; } = "Healthy";

    public ComponentModel(Guid id, string name, ComponentType type, 
                          Point? position = null) 
        : base(position ?? Point.Zero)
    {
        ComponentId = id;
        Name = name;
        Type = type;
        
        AddPort(PortAlignment.Top);
        AddPort(PortAlignment.Bottom);
        AddPort(PortAlignment.Left);
        AddPort(PortAlignment.Right);
    }
}