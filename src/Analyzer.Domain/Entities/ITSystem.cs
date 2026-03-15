namespace Analyzer.Domain.Entities;

public class ITSystem
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ITSystem(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = string.IsNullOrWhiteSpace(name) 
            ? throw new ArgumentException("Имя системы обязательно", nameof(name)) 
            : name;

        Description = description;
        
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ТЕСТОВЫЙ КОНСТРУКТОР
    public ITSystem(Guid id, string name, string description)
    {
        Id = id;
        Name = string.IsNullOrWhiteSpace(name) 
            ? throw new ArgumentException("Имя системы обязательно", nameof(name)) 
            : name;

        Description = description;
        
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(string name, string description)
    {
        Name = string.IsNullOrWhiteSpace(name) 
            ? throw new ArgumentException("Имя системы обязательно", nameof(name)) 
            : name;
        Description = description;
        
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}