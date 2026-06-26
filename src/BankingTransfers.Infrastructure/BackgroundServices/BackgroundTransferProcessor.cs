using BankingTransfers.Application.Interfaces;
using BankingTransfers.Domain.Entities;
using BankingTransfers.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace BankingTransfers.Infrastructure.BackgroundServices;

public class BackgroundTransferProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundTransferProcessor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(3);
    private const int MaxRetries = 3;

    public BackgroundTransferProcessor(IServiceScopeFactory scopeFactory, ILogger<BackgroundTransferProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingTransfersAsync(stoppingToken);
            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingTransfersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var transferRepository = scope.ServiceProvider.GetRequiredService<ITransferRepository>();

        var transfers = await transferRepository.ClaimPendingTransfersAsync(cancellationToken);

        if (transfers.Count == 0)
            return;

        _logger.LogInformation("Claimed {Count} pending transfer(s) for processing.", transfers.Count);

        foreach (var transfer in transfers)
        {
            _logger.LogInformation("Starting processing of transfer {UId}, amount {Amount} {Currency}.", transfer.UId, transfer.Amount, transfer.Currency);
            await ProcessTransferAsync(transfer, transferRepository, cancellationToken);
        }
    }

    private async Task ProcessTransferAsync(TransferRequest transfer, ITransferRepository repository, CancellationToken cancellationToken)
    {
        if (transfer.Amount < 10_000)
        {
            transfer.Status = TransferStatus.Processed;
            transfer.ProcessedAt = DateTime.UtcNow;
            _logger.LogInformation("Transfer {UId} processed successfully.", transfer.UId);
        }
        else if (transfer.Amount > 20_000)
        {
            transfer.Status = TransferStatus.Failed;
            transfer.ErrorMessage = "Transfer rejected. Amount exceeds CoreSystem limit.";
            _logger.LogWarning("Transfer {UId} failed permanently — amount exceeds CoreSystem limit.", transfer.UId);
        }
        else
        {
            var pipeline = new ResiliencePipelineBuilder<bool>()
                .AddRetry(new RetryStrategyOptions<bool>
                {
                    MaxRetryAttempts = MaxRetries,
                    Delay = _retryDelay,
                    ShouldHandle = new PredicateBuilder<bool>().HandleResult(false),
                    OnRetry = args =>
                    {
                        transfer.RetryCount = args.AttemptNumber + 1;
                        transfer.LastRetryAt = DateTime.UtcNow;
                        _logger.LogInformation("Transfer {UId} temporary error, retry {RetryCount}/{MaxRetries}.",
                            transfer.UId, transfer.RetryCount, MaxRetries);
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();

            var succeeded = await pipeline.ExecuteAsync(async _ =>
                Random.Shared.NextDouble() < 0.3, cancellationToken);

            if (succeeded)
            {
                transfer.Status = TransferStatus.Processed;
                transfer.ProcessedAt = DateTime.UtcNow;
                _logger.LogInformation("Transfer {UId} processed successfully after {RetryCount} retry attempt(s).", transfer.UId, transfer.RetryCount);
            }
            else
            {
                transfer.Status = TransferStatus.Failed;
                transfer.ErrorMessage = "Transfer failed after maximum retry attempts.";
                _logger.LogWarning("Transfer {UId} failed after {MaxRetries} retry attempts.", transfer.UId, MaxRetries);
            }
        }

        await repository.UpdateAsync(transfer, cancellationToken);
    }
}
