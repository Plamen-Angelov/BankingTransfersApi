namespace BankingTransfers.Domain.Entities;

public class UserProfileAccountPermissions
{
    public Guid Id { get; set; }
    public int UserProfileId { get; set; }
    public string IBAN { get; set; } = string.Empty;
    public bool CreateTransferPermission { get; set; }

    public UserProfile UserProfile { get; set; } = null!;
}
