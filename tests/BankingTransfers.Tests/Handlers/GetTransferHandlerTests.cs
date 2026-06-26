using BankingTransfers.Application.Interfaces;
using BankingTransfers.Application.Transfers.Get;

namespace BankingTransfers.Tests.Handlers;

public class GetTransferHandlerTests
{
    private readonly Mock<IUserProfileRepository> _userProfileRepoMock = new();
    private readonly Mock<ITransferRepository> _transferRepoMock = new();
    private readonly GetTransferHandler _handler;

    public GetTransferHandlerTests()
    {
        _handler = new GetTransferHandler(_userProfileRepoMock.Object, _transferRepoMock.Object);
    }

    private static GetTransferRequest BuildRequest(Guid? userProfileUId = null, Guid? transferUId = null) => new()
    {
        UserProfileUId = userProfileUId ?? Guid.NewGuid(),
        TransferUId = transferUId ?? Guid.NewGuid()
    };

    [Fact]
    public async Task UserProfileNotFound_ReturnsNotFound()
    {
        _userProfileRepoMock
            .Setup(r => r.GetByUIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var result = await _handler.Handle(BuildRequest(), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().ContainSingle(e => e == "User profile not found.");
    }

    [Fact]
    public async Task UserProfileNotFound_DoesNotCallTransferRepository()
    {
        _userProfileRepoMock
            .Setup(r => r.GetByUIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        await _handler.Handle(BuildRequest(), CancellationToken.None);

        _transferRepoMock.Verify(r => r.GetByUIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransferNotFound_ReturnsNotFound()
    {
        var userProfileUId = Guid.NewGuid();
        _userProfileRepoMock
            .Setup(r => r.GetByUIdAsync(userProfileUId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Id = 1, UId = userProfileUId });
        _transferRepoMock
            .Setup(r => r.GetByUIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransferRequest?)null);

        var result = await _handler.Handle(BuildRequest(userProfileUId), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().ContainSingle(e => e == "Transfer not found.");
    }

    [Fact]
    public async Task TransferBelongsToAnotherUser_ReturnsNotFound()
    {
        var userProfileUId = Guid.NewGuid();
        var transferUId = Guid.NewGuid();
        _userProfileRepoMock
            .Setup(r => r.GetByUIdAsync(userProfileUId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Id = 1, UId = userProfileUId });
        _transferRepoMock
            .Setup(r => r.GetByUIdAsync(transferUId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransferRequest { UId = transferUId, UserProfileId = 99 }); // belongs to another user

        var result = await _handler.Handle(BuildRequest(userProfileUId, transferUId), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().ContainSingle(e => e == "Transfer not found.");
    }

    [Fact]
    public async Task ValidRequest_ReturnsSuccessWithAllFields()
    {
        var userProfileUId = Guid.NewGuid();
        var transferUId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddHours(-1);
        var executionDate = DateTime.UtcNow.Date;

        _userProfileRepoMock
            .Setup(r => r.GetByUIdAsync(userProfileUId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Id = 5, UId = userProfileUId });

        var transfer = new TransferRequest
        {
            UId = transferUId,
            UserProfileId = 5,
            SourceIban = "DE89370400440532013000",
            TargetIban = "DE89370400440532013001",
            Amount = 750m,
            Currency = "USD",
            Reason = "Invoice payment",
            ExecutionDate = executionDate,
            Status = TransferStatus.Processed,
            CreatedAt = createdAt,
            ProcessedAt = DateTime.UtcNow,
            RetryCount = 0
        };
        _transferRepoMock
            .Setup(r => r.GetByUIdAsync(transferUId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        var result = await _handler.Handle(BuildRequest(userProfileUId, transferUId), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Success);
        result.UId.Should().Be(transferUId);
        result.SourceIban.Should().Be("DE89370400440532013000");
        result.TargetIban.Should().Be("DE89370400440532013001");
        result.Amount.Should().Be(750m);
        result.Currency.Should().Be("USD");
        result.Reason.Should().Be("Invoice payment");
        result.ExecutionDate.Should().Be(executionDate);
        result.TransferStatus.Should().Be("Processed");
        result.CreatedAt.Should().Be(createdAt);
        result.RetryCount.Should().Be(0);
    }
}
