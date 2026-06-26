using BankingTransfers.Application.Interfaces;
using BankingTransfers.Application.Transfers.Validate;
using FluentValidation;
using FluentValidation.Results;

namespace BankingTransfers.Tests.Handlers;

public class ValidateTransferHandlerTests
{
    private readonly Mock<IUserProfileRepository> _userProfileRepoMock = new();
    private readonly Mock<IValidator<ValidateTransferRequest>> _validatorMock = new();
    private readonly ValidateTransferHandler _handler;

    public ValidateTransferHandlerTests()
    {
        _handler = new ValidateTransferHandler(_userProfileRepoMock.Object, _validatorMock.Object);
    }

    private static ValidateTransferRequest BuildRequest(Guid? userProfileUId = null) => new()
    {
        UserProfileUId = userProfileUId ?? Guid.NewGuid(),
        SourceIban = "DE89370400440532013000",
        TargetIban = "DE89370400440532013001",
        Amount = 100m,
        Currency = "EUR",
        Reason = "Test",
        ExecutionDate = DateTime.UtcNow.Date
    };

    private void SetupValidValidation()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidateTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    [Fact]
    public async Task ValidationFails_ReturnsValidationFailure_WithErrors()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidateTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Amount", "Amount must be greater than 0.")]));

        var result = await _handler.Handle(BuildRequest(), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.ValidationFailure);
        result.Errors.Should().ContainSingle(e => e == "Amount must be greater than 0.");
    }

    [Fact]
    public async Task ValidationFails_DoesNotCallRepository()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidateTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("SourceIban", "Source IBAN is required.")]));

        await _handler.Handle(BuildRequest(), CancellationToken.None);

        _userProfileRepoMock.Verify(r => r.GetByUIdWithPermissionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UserProfileNotFound_ReturnsNotFound()
    {
        SetupValidValidation();
        _userProfileRepoMock
            .Setup(r => r.GetByUIdWithPermissionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var result = await _handler.Handle(BuildRequest(), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().ContainSingle(e => e == "User profile not found.");
    }

    [Fact]
    public async Task SourceIbanNotAssociated_ReturnsValidationFailure()
    {
        var request = BuildRequest();
        SetupValidValidation();
        _userProfileRepoMock
            .Setup(r => r.GetByUIdWithPermissionAsync(request.UserProfileUId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Id = 1, UId = request.UserProfileUId, Username = "test" });

        var result = await _handler.Handle(request, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.ValidationFailure);
        result.Errors.Should().ContainSingle(e => e.Contains("not associated"));
    }

    [Fact]
    public async Task CreateTransferPermissionDenied_ReturnsValidationFailure()
    {
        var request = BuildRequest();
        SetupValidValidation();
        _userProfileRepoMock
            .Setup(r => r.GetByUIdWithPermissionAsync(request.UserProfileUId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = 1, UId = request.UserProfileUId, Username = "test",
                AccountPermissions = [new() { IBAN = request.SourceIban, CreateTransferPermission = false }]
            });

        var result = await _handler.Handle(request, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.ValidationFailure);
    }

    [Fact]
    public async Task ValidRequestWithPermission_ReturnsSuccess()
    {
        var request = BuildRequest();
        SetupValidValidation();
        _userProfileRepoMock
            .Setup(r => r.GetByUIdWithPermissionAsync(request.UserProfileUId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = 1, UId = request.UserProfileUId, Username = "test",
                AccountPermissions = [new() { IBAN = request.SourceIban, CreateTransferPermission = true }]
            });

        var result = await _handler.Handle(request, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Success);
        result.Errors.Should().BeNull();
    }
}
