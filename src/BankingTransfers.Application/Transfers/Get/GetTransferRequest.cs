using MediatR;

namespace BankingTransfers.Application.Transfers.Get;

public record GetTransferRequest : IRequest<GetTransferResponse>
{
    public Guid UserProfileUId { get; init; }
    public Guid TransferUId { get; init; }
}
