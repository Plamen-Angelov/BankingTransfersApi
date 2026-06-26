using BankingTransfers.Application.Interfaces;
using BankingTransfers.Infrastructure.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BankingTransfers.Tests.BackgroundServices;

public class BackgroundTransferProcessorTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ITransferRepository> _transferRepoMock = new();
    private readonly Mock<ILogger<BackgroundTransferProcessor>> _loggerMock = new();
    private readonly BackgroundTransferProcessor _processor;

    public BackgroundTransferProcessorTests()
    {
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ITransferRepository)))
            .Returns(_transferRepoMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);

        _processor = new BackgroundTransferProcessor(_scopeFactoryMock.Object, _loggerMock.Object);
    }

    private static TransferRequest BuildTransfer(decimal amount) => new()
    {
        UId = Guid.NewGuid(),
        UserProfileId = 1,
        SourceIban = "DE89370400440532013000",
        TargetIban = "DE89370400440532013001",
        Amount = amount,
        Currency = "EUR",
        Reason = "Test",
        ExecutionDate = DateTime.UtcNow.Date,
        Status = TransferStatus.Processing,
        IdempotencyKey = Guid.NewGuid().ToString(),
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AmountBelowThreshold_SetsStatusToProcessed()
    {
        var transfer = BuildTransfer(5_000m);
        var updateCompleted = new TaskCompletionSource();

        _transferRepoMock
            .Setup(r => r.ClaimPendingTransfersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([transfer]);
        _transferRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TransferRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TransferRequest, CancellationToken>((_, _) => updateCompleted.TrySetResult())
            .Returns(Task.CompletedTask);

        await _processor.StartAsync(CancellationToken.None);
        await updateCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _processor.StopAsync(CancellationToken.None);

        _transferRepoMock.Verify(r => r.UpdateAsync(
            It.Is<TransferRequest>(t => t.Status == TransferStatus.Processed && t.ProcessedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AmountAboveLimit_SetsStatusToFailed_WithExpectedMessage()
    {
        var transfer = BuildTransfer(25_000m);
        var updateCompleted = new TaskCompletionSource();

        _transferRepoMock
            .Setup(r => r.ClaimPendingTransfersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([transfer]);
        _transferRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TransferRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TransferRequest, CancellationToken>((_, _) => updateCompleted.TrySetResult())
            .Returns(Task.CompletedTask);

        await _processor.StartAsync(CancellationToken.None);
        await updateCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _processor.StopAsync(CancellationToken.None);

        _transferRepoMock.Verify(r => r.UpdateAsync(
            It.Is<TransferRequest>(t =>
                t.Status == TransferStatus.Failed &&
                t.ErrorMessage == "Transfer rejected. Amount exceeds CoreSystem limit."),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoPendingTransfers_DoesNotCallUpdateAsync()
    {
        var cts = new CancellationTokenSource();
        var claimCallCount = 0;

        _transferRepoMock
            .Setup(r => r.ClaimPendingTransfersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                claimCallCount++;
                if (claimCallCount >= 1) cts.CancelAfter(50);
                return [];
            });

        await _processor.StartAsync(cts.Token);
        try { await Task.Delay(200, cts.Token); } catch (OperationCanceledException) { }
        await _processor.StopAsync(CancellationToken.None);

        _transferRepoMock.Verify(r => r.UpdateAsync(It.IsAny<TransferRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AmountInRetryRange_CallsUpdateAsyncOnce_WithFinalStatus()
    {
        // The retry range (10k-20k) uses random success/failure — verify UpdateAsync is called
        // exactly once with a final status (Processed or Failed), not multiple times during retries.
        var transfer = BuildTransfer(15_000m);
        var updateCompleted = new TaskCompletionSource();

        _transferRepoMock
            .Setup(r => r.ClaimPendingTransfersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([transfer]);
        _transferRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TransferRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TransferRequest, CancellationToken>((_, _) => updateCompleted.TrySetResult())
            .Returns(Task.CompletedTask);

        await _processor.StartAsync(CancellationToken.None);
        // Retry range can take up to MaxRetries * 3s = 9s, allow up to 15s
        await updateCompleted.Task.WaitAsync(TimeSpan.FromSeconds(15));
        await _processor.StopAsync(CancellationToken.None);

        _transferRepoMock.Verify(r => r.UpdateAsync(
            It.Is<TransferRequest>(t => t.Status == TransferStatus.Processed || t.Status == TransferStatus.Failed),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
