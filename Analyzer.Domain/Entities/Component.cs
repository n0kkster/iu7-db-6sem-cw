namespace Analyzer.Domain.Entities;

using Analyzer.Domain.Enums;
using Analyzer.Domain.Exceptions;

public class Component
{
    public Guid Id { get; } = Guid.NewGuid();
    public required ComponentType Type { get; init; }
    public required string Name
    {
        get;
        set
        {
            if (value.Length == 0)
                throw new InvalidComponentNameException("Имя компонента не может быть пустой строкой");
            field = value;
        }
    }
}