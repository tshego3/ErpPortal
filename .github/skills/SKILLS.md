
# Agent Skills (.NET 10 Blazor SSR)

This document defines what an AI coding agent is expected to do well in this repository.

## Architecture Overview

This project uses **Clean Architecture** with strict layer boundaries. Each layer may only reference layers **below** it:

```
Domain (entities, pure logic) — NO dependencies
    ↑
Application (CQRS, DTOs, validation) — references Domain
    ↑
Infrastructure (EF, services) — references Domain + Application
    ↑
API (controllers, auth) — references all above; creates MediatR requests
    ↑
Presentation (Blazor, pages) — HTTP calls to API **ONLY** — never references Application/Domain
    ↑
Tests (xUnit, InMemory DB) — mirrors Application structure
```

**Golden rule:** Never bypass a layer. Business logic stays in Domain/Application; UI never contains logic.

**Domain type rule:** Never create entity, DTO, or record types outside `Domain`. All shared types (entities, classes, value objects) must be defined in Domain and referenced directly from any layer that needs them. Do not create local copies in WebUI, WebAPI, or any other layer.

For a complete walkthrough of adding a new entity, see the feature guide documentation.

## Core Delivery Skills

1. Implement full vertical features across Presentation, Application, Domain, Infrastructure, and tests.
2. Work with Blazor Web App SSR + Interactive Server components without breaking auth, antiforgery, or routing.
3. Use dependency injection consistently through Program.cs registrations and constructor injection.
4. Build reliable HTTP integrations through typed clients, delegating handlers, and centralized error handling.
5. Keep architecture clean by preserving boundaries between domain logic, infrastructure, and UI.
6. Apply the project design system consistently with MudBlazor and brand configuration from appsettings.
7. Perform a mandatory compliance pass before completion: confirm changes align with `.github/copilot-instructions.md`, relevant `.github/skills/` guidance, and applicable standards in `docs/`.

## Blazor SSR Interaction Skills

1. Distinguish SSR-only pages from interactive pages and choose the right rendering mode.
2. Use controller POST endpoints for cookie sign-in/sign-out where headers must be written.
3. Use antiforgery correctly for all state-changing form posts.
4. Handle route authorization with Authorize attributes and route-level redirect patterns.
5. Avoid introducing interactive behavior where server-rendered behavior is required.

## Authentication and Security Skills

1. Implement cookie-based authentication flows with explicit login and logout endpoints.
2. Preserve secure defaults: HttpOnly cookies, appropriate SameSite policy, HSTS in production, and secure headers.
3. Keep privacy, crawler shield, and compliance behavior intact when changing middleware.
4. Ensure protected pages are inaccessible to anonymous users and redirect safely to login.
5. Maintain privacy-consent workflows (for example login -> privacy -> dashboard) without bypass paths.

## Data and Domain Skills

1. Add and evolve domain entities without leaking infrastructure concerns into domain classes.
2. Extend repository implementations while preserving repository contracts.
3. Keep DTO mapping and API access isolated in infrastructure and application layers.
4. Use output caching intentionally and invalidate by tag when needed.

## UI and Design System Skills

1. Implement layouts that feel premium and enterprise-grade, not generic dashboard boilerplate.
2. Follow the no-line sectioning rule: prefer tonal surfaces over 1px border-heavy composition.
3. Keep typography aligned with the project font hierarchy and high-density data readability.
4. Read brand colors from configuration; avoid hard-coded color drift from the design system.
5. Do not use scoped `.razor.css` files for component styling; prefer MudBlazor component API and global baseline styles only.
6. Do not hardcode colors in `.razor` or `.cs`; use `BrandingConfig` and MudBlazor theme variables (`var(--mud-palette-*)`).
7. Consult MudBlazor docs first (https://mudblazor.com/docs/overview) and prefer native props/variants/density/theming before custom style logic.
8. For new components, separate view and logic using `.razor` + `.razor.cs` code-behind.
9. Keep control flow flat: avoid deeply nested syntax and favor guard clauses with early returns.

## Quality and Maintenance Skills

1. Build after meaningful changes and resolve compiler/analyzer errors before finishing.
2. Add or update tests when behavior changes (especially auth flow, controllers, and repository behavior).
3. Keep edits minimal, scoped, and style-consistent with nearby code.
4. Avoid unrelated refactors while implementing requested changes.
5. Treat compile-time warnings as errors and resolve introduced warnings before completion.
6. Prefer explicit typing and return types in implementation code; avoid implicit typing unless clarity is preserved.
7. Prefer `async Task`/`async Task<T>` over `async void`; allow `async void` only for framework-required event handlers.
8. Use generics for reusable, type-safe abstractions; avoid duplicated type-specific implementations when a constrained generic design is appropriate.
9. Keep implementations simple and straightforward; avoid complicated designs when a direct approach is sufficient.
10. Use direct, descriptive, and consistent naming conventions throughout the codebase.
11. Apply DRY by preferring existing related functionality and extending it before introducing duplicate paths.
12. Ensure failures surface a descriptive, user-safe reason and a clear next step (for example retry, refresh, or contact support) instead of generic error text.
13. If code is hard to understand without comments, simplify the implementation first; use comments for context, not to explain avoidable complexity.
14. Prefer low-boilerplate implementations: small methods, small diffs, and direct control flow that delivers high impact with minimal code.
15. Keep logging concise and high-signal: log intent, result, and failures with structured properties, but avoid verbose multi-line logging blocks when one clear statement is enough.
16. Reuse compact helper methods/constants for repeated error text or repeated computation to reduce noise and keep feature methods easy to scan.

## Typical Skill Applications

1. Implement login + privacy consent + conditional logout flow end-to-end.
2. Add dashboard widgets and list pages using existing repository/services patterns.
3. Introduce new domain entities and wire them through repository, DI, and UI layers.
4. Apply white-label design updates from appsettings and design token guidance.

## Reliability, Testability, and Data Modeling Skills

1. Follow NASA JPL Power-of-10 style constraints: simple control flow, deterministic loops, bounded function size, and explicit return-value handling.
2. Apply 2 AM production rules: context-rich exceptions, defensive structured logging, guard clauses, idempotency, and mandatory timeouts.
3. Propagate `CancellationToken` through async paths and avoid empty `catch {}` blocks.
4. Prefer constructor injection only; do not use service locator (`IServiceProvider`) in application code.
5. Favor composition over inheritance and seal classes by default unless explicit extensibility is required.
6. Use records for DTOs/events/value objects and classes for DI services and I/O logic.
7. Keep records immutable; prefer `with` expressions for changes and leverage value equality in tests.
8. Use test builders/AutoFixture patterns for resilient tests and stable constructor evolution.
9. For immutable Domain records used by forms, define default/empty construction on the record itself (for example `CreateEmpty()`) and consume that factory from UI pages instead of local page-level model builders.

## Practical Development Patterns

### Domain Entity Pattern

```csharp
// Domain/Entities/Product.cs — no dependencies, pure logic
public class Product : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Domain guard clauses (pure logic, testable without EF)
    public bool IsAvailable() => IsActive && Price > 0;
}
```

### CQRS Command Pattern

```csharp
// Application/Features/Products/Commands/CreateProduct/CreateProductCommand.cs
// Command + Handler + Validator all in ONE file

public sealed class CreateProductCommand : IRequest<Result<Guid>>
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
}

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(c => c.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(c => c.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");
    }
}

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IApplicationDbContext context,
        ILogger<CreateProductCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(
        CreateProductCommand request, CancellationToken cancellationToken)
    {
        Product product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System" // Should come from ICurrentUserService in real code
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product {ProductId} created.", product.Id);
        return Result<Guid>.Success(product.Id);
    }
}
```

### API Controller Pattern

```csharp
// Controllers/ProductsController.cs — MUST have [Authorize]
[Authorize]  // REQUIRED — all data controllers must be authorized
[Route("api/[controller]")]
[ApiController]
public class ProductsController : BaseApiController
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create(CreateProductCommand command)
    {
        Result<Guid> result = await Mediator.Send(command);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data)
            : BadRequest(result.Errors);
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, VaryByQueryKeys = ["*"])]
    public async Task<ActionResult<PaginatedResult<ProductDto>>> GetAll(
        [FromQuery] GetAllProductsQuery query)
    {
        PaginatedResult<ProductDto> result = await Mediator.Send(query);
        return Ok(result);
    }
}
```

### Typed API Client Pattern

```csharp
// Presentation/Services/ProductApiClient.cs
public class ProductApiClient : BaseApiClient
{
    public ProductApiClient(HttpClient httpClient, ILogger<ProductApiClient> logger)
        : base(httpClient, logger) { }

    public Task<PaginatedList<ProductBriefDto>?> GetAllAsync(int pageNumber = 1, int pageSize = 10) =>
        GetAsync<PaginatedList<ProductBriefDto>>(
            $"api/products{BuildQueryString(new() { 
                ["pageNumber"] = pageNumber.ToString(), 
                ["pageSize"] = pageSize.ToString() 
            })}");

    public Task<ProductDetailDto?> GetByIdAsync(Guid id) =>
        GetAsync<ProductDetailDto>($"api/products/{id}");

    public Task<bool> CreateAsync(CreateProductRequest model) =>
        PostAsync("api/products", model);

    public Task<bool> UpdateAsync(UpdateProductRequest model) =>
        PutAsync($"api/products/{model.Id}", model);

    public Task<bool> DeleteAsync(Guid id) =>
        DeleteAsync($"api/products/{id}");
}
```

### Blazor SSR Page Pattern

```razor
@* Presentation/Components/Pages/Products.razor — view only, SSR by default *@
@page "/products"
@attribute [Authorize]
@inject ProductApiClient ApiClient
@inject NavigationManager Nav

<PageTitle>Products</PageTitle>

<MudText Typo="Typo.h5" Class="mb-4">Products</MudText>

@if (_products == null)
{
    <MudProgressLinear Indeterminate="true" />
}
else if (_products.Count == 0)
{
    <MudAlert Severity="Severity.Info">No products found.</MudAlert>
}
else
{
    <MudTable Items="@_products" Hover="true" Breakpoint="Breakpoint.Sm">
        <HeaderContent>
            <MudTh>Name</MudTh>
            <MudTh>Description</MudTh>
            <MudTh>Price</MudTh>
            <MudTh>Actions</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Name">@context.Name</MudTd>
            <MudTd DataLabel="Description">@context.Description</MudTd>
            <MudTd DataLabel="Price">@context.Price.ToString("C")</MudTd>
            <MudTd DataLabel="Actions">
                <MudButton Variant="Variant.Text" Color="Color.Primary" Size="Size.Small"
                    href="@Nav.GetUriByPage("/ProductDetails", new { id = context.Id })">
                    View
                </MudButton>
            </MudTd>
        </RowTemplate>
    </MudTable>
}

<MudButton Variant="Variant.Filled" Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add"
    href="/products/create" Class="mt-4">
    New Product
</MudButton>
```

```csharp
// Presentation/Components/Pages/Products.razor.cs — code-behind
using Microsoft.AspNetCore.Authorization;

public partial class Products
{
    private List<ProductBriefDto>? _products;

    protected override async Task OnInitializedAsync()
    {
        PaginatedList<ProductBriefDto>? result = await ApiClient.GetAllAsync();
        _products = result?.Items ?? new List<ProductBriefDto>();
    }
}
```

### Testing Pattern

**Command Handler Test (xUnit + FluentAssertions + InMemory DB):**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class CreateProductCommandHandlerTests
{
    private static readonly ILogger<CreateProductCommandHandler> Logger =
        NullLoggerFactory.Instance.CreateLogger<CreateProductCommandHandler>();

    private static MockApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MockApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())  // ← isolation per test
            .Options);

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessWithNewGuid()
    {
        // Arrange
        await using MockApplicationDbContext context = CreateContext();
        CreateProductCommandHandler handler = new CreateProductCommandHandler(context, Logger);
        CreateProductCommand command = new CreateProductCommand { Name = "Widget", Price = 9.99m };

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsToDatabase()
    {
        // Arrange
        await using MockApplicationDbContext context = CreateContext();
        CreateProductCommandHandler handler = new CreateProductCommandHandler(context, Logger);
        CreateProductCommand command = new CreateProductCommand { Name = "Widget", Price = 9.99m };

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Product? saved = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == result.Data);

        saved.Should().NotBeNull();
        saved!.Name.Should().Be(command.Name);
    }

    [Fact]
    public async Task Handle_WithEmptyName_ReturnsValidationFailure()
    {
        // Arrange
        CreateProductCommandValidator validator = new CreateProductCommandValidator();
        CreateProductCommand command = new CreateProductCommand { Name = "", Price = 9.99m };

        // Act
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Name));
    }
}
```

**Testing Rules:**
- Use `UseInMemoryDatabase(Guid.NewGuid().ToString())` per test for full isolation
- Never mock `DbSet<T>` — use real in-memory context (Moq cannot intercept EF Core async methods)
- Use `NullLoggerFactory` for `ILogger` — reduces noise and log is not a contract to assert
- Name tests `[Method]_[Scenario]_[ExpectedResult]` for readability
- Assert one behaviour per test — easier diagnosis on failure
- Use `await using MockApplicationDbContext context = CreateContext()` to ensure cleanup

## Security & Compliance in Development

When implementing features, always check security practices for:
- **Critical (CR-01 to CR-05):** JWT secrets, Auth attributes, CORS, health checks, file uploads
- **High (HR-01 to HR-07):** Log injection, HttpClient pooling, OTP rate limiting, CSP headers
- **Medium (MR-01 to MR-09):** Tenant isolation, ownership checks, caching strategy

**Key reminders when coding:**
1. All data controllers **must** have `[Authorize]` attribute
2. Tenant ID must be enforced in every CQRS query handler via `ICurrentUserService.TenantId`
3. File uploads must validate magic bytes, not just `Content-Type` header
4. Use `IHttpClientFactory` in services — never `new HttpClient()`
5. JWT secrets must NOT be in `appsettings.json` — use User Secrets (dev) / Key Vault (prod)

## Reference Documentation

| Document | Purpose |
|----------|---------|
| **copilot-instructions.md** | Core engineering rules, architecture layers, feature checklist |
| **skills/SKILLS.md** (this file) | Agent capabilities, practical code patterns, testing strategies |

For fast onboarding, follow this path:
1. Read **copilot-instructions.md** for architecture + feature checklist
2. Reference security practices when touching auth, data, or file handling
3. Use the patterns in this document as copy-paste templates
