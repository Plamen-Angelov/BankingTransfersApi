using BankingTransfers.Infrastructure.Data;
using BankingTransfers.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BankingTransfers.Tests.Repositories;

public class TransferRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TransferRepository _repository;

    public TransferRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new TransferRepository(_context);

        SeedUserProfile();
    }

    public void Dispose() => _context.Dispose();

    private UserProfile SeedUserProfile()
    {
        var user = new UserProfile { UId = Guid.NewGuid(), Username = "testuser" };
        _context.UserProfiles.Add(user);
        _context.SaveChanges();
        return user;
    }

    private TransferRequest BuildTransfer(int userProfileId, string? idempotencyKey = null) => new()
    {
        UId = Guid.NewGuid(),
        UserProfileId = userProfileId,
        SourceIban = "DE89370400440532013000",
        TargetIban = "DE89370400440532013001",
        Amount = 100m,
        Currency = "EUR",
        Reason = "Test",
        ExecutionDate = DateTime.UtcNow.Date,
        Status = TransferStatus.Pending,
        IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString(),
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetByUIdAsync_ReturnsNull_WhenTransferDoesNotExist()
    {
        var result = await _repository.GetByUIdAsync(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUIdAsync_ReturnsTransfer_WhenExists()
    {
        var user = _context.UserProfiles.First();
        var transfer = BuildTransfer(user.Id);
        await _repository.AddAsync(transfer, CancellationToken.None);

        var result = await _repository.GetByUIdAsync(transfer.UId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.UId.Should().Be(transfer.UId);
        result.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_ReturnsNull_WhenKeyDoesNotExist()
    {
        var result = await _repository.GetByIdempotencyKeyAsync("nonexistent-key", CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_ReturnsTransfer_WhenKeyExists()
    {
        var user = _context.UserProfiles.First();
        var idempotencyKey = "unique-key-abc123";
        var transfer = BuildTransfer(user.Id, idempotencyKey);
        await _repository.AddAsync(transfer, CancellationToken.None);

        var result = await _repository.GetByIdempotencyKeyAsync(idempotencyKey, CancellationToken.None);

        result.Should().NotBeNull();
        result!.IdempotencyKey.Should().Be(idempotencyKey);
    }

    [Fact]
    public async Task AddAsync_PersistsTransfer()
    {
        var user = _context.UserProfiles.First();
        var transfer = BuildTransfer(user.Id);

        await _repository.AddAsync(transfer, CancellationToken.None);

        var saved = await _context.TransferRequests.FirstOrDefaultAsync(t => t.UId == transfer.UId);
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(TransferStatus.Pending);
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        var user = _context.UserProfiles.First();
        var transfer = BuildTransfer(user.Id);
        await _repository.AddAsync(transfer, CancellationToken.None);

        transfer.Status = TransferStatus.Processed;
        transfer.ProcessedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(transfer, CancellationToken.None);

        var updated = await _context.TransferRequests.FirstAsync(t => t.UId == transfer.UId);
        updated.Status.Should().Be(TransferStatus.Processed);
        updated.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_PersistsFailureDetails()
    {
        var user = _context.UserProfiles.First();
        var transfer = BuildTransfer(user.Id);
        await _repository.AddAsync(transfer, CancellationToken.None);

        transfer.Status = TransferStatus.Failed;
        transfer.ErrorMessage = "Transfer failed after maximum retry attempts.";
        transfer.RetryCount = 3;
        await _repository.UpdateAsync(transfer, CancellationToken.None);

        var updated = await _context.TransferRequests.FirstAsync(t => t.UId == transfer.UId);
        updated.Status.Should().Be(TransferStatus.Failed);
        updated.ErrorMessage.Should().Be("Transfer failed after maximum retry attempts.");
        updated.RetryCount.Should().Be(3);
    }
}
