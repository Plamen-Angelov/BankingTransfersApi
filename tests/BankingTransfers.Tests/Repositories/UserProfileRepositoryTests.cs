using BankingTransfers.Infrastructure.Data;
using BankingTransfers.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BankingTransfers.Tests.Repositories;

public class UserProfileRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserProfileRepository _repository;

    public UserProfileRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new UserProfileRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetByUIdAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        var result = await _repository.GetByUIdAsync(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUIdAsync_ReturnsUser_WhenUserExists()
    {
        var uid = Guid.NewGuid();
        _context.UserProfiles.Add(new UserProfile { UId = uid, Username = "alice" });
        await _context.SaveChangesAsync();

        var result = await _repository.GetByUIdAsync(uid, CancellationToken.None);

        result.Should().NotBeNull();
        result!.UId.Should().Be(uid);
        result.Username.Should().Be("alice");
    }

    [Fact]
    public async Task GetByUIdWithPermissionAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        var result = await _repository.GetByUIdWithPermissionAsync(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUIdWithPermissionAsync_ReturnsUser_WithPermissionsLoaded()
    {
        var uid = Guid.NewGuid();
        var user = new UserProfile { UId = uid, Username = "bob" };
        _context.UserProfiles.Add(user);
        await _context.SaveChangesAsync();

        _context.UserProfileAccountPermissions.AddRange(
            new UserProfileAccountPermissions { UserProfileId = user.Id, IBAN = "DE89370400440532013000", CreateTransferPermission = true },
            new UserProfileAccountPermissions { UserProfileId = user.Id, IBAN = "DE89370400440532013001", CreateTransferPermission = false }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetByUIdWithPermissionAsync(uid, CancellationToken.None);

        result.Should().NotBeNull();
        result!.AccountPermissions.Should().HaveCount(2);
        result.AccountPermissions.Should().Contain(p => p.IBAN == "DE89370400440532013000" && p.CreateTransferPermission);
        result.AccountPermissions.Should().Contain(p => p.IBAN == "DE89370400440532013001" && !p.CreateTransferPermission);
    }

    [Fact]
    public async Task GetByUIdWithPermissionAsync_ReturnsCorrectUser_WhenMultipleUsersExist()
    {
        var uid1 = Guid.NewGuid();
        var uid2 = Guid.NewGuid();
        _context.UserProfiles.AddRange(
            new UserProfile { UId = uid1, Username = "alice" },
            new UserProfile { UId = uid2, Username = "bob" }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetByUIdWithPermissionAsync(uid2, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Username.Should().Be("bob");
    }
}
