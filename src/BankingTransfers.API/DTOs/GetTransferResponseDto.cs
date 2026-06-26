namespace BankingTransfers.API.DTOs;

public class GetTransferResponseDto
{
    public Guid UId { get; set; }
    public string SourceIban { get; set; } = string.Empty;
    public string TargetIban { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime ExecutionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}
