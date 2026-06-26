using BankingTransfers.Domain.Entities;

namespace BankingTransfers.Application.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUIdAsync(Guid uid, CancellationToken cancellationToken);
    Task<UserProfile?> GetByUIdWithPermissionAsync(Guid uid, CancellationToken cancellationToken);
}
