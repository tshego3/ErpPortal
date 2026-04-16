using ErpPortal.Api.Core.Contracts;
using ErpPortal.Api.Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ErpPortal.Api.Controllers;

/// <summary>
/// Proxies requests to DummyJSON's /auth/products (protected endpoint).
/// The DummyJSON JWT is managed transparently by TokenService + DummyJsonAuthHandler.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly IDummyJsonClient _client;

    public ProductsController(IDummyJsonClient client) => _client = client;

    [HttpGet]
    public async Task<ActionResult<ProductsResponse>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        CancellationToken ct = default)
    {
        ProductsResponse result = await _client.GetProductsAsync(skip, limit, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetById(
        int id, CancellationToken ct = default)
    {
        Product product = await _client.GetProductByIdAsync(id, ct);
        return Ok(product);
    }
}
