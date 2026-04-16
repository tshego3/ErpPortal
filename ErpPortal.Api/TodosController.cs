using ErpPortal.Api.Core.Contracts;
using ErpPortal.Api.Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ErpPortal.Api.Controllers;

/// <summary>
/// Proxies requests to DummyJSON's /auth/todos (protected endpoint).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class TodosController : ControllerBase
{
    private readonly IDummyJsonClient _client;

    public TodosController(IDummyJsonClient client) => _client = client;

    [HttpGet]
    public async Task<ActionResult<TodosResponse>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        CancellationToken ct = default)
    {
        TodosResponse result = await _client.GetTodosAsync(skip, limit, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Todo>> GetById(
        int id, CancellationToken ct = default)
    {
        Todo todo = await _client.GetTodoByIdAsync(id, ct);
        return Ok(todo);
    }
}
