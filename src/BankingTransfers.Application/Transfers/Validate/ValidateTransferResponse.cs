using BankingTransfers.Application.Common;

namespace BankingTransfers.Application.Transfers.Validate;

public record ValidateTransferResponse(ResultStatus Status, List<string>? Errors = null);
