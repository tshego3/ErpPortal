using ErpPortal.Core.Contracts;
using ErpPortal.Core.Domain;
using ErpPortal.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace ErpPortal.Infrastructure.Repositories;

// Internal DTO for deserializing the paginated API response
internal sealed record UsersApiResponse(List<User> Users, int Total);

public sealed class UserRepository : IRepository<User>
{
    private readonly IErpHttpClient _http;
    private readonly ILogger<UserRepository> _logger;
    private readonly INotificationService _notifier;

    public UserRepository(IErpHttpClient http, ILogger<UserRepository> logger, INotificationService notifier)
    {
        _http = http;
        _logger = logger;
        _notifier = notifier;
    }

    public async Task<(IReadOnlyList<User> Data, int Total)> GetAllAsync(
        int skip = 0, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            // Now calling the API Gateway's /products proxy instead of direct /users
            UsersApiResponse response = await _http.GetAsync<UsersApiResponse>($"/products?limit={limit}&skip={skip}", ct);
            return (response.Users, response.Total);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "CRITICAL: User Fetch — {Message}", e.Message);
            throw;
        }
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try { return await _http.GetAsync<User>($"/products/{id}", ct); }
        catch (Exception e) { _logger.LogError(e, "Failed to fetch user {Id}", id); throw; }
    }

    public async Task<User> CreateAsync(User entity, CancellationToken ct = default)
    {
        try
        {
            User user = await _http.PostAsync<User>("/users/add", entity, ct);
            _notifier.ShowSuccess("User Created", $"{user.FirstName} has been added.");
            return user;
        }
        catch (Exception e) { _logger.LogError(e, "Failed to create user"); throw; }
    }

    public async Task<User> UpdateAsync(int id, User entity, CancellationToken ct = default)
    {
        try
        {
            User user = await _http.PutAsync<User>($"/users/{id}", entity, ct);
            _notifier.ShowSuccess("User Updated", $"Profile for {user.FirstName} saved.");
            return user;
        }
        catch (Exception e) { _logger.LogError(e, "Failed to update user {Id}", id); throw; }
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            await _http.DeleteAsync($"/users/{id}", ct);
            _notifier.ShowSuccess("User Deleted", "The record has been permanently removed.");
        }
        catch (Exception e) { _logger.LogError(e, "Failed to delete user {Id}", id); throw; }
    }
}
