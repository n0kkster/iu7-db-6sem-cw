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

    public string Description
    {
        get;
        set
        {
            if (value.Length == 0)
                throw new InvalidComponentNameException("Описание компонента не может быть пустой строкой");
            field = value;
        }
    }


    public Component(string name, ComponentType type, string desription, Guid guid) 
    {
        Name = name;
        Type = type;
        Id = guid;
        Description = desription;
    }

    public Component(string name, ComponentType type, string desription)
        : this(name, type, desription, Guid.NewGuid())
    { }
}