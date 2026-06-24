using BankingTransfers.Domain.Enums;

namespace BankingTransfers.Domain.Entities;

public class TransferRequest
{
    public int Id { get; set; }
    public Guid UId { get; set; }
    public int UserProfileId { get; set; }
    public string SourceIban { get; set; } = string.Empty;
    public string TargetIban { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ExecutionDate { get; set; }
    public TransferStatus Status { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? LastRetryAt { get; set; }

    public UserProfile UserProfile { get; set; } = null!;
}
