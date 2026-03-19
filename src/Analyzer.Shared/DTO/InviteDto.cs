using Analyzer.Domain.Enums;

namespace Analyzer.Shared.DTO;

public class GenerateInviteDto
{
    public string Email { get; set; } = string.Empty;
    public Guid TeamId { get; set; }
    public int ValidForDays { get; set; }
    public Role Role { get; set; }
}

public class InviteDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset ExpirationDate { get; set; }
    public InviteStatus Status { get; set; }
    public Guid TeamId { get; set; }
    public Guid? ActivatedByUserId { get; set; }
}