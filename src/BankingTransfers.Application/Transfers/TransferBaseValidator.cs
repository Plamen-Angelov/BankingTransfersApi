using FluentValidation;

namespace BankingTransfers.Application.Transfers;

public abstract class TransferBaseValidator<T> : AbstractValidator<T> where T : ITransferFields
{
    private static readonly string[] AllowedCurrencies = ["EUR", "USD", "CHF"];

    protected TransferBaseValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.SourceIban)
            .NotEmpty()
            .WithMessage("Source IBAN is required.");

        RuleFor(x => x.TargetIban)
            .NotEmpty()
            .WithMessage("Target IBAN is required.");

        RuleFor(x => x)
            .Must(x => x.SourceIban != x.TargetIban)
            .WithMessage("Source and target IBANs must be different.")
            .When(x => !string.IsNullOrEmpty(x.SourceIban) && !string.IsNullOrEmpty(x.TargetIban));

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.")
            .LessThanOrEqualTo(50000)
            .WithMessage("Amount must not exceed 50,000.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Must(c => AllowedCurrencies.Contains(c))
            .WithMessage("Currency must be one of: EUR, USD, CHF.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required.");

        RuleFor(x => x.ExecutionDate)
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date)
            .WithMessage("Execution date cannot be in the past.");
    }
}
