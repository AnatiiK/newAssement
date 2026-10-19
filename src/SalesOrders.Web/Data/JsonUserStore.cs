using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using SalesOrders.Web.Models;

namespace SalesOrders.Web.Data;

public interface IUserStore
{
    Task<AppUser?> ValidateCredentialsAsync(string username, string password);
    Task<IReadOnlyList<AppUser>> GetAllAsync();

    /// <summary>Creates a user. Throws <see cref="InvalidOperationException"/> if the username is taken.</summary>
    Task CreateAsync(string username, string password, string role);
}

/// <summary>
/// Users are kept in a JSON file with PBKDF2 password hashes (ASP.NET Core Identity's
/// <see cref="PasswordHasher{TUser}"/>). Seeds an "admin" and a "guest" account on first run.
/// </summary>
public class JsonUserStore : IUserStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly PasswordHasher<AppUser> _hasher = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _path;

    public JsonUserStore(string path, IEnumerable<(string Username, string Password, string Role)> seedUsers)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        if (!File.Exists(_path))
        {
            Write(seedUsers.Select(u => NewUser(u.Username, u.Password, u.Role)).ToList());
        }
    }

    public async Task<AppUser?> ValidateCredentialsAsync(string username, string password)
    {
        var user = (await GetAllAsync())
            .FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            return null;
        }
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<IReadOnlyList<AppUser>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return Read();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task CreateAsync(string username, string password, string role)
    {
        if (!Roles.All.Contains(role))
        {
            throw new ArgumentException($"Unknown role '{role}'.", nameof(role));
        }

        await _lock.WaitAsync();
        try
        {
            var users = Read();
            if (users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"User '{username}' already exists.");
            }
            users.Add(NewUser(username, password, role));
            Write(users);
        }
        finally
        {
            _lock.Release();
        }
    }

    private AppUser NewUser(string username, string password, string role)
    {
        var user = new AppUser { Username = username, Role = role };
        user.PasswordHash = _hasher.HashPassword(user, password);
        return user;
    }

    private List<AppUser> Read() =>
        JsonSerializer.Deserialize<List<AppUser>>(File.ReadAllText(_path)) ?? new List<AppUser>();

    private void Write(List<AppUser> users)
    {
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(users, JsonOptions));
        File.Move(temp, _path, overwrite: true);
    }
}
