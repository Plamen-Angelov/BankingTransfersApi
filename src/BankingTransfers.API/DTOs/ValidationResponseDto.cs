namespace BankingTransfers.API.DTOs;

public class ValidationResponseDto
{
    public bool IsValid { get; set; }
    public List<string>? Errors { get; set; }
}
