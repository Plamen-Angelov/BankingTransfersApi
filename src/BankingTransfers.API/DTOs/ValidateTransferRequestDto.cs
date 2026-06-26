namespace BankingTransfers.API.DTOs;

public class ValidateTransferRequestDto
{
    public string SourceIban { get; set; } = string.Empty;
    public string TargetIban { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime ExecutionDate { get; set; }
}
