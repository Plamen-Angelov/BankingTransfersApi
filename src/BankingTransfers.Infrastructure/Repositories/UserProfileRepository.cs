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

    public async Task<UserProfile?> GetByUIdWithPermissionAsync(Guid uid, CancellationToken cancellationToken)
    {
        return await _context.UserProfiles
            .Include(u => u.AccountPermissions)
            .FirstOrDefaultAsync(u => u.UId == uid, cancellationToken);
    }
}
