using ErpPortal.Core.Contracts;
using ErpPortal.Core.Domain;
using Microsoft.Extensions.Logging;

namespace ErpPortal.Infrastructure.Repositories;

internal sealed record TodosApiResponse(List<Todo> Todos, int Total);

public sealed class TodoRepository : IRepository<Todo>
{
    private readonly IErpHttpClient _http;
    private readonly ILogger<TodoRepository> _logger;
    private readonly INotificationService _notifier;

    public TodoRepository(IErpHttpClient http, ILogger<TodoRepository> logger, INotificationService notifier)
    {
        _http = http; _logger = logger; _notifier = notifier;
    }

    public async Task<(IReadOnlyList<Todo> Data, int Total)> GetAllAsync(
        int skip = 0, int limit = 150, CancellationToken ct = default)
    {
        try
        {
            // Now calling the API Gateway's /todos proxy
            TodosApiResponse response = await _http.GetAsync<TodosApiResponse>($"/todos?limit={limit}&skip={skip}", ct);
            return (response.Todos, response.Total);
        }
        catch (Exception e) { _logger.LogError(e, "Failed to fetch todos"); throw; }
    }

    public async Task<Todo?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _http.GetAsync<Todo>($"/todos/{id}", ct);

    public async Task<Todo> CreateAsync(Todo entity, CancellationToken ct = default)
    {
        Todo todo = await _http.PostAsync<Todo>("/todos/add", entity, ct);
        _notifier.ShowSuccess("Task Created", "New task added.");
        return todo;
    }

    public async Task<Todo> UpdateAsync(int id, Todo entity, CancellationToken ct = default)
    {
        Todo todo = await _http.PutAsync<Todo>($"/todos/{id}", entity, ct);
        _notifier.ShowSuccess("Task Updated", "Changes saved.");
        return todo;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _http.DeleteAsync($"/todos/{id}", ct);
        _notifier.ShowSuccess("Task Deleted", "The task has been removed.");
    }
}
