namespace BankingTransfers.API.DTOs;

public class CreateTransferResponseDto
{
    public Guid UId { get; set; }
    public string Status { get; set; } = string.Empty;
}
