using BankingTransfers.Application.Common;

namespace BankingTransfers.Application.Transfers.Create;

public record CreateTransferResponse(ResultStatus Status, Guid? UId = null, string? TransferStatus = null, List<string>? Errors = null);
