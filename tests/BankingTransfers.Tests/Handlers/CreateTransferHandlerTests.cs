using BankingTransfers.Application.Interfaces;
using BankingTransfers.Application.Transfers.Create;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace BankingTransfers.Tests.Handlers;

public class CreateTransferHandlerTests
{
    private readonly Mock<IUserProfileRepository> _userProfileRepoMock = new();
    private readonly Mock<ITransferRepository> _transferRepoMock = new();
    private readonly Mock<IValidator<CreateTransferRequest>> _validatorMock = new();
    private readonly Mock<ILogger<CreateTransferHandler>> _loggerMock = new();
    private readonly CreateTransferHandler _handler;

    public CreateTransferHandlerTests()
    {
        _handler = new CreateTransferHandler(
            _userProfileRepoMock.Object,
            _transferRepoMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    private static CreateTransferRequest BuildRequest(Guid? userProfileUId = null, string? idempotencyKey = null) => new()
    {
        UserProfileUId = userProfileUId ?? Guid.NewGuid(),
        IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString(),
        SourceIban = "DE89370400440532013000",
        TargetIban = "DE89370400440532013001",
        Amount = 500m,
        Currency = "EUR",
        Reason = "Test payment",
        ExecutionDate = DateTime.UtcNow.Date
    };

    private void SetupValidValidation()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupNoExistingTransfer()
    {
        _transferRepoMock
            .Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransferRequest?)null);
    }

    [Fact]
    public async Task ValidationFails_ReturnsValidationFailure_WithErrors()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("IdempotencyKey", "Idempotency key is required.")]));

        var result = await _handler.Handle(BuildRequest(), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.ValidationFailure);
        result.Errors.Should().ContainSingle(e => e == "Idempotency key is required.");
    }

    [Fact]
    public async Task ValidationFails_DoesNotCallRepositories()
    {
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Amount", "Amount must be greater than 0.")]));

        await _handler.Handle(BuildRequest(), CancellationToken.None);

        _transferRepoMock.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _userProfileRepoMock.Verify(r => r.GetByUIdWithPermissionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DuplicateIdempotencyKey_ReturnsExistingTransfer()
    {
        var existingUId = Guid.NewGuid();
        var existing = new TransferRequest { UId = existingUId, Status = TransferStatus.Processed };
        SetupValidValidation();
        _transferRepoMock
            .Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(BuildRequest(), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Success);
        result.UId.Should().Be(existingUId);
        result.TransferStatus.Should().Be("Processed");
    }

    [Fact]
    public async Task DuplicateIdempotencyKey_DoesNotCallUserProfileRepository()
    {
        SetupValidValidation();
        _transferRepoMock
            .Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransferRequest { UId = Guid.NewGuid(), Status = TransferStatus.Pending });

        await _handler.Handle(BuildRequest(), CancellationToken.None);

        _userProfileRepoMock.Verify(r => r.GetByUIdWithPermissionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UserProfileNotFound_ReturnsNotFound()
    {
        SetupValidValidation();
        SetupNoExistingTransfer();
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
        SetupNoExistingTransfer();
        _userProfileRepoMock
            .Setup(r => r.GetByUIdWithPermissionAsync(request.UserProfileUId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Id = 1, UId = request.UserProfileUId, Username = "test" });

        var result = await _handler.Handle(request, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.ValidationFailure);
        result.Errors.Should().ContainSingle(e => e.Contains("not associated"));
    }

    [Fact]
    public async Task ValidRequest_CreatesTransfer_ReturnsSuccess()
    {
        var request = BuildRequest();
        SetupValidValidation();
        SetupNoExistingTransfer();
        _userProfileRepoMock
            .Setup(r => r.GetByUIdWithPermissionAsync(request.UserProfileUId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = 1, UId = request.UserProfileUId, Username = "test",
                AccountPermissions = [new() { IBAN = request.SourceIban, CreateTransferPermission = true }]
            });
        _transferRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TransferRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(request, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Success);
        result.UId.Should().NotBeNull().And.NotBe(Guid.Empty);
        result.TransferStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task ValidRequest_CallsAddAsync_WithCorrectTransferData()
    {
        var request = BuildRequest();
        SetupValidValidation();
        SetupNoExistingTransfer();
        _userProfileRepoMock
            .Setup(r => r.GetByUIdWithPermissionAsync(request.UserProfileUId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = 7, UId = request.UserProfileUId, Username = "test",
                AccountPermissions = [new() { IBAN = request.SourceIban, CreateTransferPermission = true }]
            });
        _transferRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TransferRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.Handle(request, CancellationToken.None);

        _transferRepoMock.Verify(r => r.AddAsync(
            It.Is<TransferRequest>(t =>
                t.UserProfileId == 7 &&
                t.SourceIban == request.SourceIban &&
                t.TargetIban == request.TargetIban &&
                t.Amount == request.Amount &&
                t.Currency == request.Currency &&
                t.Status == TransferStatus.Pending &&
                t.IdempotencyKey == request.IdempotencyKey),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
