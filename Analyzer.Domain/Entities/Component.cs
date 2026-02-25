namespace Analyzer.Domain.Entities;

using Analyzer.Domain.Enums;
using Analyzer.Domain.Exceptions;

public class Component
{
    public Guid Id { get; init; }
    public ComponentType Type { get; init; }
    public string Name
    {
        get;
        set
        {
            if (value.Length == 0)
                throw new InvalidComponentNameException("Имя компонента не может быть пустой строкой");
            field = value;
        }
    }

    public List<Link> Links { get; } = [];

    public Component(string name, ComponentType type, Guid guid) 
    {
        Name = name;
        Type = type;
        Id = guid;        
    }

    public Component(string name, ComponentType type)
        : this(name, type, Guid.NewGuid())
    { }
}