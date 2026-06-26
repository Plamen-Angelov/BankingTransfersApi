using MediatR;

namespace BankingTransfers.Application.Transfers.Create;

public record CreateTransferRequest : IRequest<CreateTransferResponse>, ITransferFields
{
    public Guid UserProfileUId { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public string SourceIban { get; init; } = string.Empty;
    public string TargetIban { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTime ExecutionDate { get; init; }
}
