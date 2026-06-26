using BankingTransfers.Application.Common;
using BankingTransfers.Application.Interfaces;
using BankingTransfers.Domain.Entities;
using BankingTransfers.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankingTransfers.Application.Transfers.Create;

public class CreateTransferHandler : IRequestHandler<CreateTransferRequest, CreateTransferResponse>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly IValidator<CreateTransferRequest> _validator;
    private readonly ILogger<CreateTransferHandler> _logger;

    public CreateTransferHandler(
        IUserProfileRepository userProfileRepository,
        ITransferRepository transferRepository,
        IValidator<CreateTransferRequest> validator,
        ILogger<CreateTransferHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _transferRepository = transferRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<CreateTransferResponse> Handle(CreateTransferRequest request, CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByUIdAsync(request.UserProfileUId, cancellationToken);
        if (userProfile == null)
            return new CreateTransferResponse(ResultStatus.NotFound, Errors: ["User profile not found."]);

        var existing = await _transferRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existing != null)
        {
            _logger.LogInformation("Duplicate request detected for idempotency key {IdempotencyKey}, returning existing transfer {UId}.", request.IdempotencyKey, existing.UId);
            return new CreateTransferResponse(ResultStatus.Success, existing.UId, existing.Status.ToString());
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return new CreateTransferResponse(ResultStatus.ValidationFailure, Errors: errors);
        }

        var permission = await _userProfileRepository.GetAccountPermissionAsync(
            request.UserProfileUId, request.SourceIban, cancellationToken);

        if (permission == null || !permission.CreateTransferPermission)
            return new CreateTransferResponse(ResultStatus.ValidationFailure, Errors: ["Source IBAN is not associated with your profile or you don't have transfer permission."]);

        var transfer = new TransferRequest
        {
            UId = Guid.NewGuid(),
            UserProfileId = userProfile.Id,
            SourceIban = request.SourceIban,
            TargetIban = request.TargetIban,
            Amount = request.Amount,
            Currency = request.Currency,
            Reason = request.Reason,
            ExecutionDate = request.ExecutionDate,
            Status = TransferStatus.Pending,
            IdempotencyKey = request.IdempotencyKey,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _transferRepository.AddAsync(transfer, cancellationToken);

        _logger.LogInformation("Transfer {UId} created for user {UserProfileUId}, amount {Amount} {Currency}, execution date {ExecutionDate}.",
            transfer.UId, request.UserProfileUId, transfer.Amount, transfer.Currency, transfer.ExecutionDate.Date);

        return new CreateTransferResponse(ResultStatus.Success, transfer.UId, transfer.Status.ToString());
    }
}
