using BankingTransfers.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingTransfers.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.UserProfiles.AnyAsync())
            return;

        var john = new UserProfile
        {
            UId = Guid.Parse("8d5fa53a-fe3b-4f74-b5c4-7b43dc4e2187"),
            Username = "john.doe",
            AccountPermissions =
            [
                new UserProfileAccountPermissions
                {
                    Id = Guid.Parse("01814d3d-d15d-44a8-a128-7d3435faaf06"),
                    IBAN = "BG12TEST1234567890",
                    CreateTransferPermission = true
                },
                new UserProfileAccountPermissions
                {
                    Id = Guid.Parse("c7eaadfe-867c-4a10-b12f-20a95fdfbb66"),
                    IBAN = "BG99TEST0000000001",
                    CreateTransferPermission = false
                }
            ]
        };

        var jane = new UserProfile
        {
            UId = Guid.Parse("78f90a74-c792-45ca-a6b1-4dac75f4604d"),
            Username = "jane.smith",
            AccountPermissions =
            [
                new UserProfileAccountPermissions
                {
                    Id = Guid.Parse("345fc413-2e60-4d83-9a75-b8d11c3b9b3d"),
                    IBAN = "BG34TEST9876543210",
                    CreateTransferPermission = true
                },
                new UserProfileAccountPermissions
                {
                    Id = Guid.Parse("f6b47560-5a58-4ea7-bc0b-6f653f003848"),
                    IBAN = "BG99TEST0000000002",
                    CreateTransferPermission = false
                }
            ]
        };

        context.UserProfiles.AddRange(john, jane);
        await context.SaveChangesAsync();
    }
}
