using BankingTransfers.Application.Common;
using BankingTransfers.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace BankingTransfers.Application.Transfers.Validate;

public class ValidateTransferHandler : IRequestHandler<ValidateTransferRequest, ValidateTransferResponse>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IValidator<ValidateTransferRequest> _validator;

    public ValidateTransferHandler(IUserProfileRepository userProfileRepository, IValidator<ValidateTransferRequest> validator)
    {
        _userProfileRepository = userProfileRepository;
        _validator = validator;
    }

    public async Task<ValidateTransferResponse> Handle(ValidateTransferRequest request, CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByUIdAsync(request.UserProfileUId, cancellationToken);
        if (userProfile == null)
            return new ValidateTransferResponse(ResultStatus.NotFound, ["User profile not found."]);

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return new ValidateTransferResponse(ResultStatus.ValidationFailure, errors);
        }

        var permission = await _userProfileRepository.GetAccountPermissionAsync(
            request.UserProfileUId, request.SourceIban, cancellationToken);

        if (permission == null || !permission.CreateTransferPermission)
            return new ValidateTransferResponse(ResultStatus.ValidationFailure, ["Source IBAN is not associated with your profile or you don't have transfer permission."]);

        return new ValidateTransferResponse(ResultStatus.Success);
    }
}
