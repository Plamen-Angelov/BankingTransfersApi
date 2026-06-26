using BankingTransfers.Application.Common;

namespace BankingTransfers.Application.Transfers.Get;

public record GetTransferResponse(
    ResultStatus Status,
    Guid? UId = null,
    string? SourceIban = null,
    string? TargetIban = null,
    decimal? Amount = null,
    string? Currency = null,
    string? Reason = null,
    DateTime? ExecutionDate = null,
    string? TransferStatus = null,
    DateTime? CreatedAt = null,
    DateTime? ProcessingStartedAt = null,
    DateTime? ProcessedAt = null,
    int? RetryCount = null,
    string? ErrorMessage = null,
    List<string>? Errors = null);
