using BankingTransfers.Application.Interfaces;
using BankingTransfers.Domain.Entities;
using BankingTransfers.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingTransfers.Infrastructure.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _context;

    public UserProfileRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByUIdAsync(Guid uid, CancellationToken cancellationToken)
    {
        return await _context.UserProfiles
            .FirstOrDefaultAsync(x => x.UId == uid, cancellationToken);
    }

    public async Task<UserProfileAccountPermissions?> GetAccountPermissionAsync(Guid userUId, string iban, CancellationToken cancellationToken)
    {
        return await _context.UserProfileAccountPermissions
            .FirstOrDefaultAsync(x => x.UserProfile.UId == userUId && x.IBAN == iban, cancellationToken);
    }
}
