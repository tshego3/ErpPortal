# Engineering Rules (.NET 10 Blazor SSR)

These rules are mandatory for all feature work, bug fixes, and refactors in this repository.

> **Quick navigation:** For a specific task, focus on the relevant section: Security (7), UI (8), Architecture (13–14), Testing (10). The Decision Guide below determines which rendering pattern applies.

> **Decision guide — which pattern applies?**
> - **Interactive Server pages** (greenfield features with `@rendermode InteractiveServer`): follow Section 19 Gold Standard (4-branch: Loading → Error → Data → Empty).
> - **SSR-only pages** (default): use the 2-branch approach (HasData / Empty, plus try/catch error handling in `OnInitializedAsync` surfaced via a message variable). SSR pages render once after `OnInitializedAsync` completes — there is no Loading spinner visible to the user.

## 1) Platform Identity

1. Stack is .NET 10 + ASP.NET Core + Blazor Web App.
2. Rendering model is SSR-only by default. Interactive Server render mode is permitted only when a page requires post-render user interaction that cannot be achieved via form POST round-trips (e.g., real-time data grids with inline editing). Pages using interactivity must document the justification in a code comment.
3. UI framework is MudBlazor.
4. Architecture is clean-layered: Domain, Application, Infrastructure, and Presentation.
5. Authentication is cookie-based via server endpoints.

## 2) Non-Negotiable Architecture Rules

1. Domain layer must remain pure and independent of Web/UI/infrastructure concerns.
2. Infrastructure may depend on Domain and Application abstractions, never the reverse.
3. Presentation composes application behavior but should not contain business rules.
4. Do not bypass abstractions by calling external APIs directly from Razor components.
5. Register all concrete services through DI in Program.cs or dedicated dependency modules.
6. **Never create duplicate entity, DTO, or record types outside `Domain`.** All shared types (entities, stored-procedure result classes, value objects) must be defined in Domain and referenced directly. Do not create local copies any other layer. If a type does not yet exist in Domain, add it there first.

## 3) Dependency Injection Rules

1. Prefer constructor injection for controllers, services, and handlers.
2. Use scoped lifetimes for request/user-scoped services.
3. Use singleton only for stateless/shared services that are safe across requests.
4. In Development, keep DI validation enabled (ValidateScopes and ValidateOnBuild).
5. Do not use ad hoc service location in application code unless an existing pattern requires it.

## 4) Blazor SSR and Routing Rules

1. Default all pages/components to SSR and opt in to interactivity only when a clear requirement exists.
2. Pages that require HTTP header writes during request processing (for example auth POST flows) must use SSR patterns.
3. For auth forms, use POST endpoints with antiforgery tokens rather than interactive event handlers.
4. Keep login/logout flows controller-based to avoid response-header write timing issues.
5. Use Authorize attributes for protected pages and enforce deterministic redirects.
6. **Manage async component state by render mode:** Interactive Server components that fetch data must implement Section 19's Gold Standard 4-branch pattern (Loading → HasError → HasSourceData → Empty). SSR-only pages use the simplified 2-branch approach (HasData / Empty) with error handling via a message variable rendered on the single server pass.

## 5) Authentication and Session Rules

1. Use cookie authentication as configured in Program.cs.
2. Preserve secure cookie settings and avoid weakening security defaults without explicit approval.
3. Keep login path, logout path, expiration, and sliding expiration behavior coherent with current app policy.
4. Never expose tokens or sensitive claims in logs.
5. Privacy acceptance flow must remain enforceable: login leads to privacy acceptance before dashboard access.

## 6) API and Repository Rules

1. External API access must go through typed HTTP clients and repository/service abstractions.
2. Use delegating handlers for cross-cutting concerns such as auth token forwarding and error handling.
3. Keep error translation centralized (for example AppException patterns) and user-safe.
4. Avoid scattering endpoint URLs; read base URLs and options from configuration.
5. Apply cancellation tokens on async API operations.
6. **Legacy integration compatibility is mandatory** — when modernizing legacy routes, preserve old endpoint paths, request body contracts, and response shapes through backward-compatible adapters/aliases so existing integrations continue working unchanged.
7. **No duplicate APIs for legacy compatibility** — do not create separate controllers or action methods that duplicate existing functionality just to serve legacy routes. Instead, add legacy route aliases (e.g., `[HttpPost("/User/Login")]`) directly on the existing controller actions so that both old and new paths resolve to the same implementation. When an existing API already handles the business logic, wire legacy callers to it via route attributes or thin redirect controllers — never copy-paste the logic into a parallel endpoint.
8. **No phantom or unreferenced routes** — every route an endpoint exposes must be actively called by a known client. Do not use multiple class-level `[Route]` attributes that generate combinatorial routes nobody calls. Use absolute route templates (e.g., `[HttpPost("/api/auth/login")]`) when an action must be reachable at a path outside its controller's primary route prefix. Before adding a route, confirm at least one client references it.
9. **Dynamics API contract serialization rules** — contracts sent to the Dynamics 365 REST API must not inherit from `XppObjectBase`. The `XppObjectBase` is a god object with 100+ properties that pollutes serialized payloads and causes `XppServicesDeserializationException` on the Dynamics side. When creating or modifying contracts for Dynamics: (a) never inherit from `XppObjectBase` — define only the properties the contract needs directly, (b) all enum properties must be non-nullable so they serialize as integers (e.g., `0`) rather than `null` — Dynamics cannot deserialize `null` into X++ enums, (c) match the legacy WCF payload format, (d) `DateTime` fields default to `0001-01-01T00:00:00` (C# default) not null, (e) string properties may be null.

## 7) Security and Compliance Rules

1. Keep standard security headers active (CSP, X-Content-Type-Options, X-Frame-Options, etc.)
2. Keep crawler shield behavior unless explicitly changed by product/security decision.
3. Preserve antiforgery for all state-changing form actions.
4. Keep privacy content and consent behavior aligned with approved legal text.
5. Never hardcode secrets, credentials, or environment-specific sensitive data.
6. **Critical: Add `[Authorize]` attribute to all data controllers** — this is a production blocker.
7. **Critical: JWT secrets must be stored in User Secrets locally and Azure Key Vault in production.**
8. **High: Implement magic byte validation for file uploads** — do not rely on client Content-Type header.
9. **High: Restrict CORS to specific origins** — never use `AllowAnyOrigin()` with credentials.
10. **High: Enforce tenant isolation in all CQRS queries** — filter by `ICurrentUserService.TenantId`.
11. **Medium: Use `IHttpClientFactory` instead of `new HttpClient()`** — prevents socket exhaustion.
12. **Medium: Apply rate limiting to OTP endpoints** — prevents email/SMS flooding.
13. **Vet all dependencies before adoption** — before adding any NuGet package or npm dependency: (a) verify it exists on the official package registry (nuget.org, npmjs.com), (b) confirm the publisher/organization is legitimate, (c) check for known CVEs or security advisories, (d) review the package's download count, maintenance activity, and last publish date, (e) prefer packages with a clear license (MIT, Apache 2.0). Do not blindly trust AI-suggested package names — they may not exist or may be typosquatted.

## 8) UI and Design System Rules

Rules below are derived from the design system and apply to all presentation screens.

1. Follow the White-Labeled Enterprise UI direction: premium, authoritative, high-signal layouts.
2. Apply the no-line rule for sectioning: use tonal surface layering instead of border-heavy grids.
3. Use dynamic brand colors from appsettings/configuration; avoid hard-coded palette divergence.
4. Keep typography aligned to Libre Franklin hierarchy for both display and dense operational data.
5. Prefer atmospheric depth with gradients, surface tiers, and subtle shadow tinting over flat blocks.
6. Maintain responsive behavior for desktop and mobile without collapsing readability.
7. **No scoped `.razor.css` files and no `<style>` blocks in `.razor` components** — use only MudBlazor component API (props, variant, style classes, inline `Style=`) for all styling. Exception: global `wwwroot/app.css` only for baseline/framework-level styles (not component-specific). Never embed CSS directly in `.razor` files; all styling must be applied through component attributes or global stylesheets.
8. **No hardcoded colors anywhere in `.razor` or `.cs` files** — all color values must come from `BrandingConfig` (injected via `IOptions<BrandingConfig>`) or MudBlazor theme CSS variables (`var(--mud-palette-*)`). This ensures white-labeling and automatic dark/light mode compliance.
9. **Consult MudBlazor component API first** — before implementing any component styling or behavior, review the official MudBlazor documentation (https://mudblazor.com/docs/overview) for native props/features. Use MudBlazor's built-in theming, size/color enums, variant/density controls, and spacing utilities. Only fall back to inline `Style=` when MudBlazor provides no alternative.
10. **Separate markup and logic in new components** — create `.razor.cs` code-behind files for component logic, parameters, lifecycle, and event handlers. Keep `.razor` files focused on the view template. This improves readability, testability, and maintainability. Example: `MyComponent.razor` + `MyComponent.razor.cs`.
11. **Use the 4-branch rendering pattern for Interactive Server async data flows** — see Section 19 for the authoritative specification. All Interactive Server components (with `@rendermode InteractiveServer`) that load data asynchronously must render four states in order: Loading, HasError, HasSourceData, Empty. SSR-only pages use the 2-branch pattern (HasData / Empty).
12. **Filter UX must remain recoverable** — for searchable/filterable tables, render toolbars/search inputs based on the unfiltered source collection (for example `_data.Any()`), not filtered results. When a filter returns zero matches, keep the table shell and controls visible and rely on `NoRecordsContent` (or equivalent in-table empty messaging) so users can clear/adjust filters without getting trapped in page-level empty states.

## 9) Performance and Caching Rules

1. Use output caching policies for list and read-heavy endpoints/pages where applicable.
2. Use cache tags and invalidation paths when mutating data that affects cached reads.
3. Avoid expensive UI re-renders by keeping component state scoped and purposeful.
4. Do not introduce unnecessary middleware or per-request overhead.

## 10) Testing and Validation Rules

1. Build the affected projects after code changes.
2. Resolve compile errors and relevant analyzer warnings introduced by the change.
3. Add or update tests for behavior changes in controllers, services, and critical flows.
4. Validate authentication and authorization regressions whenever auth-related code is touched.
5. Validate privacy and redirect flows end-to-end when those areas are modified.

## 11) Logging and Observability Rules

1. Log meaningful operational events (login success/failure, critical exceptions).
2. Avoid noisy or duplicate logs that reduce signal.
3. Use structured logging patterns and include actionable context.
4. Never log passwords, tokens, or raw personal information.
5. Always provide users with a descriptive, actionable failure reason when an operation fails.
6. User-facing error messages must be clear and safe: explain what failed and the next step, without exposing sensitive internals.

## 12) Change Management Rules

1. Prefer minimal, scoped changes over broad rewrites.
2. Do not refactor unrelated areas while implementing targeted fixes.
3. Keep naming, formatting, and coding style aligned with surrounding code.
4. Document new conventions in docs only when behavior or standards materially change.
5. Prefer explicit typing and return types: avoid `void`; **never use `var`** — always declare the explicit type. This applies to all declarations including loop variables, LINQ results, and `new` expressions.
6. Prefer .NET-native and Blazor-native implementations; use JavaScript only as a last resort when there is no practical framework-supported alternative.
7. No nested syntax: prefer flat, readable structures with guard clauses and early returns instead of deeply nested conditionals, loops, or list hierarchies.
8. AI agent compliance check is mandatory: before finalizing changes, verify the implementation aligns with `.github/copilot-instructions.md`, all relevant guidance under `.github/skills/`, and applicable guidance in `docs/`. **When a compliance check is run, also audit dependencies and security**: (a) verify all packages in `.csproj` / `packages.lock.json` are from known, legitimate publishers, (b) check for outdated or unmaintained dependencies (last publish > 12 months), (c) scan for known vulnerabilities (CVEs, security advisories) against current dependency versions, (d) confirm no secrets, API keys, or credentials are hardcoded in source or config files, (e) validate that all network calls use HTTPS, timeouts, and input validation, (f) ensure error messages do not leak internal details (stack traces, raw error descriptions, file paths).
9. Prefer `async Task`/`async Task<T>` over `async void`/`void` by default. Use `async void`/`void` only for framework-required event handlers.
10. Use generics for reusable, type-safe abstractions; avoid duplicated type-specific implementations when a constrained generic design is appropriate.
11. Keep code simple and straightforward. Avoid complex or clever patterns when a clear, direct implementation can meet the requirement.
12. If code cannot be understood quickly without comments, simplify it first; comments are for context, not to compensate for avoidable complexity.
13. Use direct, descriptive, and consistent naming for classes, methods, variables, and files.
14. Apply DRY by reusing existing related functionality where possible; extend or refactor existing components before creating parallel implementations.
15. Prefer low-boilerplate implementations: small methods, small diffs, and direct control flow that delivers high impact with minimal code.
16. Keep logging concise and high-signal: log intent, result, and failures with structured properties, but avoid verbose multi-line logging blocks when one clear statement is enough.
17. Reuse compact helper methods/constants for repeated error text or repeated computation to reduce noise and keep feature methods easy to scan.
18. For immutable Domain records used by forms, define a static factory on the record itself (for example `CreateEmpty()`) and consume that from UI code instead of creating local page-level `CreateEmptyModel` helpers.
19. **If hardcoded mock/placeholder data is found in components or pages, treat it as temporary image-to-code scaffolding and replace it before completion** — UI generation from designs may introduce sample text, dates, names, or numeric values, but these must not ship. Replace with parameterized bindings, service calls, or empty-state defaults; if backend wiring is pending, use clearly marked `// TODO:` stubs that return empty collections.
20. **Avoid unnecessary `if` statements** — do not wrap code in conditionals when the condition is always true, when a single expression or null-coalescing operator suffices, or when the branch adds no distinct behavior. Prefer ternary expressions, pattern matching, null-coalescing (`??`/`??=`), and early returns over redundant conditional blocks.
21. **Code is liability, not an asset.** Every line added must justify its existence. Prefer deleting code over adding it, and always pursue the smallest diff that solves the problem. If a feature can be achieved by removing or simplifying existing code instead of writing new code, do that.

## 13) Architecture Layers & Responsibilities

**Dependency flow (each layer references only layers below it):**
```markdown
Presentation  ──HTTP──►  API  ──MediatR──►  Application  ──►  Domain
                          │                       │
                          └─────────────────►  Infrastructure
```

| Layer | Responsibility | Key Files |
|-------|---|---|
| **Domain** | Pure business entities, value objects, domain exceptions. **Zero framework dependencies.** | `Entities/`, `Exceptions/`, `Events/`, `ValueObjects/` |
| **Application** | CQRS commands/queries (MediatR), DTOs, validation rules (FluentValidation), pipeline behaviours. References Domain only. | `Features/[Entity]/{Commands,Queries}`, `Common/Interfaces/` |
| **Infrastructure** | EF Core DbContext, migrations, external service implementations (blob storage, email, etc.). Implements Application interfaces. | `Persistence/`, `Services/`, `Seeding/` |
| **Infrastructure.Shared** | Cross-cutting infrastructure — white-label branding, configuration. | `Branding/` |
| **API** | ASP.NET Core controllers, middleware, JWT/cookie auth, dependency injection setup. Dispatches via MediatR. **Must apply `[Authorize]` on data endpoints.** | `Controllers/`, `Middleware/`, `Program.cs` |
| **Presentation** | Blazor Web App (SSR + Interactive Server), typed HttpClient API clients, Razor pages, authentication state. **Never reference Domain or Application directly — only call the API via HTTP.** | `Components/Pages/`, `Services/`, `Program.cs` |
| **Tests** | xUnit unit + integration tests. Mirrors Application folder structure. Use EF Core InMemory, never mock `DbSet<T>`. | `Features/[Entity]/{Commands,Queries}/` |

## 14) Adding a New Feature (Full Checklist)

Quick outline:

1. **Domain** — Create entity inheriting `AuditableEntity`
2. **Application** — Add `DbSet<Entity>` to `IApplicationDbContext`; create CQRS commands/queries
3. **Infrastructure** — Register `DbSet<Entity>` in `MockApplicationDbContext`
4. **API** — Create controller with `[Authorize]`, delegate to MediatR
5. **Presentation** — Create typed `BaseApiClient` subclass and Blazor pages
6. **Tests** — Add xUnit handler/validator tests using InMemory database

**Everything auto-wires** through `Program.cs` DI and MediatR assembly scanning — no manual registration needed for handlers/validators.

## 15) NASA JPL "Power of 10" Adaptation for C#

These principles are derived from NASA Jet Propulsion Laboratory's strict coding standards for safety-critical systems. Adapted for enterprise .NET development.

1. **Simple Control Flow**: Avoid complex recursion. Use iteration for predictable stack depth.
2. **Fixed Loops**: All loops must have a deterministic upper bound. Avoid infinite loops or logic that relies on external state to terminate without a safety break.
3. **No Dynamic Memory Allocation after Init**: In high-reliability paths, avoid excessive object instantiation to prevent GC pressure and "Stop-the-World" pauses. Use `Span<T>` and `Memory<T>` where appropriate.
4. **Small Functions**: No function should exceed 60 lines of code. If it's longer, it's doing too much.
5. **Low Assertion Density**: Use `Debug.Assert` and clear exception handling. Every function must validate its inputs.
6. **Data Hiding**: Limit the scope of variables to the smallest possible block.
7. **Check Return Values**: Never ignore a `Task` or a return value. Use `_ = Task.Run(...)` only if explicitly intended to fire-and-forget; otherwise, always await.
8. **Minimal Preprocessor Use**: Avoid complex `#if` directives. Use interfaces and Dependency Injection for platform-specific logic.
9. **Pointer Safety**: Avoid `unsafe` blocks unless absolutely required for high-performance interop. Prefer safe managed code.
10. **Compile-Time Warnings**: Treat all compiler warnings as errors.

## 16) Production Stability & Debuggability (The 2 AM Rules)

These rules ensure that production incidents can be diagnosed and resolved quickly with minimal guesswork, especially during on-call scenarios.

1. **Context-Rich Exceptions**: Never throw a generic `Exception`. Throw specific types and include the "state" (e.g., ID, UserContext) in the message.
   - *Bad:* `throw new Exception("Failed");`
   - *Good:* `throw new OrderProcessingException($"Failed to process order {orderId} for customer {userId}");`
2. **Defensive Logging**: Log the *intent* before an action and the *result* after. Use Structured Logging (Serilog/Message Templates) so logs are searchable by properties.
3. **Fail Fast**: Check for nulls, empty strings, and invalid ranges at the very beginning of a method (Guard Clauses).
4. **Idempotency by Default**: Write logic (especially in Background Workers/Hangfire) assuming it might run twice. Check state before executing side effects.
5. **CancellationToken Propagation**: Every async method must accept and honor a `CancellationToken`. No exceptions.
6. **No "Ghost" Errors**: Never use empty `catch {}` blocks. If you must ignore an error, log it as 'Information' or 'Debug' with a comment explaining why it is safe to ignore.
7. **Timeouts are Mandatory**: Any external call (HTTP, Database, Redis) must have a hard timeout. Never let a thread wait indefinitely.
8. **Telemetry over Debugging**: Write code that exports Metrics (OpenTelemetry). It's easier to look at a dashboard at 2 AM than to attach a remote debugger.
9. **Pure Logic Separation**: Keep business logic in "Pure Functions" (input in, output out) separate from I/O (Database/API). Pure functions are trivial to unit test and verify during a crisis.
10. **Avoid Global State**: Static variables are the enemy of thread safety. Use Scoped or Transient lifetimes via Dependency Injection.

## 17) Testability and Architecture Rules

1. **Constructor Injection Only**: Always use Constructor Injection for dependencies. Avoid `internal` or `public` setters for dependencies unless strictly required for specific frameworks.
2. **Interface-Based Abstractions**: Define interfaces (`IService`, `IRepository`) for any class that performs I/O or contains volatile logic. Program to the interface, not the implementation.
3. **High Cohesion, Low Coupling**: Each class should have a single responsibility. If a constructor has more than 5 dependencies, suggest breaking the class into smaller components.
4. **Avoid 'new' for Logic**: Never instantiate dependencies (e.g., `new HttpClient()` or `new DbContext()`) inside a class. Require them from the DI container.
5. **Pure Logic Extraction**: Move complex algorithmic logic into "Pure" static methods or helper classes that do not depend on external state, making them trivial to unit test.
6. **Mockability**: Ensure methods are either virtual or defined in an interface so they can be mocked using Moq or NSubstitute.
7. **No Service Locator Pattern**: Never inject `IServiceProvider`. Explicitly request the dependencies you need in the constructor.
8. **Composition over Inheritance**: Prefer wrapping functionality in a new class (Decorator or Strategy pattern) rather than creating deep inheritance hierarchies.
9. **Sealed by Default**: Mark classes as `sealed` unless they are explicitly designed for inheritance. This prevents "accidental" extensibility that breaks logic.
10. **Test Data Builders**: When generating unit tests, use the Builder pattern or AutoFixture approach to keep tests resilient to constructor changes.

## 18) Records vs Classes Rules

1. **Prefer Records for Data**: Use `public record Name(Type Prop, ...);` for all DTOs, events, and value objects. This ensures immutability and simplifies unit test assertions.
2. **Use Classes for Services**: Use `class` for any object registered in the DI container that contains logic or manages external resources (I/O).
3. **Avoid Mixed Records**: Do not create mutable records (using `set;`). If data must change, use the `with` keyword to create a new instance.
4. **Value Equality in Tests**: When testing logic that returns data, leverage record equality for assertions (e.g., `actualRecord.Should().Be(expectedRecord);`).
5. **Brief Descriptions**: Ensure classes have clear lifecycle roles, while records remain "dumb" data holders with minimal-to-no logic.

## 19) Gold Standard State Management for Blazor Components

**Mandatory pattern** for Interactive Server components that fetch and display data. This establishes consistent, production-grade behavior for interactive pages across the presentation layer.

> **Scope:** This pattern applies **only** to pages/components that use `@rendermode InteractiveServer`. For SSR-only pages, data is fetched once in `OnInitializedAsync` with no client-side re-rendering. Do **not** add Loading/HasError state branches to SSR-only pages; they render a single pass after `OnInitializedAsync` completes, making the Loading flag invisible to users.

### State Structure (Required Elements)

Every async data-loading component **must** include:

1. **Injected Dependencies**
   - `[Inject] public YourApiClient ApiClient { get; set; } = null!;`
   - `[Inject] public ILogger<YourComponent> Logger { get; set; } = null!;`

2. **Private Fields** (for data storage and user input)
   - `private List<YourDto> _data = new();` — raw API response
   - `private string _searchText = string.Empty;` — user input for filters
   - `private string? _selectedCategory = null;` — category filter state

3. **Public State Properties** (three mandatory flags)
   - `public bool Loading { get; set; } = true;` — initially true
   - `public bool HasError { get; set; }` — tracks error state
   - `public string? ErrorMessage { get; set; }` — user-safe error message

4. **Computed Properties** (derived state, never cached)
   - `public IEnumerable<YourDto> FilteredData => _data.Where(ApplyFilters).ToList();`
   - `public int ResultCount => FilteredData.Count();`
   - `public bool HasSourceData => _data.Any();`

5. **Initialization** (in `OnInitializedAsync`)
   - Set `Loading = true` at start
   - Set `Loading = false` **only in finally block** — ensures it's always set
   - Always wrap in try/catch/finally

6. **Error Handling** (three levels)
   - **Network errors** (HttpRequestException): "Network error. Please check your connection and try again."
   - **Business errors** (known exceptions): Domain-specific message
   - **Unexpected errors** (catch-all): "Failed to load data. Please try again or contact support."
   - Log all errors with context; never expose stack traces to users

7. **User Actions**
   - `RefreshAsync()` — user-triggered data reload
   - `ResetFilters()` — clear all filters and error state
   - `ClearSearch()` — clear search field only

### 4-Branch Rendering Pattern (Exact Order)

```
@if (Loading)
    --> Show MudProgressCircular or MudProgressLinear
else if (HasError)
    --> Show MudAlert with error message + "Try Again" button
else if (HasSourceData)
    --> Show table + toolbar with filters + result count
else
    --> Show MudAlert empty state with contextual guidance
```

### Non-Negotiable Rules

1. **Always use try/catch/finally** — `Loading = false` in finally block only
2. **Never display raw data** — always use `FilteredData` computed property
3. **Never cache computed results** — recalculate on every render
4. **Always provide user-safe error messages** — never expose internals
5. **Always log errors** — use `ILogger.LogError()` with exception
6. **Always show result count** when data is displayed
7. **Always show "Try Again" button** in error state
8. **Always initialize collections to `new()`** — never leave them null
9. **Always use `@key` only for large dynamic lists** — avoid unnecessary directives
10. **Never call `StateHasChanged()` manually** — Blazor re-renders automatically after lifecycle methods and event handlers
11. **For filterable grids, never gate toolbar/table visibility on filtered rows** — use source-data availability for branch selection and in-table `NoRecordsContent` for zero-match filters.

### Anti-Patterns (Explicitly Forbidden)

- DO NOT: Displaying `_data` directly in template
- DO NOT: Setting `Loading = false` in multiple places
- DO NOT: Silent exception handling (catch without logging)
- DO NOT: Showing raw error messages to users
- DO NOT: Rendering without checking `Loading` or `HasResults`
- DO NOT: Mixing filter logic in the template
- DO NOT: Driving page-level empty states from `FilteredData.Any()` in searchable tables
- DO NOT: Forgetting to initialize state flags
- DO NOT: Using `OnAfterRender` for initial data load (use `OnInitializedAsync` only)
- DO NOT: Hardcoding retry logic (provide a button instead)
- DO NOT: Storing computed results in fields

### Testing Requirements

Every component using this pattern **must** be tested with:
1. **Happy path** — successful API call populates data and sets `Loading = false`
2. **Error path** — exception sets `HasError = true`, `ErrorMessage` is user-safe, error is logged
3. **Filter path** — `FilteredData` matches filter criteria exactly
4. **Empty path** — empty result set shows empty state with contextual guidance
5. **Reset path** — `ResetFilters()` clears all state including errors

See **SKILL.md** for complete implementation examples, code patterns, and detailed testing strategies.

### Reference

For comprehensive implementation guidance including complete code examples, Razor templates, testing patterns, and detailed anti-patterns, see **skills/SKILL.md** section "Gold Standard State Management for Blazor Components".
