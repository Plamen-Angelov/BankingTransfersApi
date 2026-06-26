using BankingTransfers.Application.Common;
using BankingTransfers.Application.Interfaces;
using MediatR;

namespace BankingTransfers.Application.Transfers.Get;

public class GetTransferHandler : IRequestHandler<GetTransferRequest, GetTransferResponse>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ITransferRepository _transferRepository;

    public GetTransferHandler(IUserProfileRepository userProfileRepository, ITransferRepository transferRepository)
    {
        _userProfileRepository = userProfileRepository;
        _transferRepository = transferRepository;
    }

    public async Task<GetTransferResponse> Handle(GetTransferRequest request, CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByUIdAsync(request.UserProfileUId, cancellationToken);
        if (userProfile == null)
            return new GetTransferResponse(ResultStatus.NotFound, Errors: ["User profile not found."]);

        var transfer = await _transferRepository.GetByUIdAsync(request.TransferUId, cancellationToken);
        if (transfer == null || transfer.UserProfileId != userProfile.Id)
            return new GetTransferResponse(ResultStatus.NotFound, Errors: ["Transfer not found."]);

        return new GetTransferResponse(
            Status: ResultStatus.Success,
            UId: transfer.UId,
            SourceIban: transfer.SourceIban,
            TargetIban: transfer.TargetIban,
            Amount: transfer.Amount,
            Currency: transfer.Currency,
            Reason: transfer.Reason,
            ExecutionDate: transfer.ExecutionDate,
            TransferStatus: transfer.Status.ToString(),
            CreatedAt: transfer.CreatedAt,
            ProcessingStartedAt: transfer.ProcessingStartedAt,
            ProcessedAt: transfer.ProcessedAt,
            RetryCount: transfer.RetryCount,
            ErrorMessage: transfer.ErrorMessage);
    }
}
