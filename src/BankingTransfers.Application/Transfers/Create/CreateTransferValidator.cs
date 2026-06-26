using FluentValidation;

namespace BankingTransfers.Application.Transfers.Create;

public class CreateTransferValidator : TransferBaseValidator<CreateTransferRequest>
{
    public CreateTransferValidator() : base()
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required.");
    }
}
