using BankingTransfers.Application.Interfaces;
using BankingTransfers.Domain.Entities;
using BankingTransfers.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingTransfers.Infrastructure.Repositories;

public class TransferRepository : ITransferRepository
{
    private readonly AppDbContext _context;

    public TransferRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TransferRequest?> GetByUIdAsync(Guid uid, CancellationToken cancellationToken)
    {
        return await _context.TransferRequests
            .FirstOrDefaultAsync(x => x.UId == uid, cancellationToken);
    }

    public async Task<TransferRequest?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        return await _context.TransferRequests
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<List<TransferRequest>> ClaimPendingTransfersAsync(CancellationToken cancellationToken)
    {
        var claimedUIds = new List<Guid>();

        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE TOP (2) TransferRequests
            SET Status = 'Processing', ProcessingStartedAt = GETUTCDATE()
            OUTPUT INSERTED.UId
            WHERE Status = 'Pending' AND CAST(ExecutionDate AS DATE) <= CAST(GETUTCDATE() AS DATE)
            """;

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                claimedUIds.Add(reader.GetGuid(0));
        }

        if (claimedUIds.Count == 0)
            return [];

        return await _context.TransferRequests
            .Where(t => claimedUIds.Contains(t.UId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TransferRequest transfer, CancellationToken cancellationToken)
    {
        _context.TransferRequests.Add(transfer);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TransferRequest transfer, CancellationToken cancellationToken)
    {
        _context.TransferRequests.Update(transfer);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
