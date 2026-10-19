using SalesOrders.Web.Data;
using SalesOrders.Web.Models;

namespace SalesOrders.Tests;

public class UserStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "salesorders-users-" + Guid.NewGuid().ToString("N"));
    private readonly JsonUserStore _store;

    public UserStoreTests()
    {
        _store = new JsonUserStore(Path.Combine(_dir, "users.json"), new[] { ("admin", "Admin@123", Roles.Admin) });
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public async Task Seeded_user_can_log_in_and_wrong_password_is_rejected()
    {
        var user = await _store.ValidateCredentialsAsync("ADMIN", "Admin@123");

        Assert.NotNull(user);
        Assert.Equal(Roles.Admin, user!.Role);
        Assert.Null(await _store.ValidateCredentialsAsync("admin", "wrong"));
    }

    [Fact]
    public async Task CreateAsync_adds_guest_and_stores_hash_not_password()
    {
        await _store.CreateAsync("viewer", "Secret#1", Roles.Guest);

        var user = await _store.ValidateCredentialsAsync("viewer", "Secret#1");
        Assert.Equal(Roles.Guest, user!.Role);
        Assert.DoesNotContain("Secret#1", File.ReadAllText(Path.Combine(_dir, "users.json")));
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_username()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.CreateAsync("Admin", "whatever1", Roles.Guest));
    }
}
