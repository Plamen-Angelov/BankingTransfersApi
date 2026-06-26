using BankingTransfers.Application.Transfers.Create;

namespace BankingTransfers.Tests.Validators;

public class CreateTransferValidatorTests
{
    private readonly CreateTransferValidator _validator = new();

    private static CreateTransferRequest ValidRequest() => new()
    {
        UserProfileUId = Guid.NewGuid(),
        IdempotencyKey = Guid.NewGuid().ToString(),
        SourceIban = "DE89370400440532013000",
        TargetIban = "DE89370400440532013001",
        Amount = 100m,
        Currency = "EUR",
        Reason = "Test payment",
        ExecutionDate = DateTime.UtcNow.Date
    };

    [Fact]
    public async Task ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(ValidRequest());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyIdempotencyKey_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { IdempotencyKey = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Idempotency key is required.");
    }

    [Fact]
    public async Task EmptySourceIban_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { SourceIban = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Source IBAN is required.");
    }

    [Fact]
    public async Task SameSourceAndTargetIban_Fails()
    {
        var iban = "DE89370400440532013000";
        var result = await _validator.ValidateAsync(ValidRequest() with { SourceIban = iban, TargetIban = iban });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Source and target IBANs must be different.");
    }

    [Fact]
    public async Task AmountExceedsLimit_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Amount = 50001m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Amount must not exceed 50,000.");
    }

    [Fact]
    public async Task UnsupportedCurrency_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Currency = "GBP" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Currency must be one of: EUR, USD, CHF.");
    }

    [Fact]
    public async Task ExecutionDateInPast_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { ExecutionDate = DateTime.UtcNow.Date.AddDays(-1) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Execution date cannot be in the past.");
    }
}
