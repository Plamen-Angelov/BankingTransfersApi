using BankingTransfers.Domain.Entities;

namespace BankingTransfers.Application.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUIdAsync(Guid uid, CancellationToken cancellationToken);
    Task<UserProfileAccountPermissions?> GetAccountPermissionAsync(Guid userUId, string iban, CancellationToken cancellationToken);
}
