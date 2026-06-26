using BankingTransfers.Domain.Entities;

namespace BankingTransfers.Application.Interfaces;

public interface ITransferRepository
{
    Task<TransferRequest?> GetByUIdAsync(Guid uid, CancellationToken cancellationToken);
    Task<TransferRequest?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<List<TransferRequest>> ClaimPendingTransfersAsync(CancellationToken cancellationToken);
    Task AddAsync(TransferRequest transfer, CancellationToken cancellationToken);
    Task UpdateAsync(TransferRequest transfer, CancellationToken cancellationToken);
}
