namespace BankingTransfers.Application.Transfers;

public interface ITransferFields
{
    string SourceIban { get; }
    string TargetIban { get; }
    decimal Amount { get; }
    string Currency { get; }
    string Reason { get; }
    DateTime ExecutionDate { get; }
}
