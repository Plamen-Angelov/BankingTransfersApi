using BankingTransfers.Application.Transfers.Validate;

namespace BankingTransfers.Tests.Validators;

public class ValidateTransferValidatorTests
{
    private readonly ValidateTransferValidator _validator = new();

    private static ValidateTransferRequest ValidRequest() => new()
    {
        UserProfileUId = Guid.NewGuid(),
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
    public async Task EmptySourceIban_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { SourceIban = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Source IBAN is required.");
    }

    [Fact]
    public async Task EmptyTargetIban_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { TargetIban = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Target IBAN is required.");
    }

    [Fact]
    public async Task SameSourceAndTargetIban_Fails()
    {
        var iban = "DE89370400440532013000";
        var result = await _validator.ValidateAsync(ValidRequest() with { SourceIban = iban, TargetIban = iban });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Source and target IBANs must be different.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AmountNotGreaterThanZero_Fails(decimal amount)
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Amount = amount });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Amount must be greater than 0.");
    }

    [Fact]
    public async Task AmountExceedsLimit_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Amount = 50001m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Amount must not exceed 50,000.");
    }

    [Fact]
    public async Task AmountAtLimit_Passes()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Amount = 50000m });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyCurrency_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Currency = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Currency is required.");
    }

    [Fact]
    public async Task UnsupportedCurrency_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Currency = "GBP" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Currency must be one of: EUR, USD, CHF.");
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("CHF")]
    public async Task SupportedCurrency_Passes(string currency)
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Currency = currency });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyReason_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { Reason = string.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Reason is required.");
    }

    [Fact]
    public async Task ExecutionDateInPast_Fails()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { ExecutionDate = DateTime.UtcNow.Date.AddDays(-1) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Execution date cannot be in the past.");
    }

    [Fact]
    public async Task ExecutionDateInFuture_Passes()
    {
        var result = await _validator.ValidateAsync(ValidRequest() with { ExecutionDate = DateTime.UtcNow.Date.AddDays(1) });
        result.IsValid.Should().BeTrue();
    }
}
