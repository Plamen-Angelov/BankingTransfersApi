using MediatR;

namespace BankingTransfers.Application.Transfers.Validate;

public record ValidateTransferRequest : IRequest<ValidateTransferResponse>, ITransferFields
{
    public Guid UserProfileUId { get; init; }
    public string SourceIban { get; init; } = string.Empty;
    public string TargetIban { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTime ExecutionDate { get; init; }
}
