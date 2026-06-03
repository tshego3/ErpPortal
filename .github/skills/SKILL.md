# Agent Skills (.NET 10 Blazor SSR)

This document defines what an AI coding agent is expected to do well in this repository.

> **Rule priority:** Rules in this document are grouped by enforcement level. The "Top 5 Non-Negotiables" are **MUST** rules — violations are blockers. Section-level rules are **SHOULD** — follow unless a documented exception applies. Inline suggestions (e.g., "prefer", "consider") are **MAY** — use judgment.

## Top 5 Non-Negotiables

When processing any feature request, these rules take absolute precedence:

1. **4-branch rendering (Interactive Server only)** — every Interactive Server component that loads data asynchronously must render exactly 4 states in order: Loading → Error → Data → Empty (see Gold Standard section below). SSR-only pages use the simpler 2-branch pattern (HasData / Empty) with try/catch error handling.
2. **`[Authorize]` on all data controllers** — no exceptions; this is a production blocker.
3. **Layer boundaries** — never bypass Clean Architecture layers; UI never references Domain/Application directly.
4. **Domain type ownership** — all entity/DTO/record types live in `Domain`; no duplicates in other layers.
5. **SSR-only by default** — pages use no `@rendermode`, no SignalR, no custom JS unless Interactive Server is justified. New greenfield features may use Interactive Server only when the feature requires real-time UI updates or sub-second feedback that cannot be achieved with form POST round-trips (e.g., real-time data grids with inline editing, collaborative editing, live dashboards). Document the justification in a code comment.

All other rules in this document elaborate on these principles.

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

**Domain type rule:** Never create entity, DTO, or record types outside `Domain`. All shared types (entities, classes, value objects) must be defined in Domain and referenced directly from any layer that needs them. Do not create local copies in Presentation, API, or any other layer.

For a complete walkthrough of adding a new entity, see the feature guide documentation.

## Core Delivery Skills

1. Implement full vertical features across Presentation, Application, Domain, Infrastructure, and tests.
2. Work with Blazor Web App SSR (default) and Interactive Server components (greenfield features only) without breaking auth, antiforgery, or routing. Interactive Server is justified only when the feature requires real-time UI updates or sub-second feedback that cannot be achieved with form POST round-trips (e.g., collaborative editing, live dashboards, inline grid editing).
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
6. **Choose the correct rendering pattern by page type** — see the Gold Standard section (Interactive Server) for full specifications and examples. SSR-only pages use the 2-branch pattern (HasData / Empty) with try/catch error handling in `OnInitializedAsync`.

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
3. Keep typography aligned with Libre Franklin hierarchy and high-density data readability.
4. Read brand colors from configuration; avoid hard-coded color drift from the design system.
5. Do not use scoped `.razor.css` files or `<style>` blocks in `.razor` components for styling; prefer MudBlazor component API and global baseline styles only.
6. Do not hardcode colors in `.razor` or `.cs`; use `BrandingConfig` and MudBlazor theme variables (`var(--mud-palette-*)`).
7. Consult MudBlazor docs first (https://mudblazor.com/docs/overview) and prefer native props/variants/density/theming before custom style logic.
8. For new components, separate view and logic using `.razor` + `.razor.cs` code-behind.
9. Keep control flow flat: avoid deeply nested syntax and favor guard clauses with early returns.
10. **Choose the correct rendering pattern for async data by page type** — Interactive Server uses the 4-branch Gold Standard pattern (Loading → Error → Data → Empty). SSR-only pages use the 2-branch pattern (HasData / Empty).

For the comprehensive design system specification (color tokens, typography scale, spacing, elevation, component styling guidance), see **docs/design-system.md**.

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
4. Prefer constructor injection for services and handlers; do not use service locator (`IServiceProvider`) in application code. Exception: Blazor components use `[Inject]` property injection as required by the framework (see Gold Standard section).
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

### Interactive Server Page Pattern with 4-Branch State Management

> **Scope:** This pattern applies **only** to pages/components that use `@rendermode InteractiveServer`. For SSR-only pages, data is fetched once in `OnInitializedAsync` with no client-side re-rendering — those pages render once after `OnInitializedAsync` completes and cannot use event callbacks like `OnClick` or `@bind-Value`.

The **4-branch rendering pattern** is mandatory for Interactive Server components that load data asynchronously: Loading → Error → Data → Empty. This ensures consistent UX feedback, clear error recovery, and prevents visual jank.

**Branch 1: Loading State** — Display while fetching data from the API.  
**Branch 2: Error State** — Show user-safe error message with a "Try Again" button.  
**Branch 3: Data State** — Show populated table/content only when data is non-empty.  
**Branch 4: Empty State** — Show helpful message when no results match filters or no data exists.

```razor
@* Presentation/Components/Pages/Products.razor — Interactive Server (requires @rendermode) *@
@page "/products"
@rendermode InteractiveServer
@attribute [Authorize]
@inject ProductApiClient ApiClient
@inject NavigationManager Nav

<PageTitle>Products</PageTitle>

<div style="display: flex; flex-direction: column; gap: 16px;">
    <MudText Typo="Typo.h5">Products</MudText>

    @if (Loading)
    {
        <!-- Branch 1: Loading State — Show progress while fetching -->
        <MudProgressCircular Indeterminate="true" />
    }
    else if (HasError)
    {
        <!-- Branch 2: Error State — Show error with retry -->
        <MudAlert Severity="Severity.Error" Icon="@Icons.Material.Filled.ErrorOutline">
            <MudStack Row="false" Spacing="2">
                <MudText Typo="Typo.body2">@ErrorMessage</MudText>
                <MudButton Variant="Variant.Text" Size="Size.Small" Color="Color.Error" OnClick="RefreshAsync">
                    Try Again
                </MudButton>
            </MudStack>
        </MudAlert>
    }
    else if (FilteredProducts.Any())
    {
        <!-- Branch 3: Data State — Show table with controls -->
        <div style="display: flex; gap: 8px; margin-bottom: 16px; align-items: center;">
            <MudTextField @bind-Value="_searchText" Placeholder="Search by name..." 
                Variant="Variant.Outlined" Margin="Margin.Dense" 
                Adornment="Adornment.End" AdornmentIcon="@Icons.Material.Filled.Search" 
                Style="flex: 0 1 300px;" />
            <MudButton Variant="Variant.Text" Size="Size.Small" OnClick="ResetSearch">
                Clear
            </MudButton>
            <MudSpacer />
            <MudButton Variant="Variant.Filled" Color="Color.Primary" 
                StartIcon="@Icons.Material.Filled.Add" Size="Size.Small"
                href="/products/create">
                New Product
            </MudButton>
        </div>

        <MudTable Items="@FilteredProducts" Hover="true" Breakpoint="Breakpoint.Sm">
            <HeaderContent>
                <MudTh>Name</MudTh>
                <MudTh>Description</MudTh>
                <MudTh Style="text-align: right;">Actions</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd DataLabel="Name">@context.Name</MudTd>
                <MudTd DataLabel="Description">@context.Description</MudTd>
                <MudTd DataLabel="Actions" Style="text-align: right;">
                    <MudButton Variant="Variant.Text" Color="Color.Primary" Size="Size.Small"
                        href="@Nav.GetUriByPage("/ProductDetails", new { id = context.Id })">
                        View
                    </MudButton>
                </MudTd>
            </RowTemplate>
        </MudTable>

    }
    else
    {
        <!-- Branch 4: Empty State — Show helpful message -->
        <MudAlert Severity="Severity.Info" Icon="@Icons.Material.Filled.Info">
            <MudStack Row="false" Spacing="2">
                <MudText>No products found.</MudText>
                <MudText Typo="Typo.body2">
                    @if (!string.IsNullOrEmpty(_searchText))
                    {
                        <span>Try adjusting your search filters or <MudLink Href="javascript:void(0)" OnClick="ResetSearch">clear all filters</MudLink>.</span>
                    }
                    else
                    {
                        <span>Create your first product to get started.</span>
                    }
                </MudText>
                <MudButton Variant="Variant.Filled" Color="Color.Primary" 
                    StartIcon="@Icons.Material.Filled.Add" Size="Size.Small"
                    href="/products/create">
                    Create Product
                </MudButton>
            </MudStack>
        </MudAlert>
    }
</div>
```

```csharp
// Presentation/Components/Pages/Products.razor.cs — code-behind with state management
using Microsoft.AspNetCore.Authorization;

public partial class Products
{
    private List<ProductBriefDto> _products = new();
    private string _searchText = string.Empty;
    
    public bool Loading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    public IEnumerable<ProductBriefDto> FilteredProducts =>
        _products
            .Where(p => string.IsNullOrEmpty(_searchText) || 
                        p.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Loading = true;
            HasError = false;
            ErrorMessage = null;
            
            PaginatedList<ProductBriefDto>? result = await ApiClient.GetAllAsync();
            _products = result?.Items ?? new List<ProductBriefDto>();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = "Failed to load products. Please try again.";
            _products = new List<ProductBriefDto>();
        }
        finally
        {
            Loading = false;
        }
    }

    private void ResetSearch()
    {
        _searchText = string.Empty;
    }

    private async Task RefreshAsync()
    {
        await OnInitializedAsync();
    }
}
```

## Gold Standard State Management for Interactive Server Components

> **Scope:** This section applies **only** to pages/components that use `@rendermode InteractiveServer`. For SSR-only pages, data is fetched once in `OnInitializedAsync` with no client-side re-rendering, making Loading states invisible to users, and cannot use event callbacks.

**Gold Standard** state management establishes a consistent, production-grade pattern for Interactive Server Blazor components that fetch and display data. This ensures predictable behavior, clear error handling, and reliable user experience across the application.

### State Structure (Complete Checklist)

Every component following the 4-branch pattern must include:

```csharp
public partial class YourComponentName
{
    // ============ INJECTED DEPENDENCIES ============
    [Inject] public YourApiClient ApiClient { get; set; } = null!;
    [Inject] public ILogger<YourComponentName> Logger { get; set; } = null!;

    // ============ PRIVATE FIELDS (Data Storage) ============
    // Raw data from API — never displayed directly
    private List<YourDto> _data = new();
    
    // User input for filtering/searching
    private string _searchText = string.Empty;
    private string? _selectedCategory = null;

    // ============ PUBLIC PROPERTIES (State) ============
    // THREE MANDATORY STATE FLAGS
    public bool Loading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }

    // ============ COMPUTED PROPERTIES (Derived State) ============
    // Always recalculate — never cache these
    public IEnumerable<YourDto> FilteredData =>
        _data
            .Where(item => ApplySearchFilter(item))
            .Where(item => ApplyStatusFilter(item))
            .ToList();

    public int ResultCount => FilteredData.Count();
    public bool HasResults => FilteredData.Any();

    // ============ LIFECYCLE ============
    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    // ============ PRIVATE METHODS (Internal Logic) ============
    private async Task LoadDataAsync()
    {
        try
        {
            // Reset state before fetch
            Loading = true;
            HasError = false;
            ErrorMessage = null;

            // Fetch from API
            PaginatedList<YourDto> result = await ApiClient.GetAsync();
            _data = result?.Items ?? new List<YourDto>();

            Logger.LogInformation("Loaded {Count} items.", _data.Count);
        }
        catch (HttpRequestException ex)
        {
            HasError = true;
            ErrorMessage = "Network error. Please check your connection and try again.";
            Logger.LogError(ex, "Network error loading data");
            _data = new List<YourDto>();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = "Failed to load data. Please try again or contact support.";
            Logger.LogError(ex, "Unexpected error loading data");
            _data = new List<YourDto>();
        }
        finally
        {
            Loading = false;
        }
    }

    private bool ApplySearchFilter(YourDto item) =>
        string.IsNullOrEmpty(_searchText) ||
        item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
        item.Description?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true;

    private bool ApplyStatusFilter(YourDto item) =>
        _selectedCategory == null || item.Category == _selectedCategory;

    private void ResetFilters()
    {
        _searchText = string.Empty;
        _selectedCategory = null;
        HasError = false;
        ErrorMessage = null;
    }

    private void ClearSearch()
    {
        _searchText = string.Empty;
    }

    // ============ PUBLIC METHODS (User Actions) ============
    public async Task RefreshAsync()
    {
        await LoadDataAsync();
    }
}
```

### Razor Template (4-Branch Rendering)

```razor
@* Always check in this exact order: Loading → Error → Data → Empty *@

@if (Loading)
{
    <!-- BRANCH 1: LOADING STATE -->
    <MudProgressCircular Indeterminate="true" />
}
else if (HasError)
{
    <!-- BRANCH 2: ERROR STATE -->
    <MudAlert Severity="Severity.Error" Icon="@Icons.Material.Filled.ErrorOutline">
        <MudStack Row="false" Spacing="2">
            <MudText Typo="Typo.body2">@ErrorMessage</MudText>
            <MudButton Variant="Variant.Text" Size="Size.Small" Color="Color.Error" OnClick="RefreshAsync">
                <MudIcon Icon="@Icons.Material.Filled.Refresh" Size="Size.Small" />
                Try Again
            </MudButton>
        </MudStack>
    </MudAlert>
}
else if (HasResults)
{
    <!-- BRANCH 3: DATA STATE — Controls + Table + Results -->
    
    <!-- Toolbar with Search, Filters, Actions -->
    <div style="display: flex; gap: 8px; margin-bottom: 16px; align-items: center; flex-wrap: wrap;">
        <MudTextField @bind-Value="_searchText" Placeholder="Search..." 
            Variant="Variant.Outlined" Margin="Margin.Dense" 
            Adornment="Adornment.End" AdornmentIcon="@Icons.Material.Filled.Search" 
            Class="flex-grow-1" />
        
        <MudSelect @bind-Value="_selectedCategory" Label="Category" 
            Variant="Variant.Outlined" Margin="Margin.Dense" 
            Dense="true" Style="min-width: 200px;" ClearButton="true">
            <MudSelectItem Value="@((string?)null)">All</MudSelectItem>
            <MudSelectItem Value="Active">Active</MudSelectItem>
            <MudSelectItem Value="Archived">Archived</MudSelectItem>
        </MudSelect>

        <MudSpacer />

        <div style="display: flex; gap: 8px;">
            <MudButton Variant="Variant.Text" Size="Size.Small" OnClick="ClearSearch">
                Clear Filters
            </MudButton>
            <MudButton Variant="Variant.Filled" Color="Color.Primary" 
                StartIcon="@Icons.Material.Filled.Add" Size="Size.Small"
                href="/your-component/create">
                New Item
            </MudButton>
        </div>
    </div>

    <!-- Results Counter -->
    <MudText Typo="Typo.caption" Class="mb-3">
        Showing @ResultCount result@(ResultCount != 1 ? "s" : "")
    </MudText>

    <!-- Data Table -->
    <MudTable Items="@FilteredData" Hover="true" Breakpoint="Breakpoint.Sm" Dense="true">
        <HeaderContent>
            <MudTh>Name</MudTh>
            <MudTh>Category</MudTh>
            <MudTh>Status</MudTh>
            <MudTh Style="text-align: right;">Actions</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Name">@context.Name</MudTd>
            <MudTd DataLabel="Category">@context.Category</MudTd>
            <MudTd DataLabel="Status">
                <MudChip Variant="Variant.Text" Color="@GetStatusColor(context)">
                    @context.Status
                </MudChip>
            </MudTd>
            <MudTd DataLabel="Actions" Style="text-align: right;">
                <MudButton Variant="Variant.Text" Color="Color.Primary" Size="Size.Small"
                    href="@GetDetailUrl(context)">
                    View
                </MudButton>
            </MudTd>
        </RowTemplate>
    </MudTable>
}
else
{
    <!-- BRANCH 4: EMPTY STATE -->
    <MudAlert Severity="Severity.Info" Icon="@Icons.Material.Filled.Info">
        <MudStack Row="false" Spacing="2">
            <MudText Typo="Typo.body2">
                @if (!string.IsNullOrEmpty(_searchText) || _selectedCategory != null)
                {
                    <span>No results match your filters.</span>
                }
                else
                {
                    <span>No items yet.</span>
                }
            </MudText>
            <MudText Typo="Typo.caption">
                @if (!string.IsNullOrEmpty(_searchText))
                {
                    <span>Try clearing your search or filters to see all items.</span>
                }
                else
                {
                    <span>Create your first item to get started.</span>
                }
            </MudText>
            @if (!string.IsNullOrEmpty(_searchText) || _selectedCategory != null)
            {
                <MudButton Variant="Variant.Text" Size="Size.Small" Color="Color.Info" OnClick="ResetFilters">
                    Clear All Filters
                </MudButton>
            }
            else
            {
                <MudButton Variant="Variant.Filled" Color="Color.Primary" 
                    StartIcon="@Icons.Material.Filled.Add" Size="Size.Small"
                    href="/your-component/create">
                    Create Item
                </MudButton>
            }
        </MudStack>
    </MudAlert>
}
```

### Gold Standard Rules Checklist

When implementing state management, enforce these non-negotiables:

1. **Initialization**
   - Set `Loading = true` at the start of `OnInitializedAsync`
   - Set `Loading = false` **only in the finally block** — ensures it's always set, regardless of error
   - Initialize all fields to safe defaults (empty collections, null strings)

2. **Error Handling**
   - Use **three error levels**: Network (HttpRequestException), Business (known exceptions), Unexpected (catch-all)
   - **Always set a user-safe error message** — never expose stack traces or internal details
   - Log errors at appropriate levels: Warning for network, Error for unexpected
   - Provide a "Try Again" button in error state

3. **Computed Properties**
   - Mark all filter/search results as computed (not cached) — recalculate on every render
   - Use `@if (HasResults)` not `@if (_data.Any())` — centralize result logic in property
   - Never cache computed results in fields; always derive from source data

4. **State Branches (Exact Order)**
   1. `@if (Loading)` — Show spinner
   2. `@else if (HasError)` — Show error + retry
   3. `@else if (HasResults)` — Show table + controls
   4. `@else` — Show empty state + guidance

5. **Filtering & Search**
   - Store user input in **private fields** (`_searchText`, `_selectedCategory`)
   - Implement filter logic in **private methods** that return `bool` (pure functions)
   - Display filter state in UI (e.g., "Showing X results", "Clear Filters" button)
   - Show contextual guidance in empty state when filters are active

6. **User Actions**
   - Provide `ResetFilters()` to clear all filters and error state
   - Provide `ClearSearch()` for individual search field
   - Provide `RefreshAsync()` for user-initiated data reload
   - Show result count when data is displayed

7. **Dependency Injection**
   - Inject `ApiClient` as `[Inject]` property (Blazor components do not support constructor injection)
   - Inject `ILogger<T>` for diagnostics
   - Inject `NavigationManager` only if needed for routing

8. **Private vs Public**
   - **Private**: Raw data (`_jobs`), user input (`_searchText`), internal methods
   - **Public**: State flags (`Loading`, `HasError`, `ErrorMessage`), computed results (`FilteredJobs`)
   - No public setters on computed properties — they are read-only

9. **Null Safety**
   - Always initialize collections to `new()` — never leave them null
   - Use `?? new List<T>()` when API returns null
   - Check `string.IsNullOrEmpty()` before displaying user input in UI

10. **Performance**
    - Avoid `StateHasChanged()` calls — let Blazor handle rendering automatically
    - Keep component-level state minimal — move shared state to parent or service
    - Use `@key` directive only for large dynamic lists (tables with many rows)

### Anti-Patterns to Avoid

- DO NOT: Directly displaying `_data` in template — use `FilteredData` computed property
- DO NOT: Setting `Loading = false` in multiple places — use try/catch/finally
- DO NOT: Storing API result in a property that changes unexpectedly — use consistent data flow
- DO NOT: Catching exceptions silently without logging — always log errors
- DO NOT: Showing raw error messages to users — translate to safe, actionable messages
- DO NOT: Rendering without checking `Loading`, `HasError`, or `HasResults` — always use 4 branches
- DO NOT: Mixing filter logic in the template — keep LINQ in code-behind
- DO NOT: Forgetting to initialize state flags to safe defaults — assume first-load is always Loading
- DO NOT: Hardcoding retry logic — provide a "Try Again" button in error state
- DO NOT: Using `OnAfterRender` for initial data load — use `OnInitializedAsync` only

### Testing Gold Standard State Management

```csharp
[Fact]
public async Task OnInitializedAsync_WithSuccessfulApiCall_PopulatesDataAndSetsLoadingFalse()
{
    // Arrange
    Mock<YourApiClient> mockClient = new Mock<YourApiClient>();
    Mock<ILogger<YourComponent>> mockLogger = new Mock<ILogger<YourComponent>>();
    mockClient.Setup(c => c.GetAsync())
        .ReturnsAsync(new PaginatedList<YourDto> { Items = new() { new YourDto { Name = "Test" } } });

    YourComponent component = new YourComponent
    {
        ApiClient = mockClient.Object,
        Logger = mockLogger.Object
    };

    // Act
    await component.OnInitializedAsync();

    // Assert
    component.Loading.Should().BeFalse();
    component.HasError.Should().BeFalse();
    component.ResultCount.Should().Be(1);
    mockClient.Verify(c => c.GetAsync(), Times.Once);
}

[Fact]
public async Task OnInitializedAsync_WithApiException_SetsErrorMessageAndLoggingError()
{
    // Arrange
    Mock<YourApiClient> mockClient = new Mock<YourApiClient>();
    Mock<ILogger<YourComponent>> mockLogger = new Mock<ILogger<YourComponent>>();
    mockClient.Setup(c => c.GetAsync())
        .ThrowsAsync(new HttpRequestException("Network failed"));

    YourComponent component = new YourComponent
    {
        ApiClient = mockClient.Object,
        Logger = mockLogger.Object
    };

    // Act
    await component.OnInitializedAsync();

    // Assert
    component.Loading.Should().BeFalse();
    component.HasError.Should().BeTrue();
    component.ErrorMessage.Should().Contain("Network error");
}

[Fact]
public void FilteredData_WithActiveSearchText_ReturnsOnlyMatchingItems()
{
    // Arrange
    YourComponent component = new YourComponent();
    component._data = new()
    {
        new YourDto { Name = "Alpha Item" },
        new YourDto { Name = "Beta Item" },
        new YourDto { Name = "Gamma Item" }
    };
    component._searchText = "Alpha";

    // Act
    IEnumerable<YourDto> result = component.FilteredData;

    // Assert
    result.Should().HaveCount(1);
    result.First().Name.Should().Be("Alpha Item");
}
```

### Testing Rules for Gold Standard

- Test initial load with success and error paths (Happy + Sad paths)
- Verify `Loading` is false after completion (success or error)
- Verify `HasError` and `ErrorMessage` are set appropriately on exception
- Verify filtered results match the filter criteria exactly
- Mock `ILogger` and verify error logs are created on exceptions
- Test `ResetFilters()` clears all state (search, category, error)
- Never mock `ApiClient` methods without setting up return values
- Use `It.IsAny<T>()` for logger assertions when exact values don't matter

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

When implementing features, always check security guidelines for:
- **Critical:** JWT secrets, Auth attributes, CORS, health checks, file uploads
- **High:** Log injection, HttpClient pooling, OTP rate limiting, CSP headers
- **Medium:** Tenant isolation, ownership checks, caching strategy

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
| **SKILL.md** (this file) | Agent capabilities, practical code patterns, testing strategies |

For fast onboarding, follow this path:
1. Read **copilot-instructions.md** sections 13–14 for architecture + feature checklist
2. Reference security guidelines when touching auth, data, or file handling
3. Use the patterns in this document as copy-paste templates
