namespace Analyzer.Shared.DTO;

public record ITSystemDto(Guid Id, string Name, string Description, 
                          DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, Guid TeamId);

public record CreateITSystemDto(string Name, string Description, Guid TeamId);