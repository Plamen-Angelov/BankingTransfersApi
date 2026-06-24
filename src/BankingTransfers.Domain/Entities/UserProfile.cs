namespace BankingTransfers.Domain.Entities;

public class UserProfile
{
    public int Id { get; set; }
    public Guid UId { get; set; }
    public string Username { get; set; } = string.Empty;

    public ICollection<UserProfileAccountPermissions> AccountPermissions { get; set; } = [];
    public ICollection<TransferRequest> TransferRequests { get; set; } = [];
}
