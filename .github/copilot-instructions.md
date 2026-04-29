# Engineering Rules (.NET 10 Blazor SSR)

These rules are mandatory for all feature work, bug fixes, and refactors in this repository.

## 1) Platform Identity

1. Stack is .NET 10 + ASP.NET Core + Blazor Web App.
2. Rendering model is SSR-first; interactivity is opt-in per page/component only where required.
3. UI framework is MudBlazor.
4. Architecture is clean-layered: Domain, Application, Infrastructure, and Presentation.
5. Authentication is cookie-based via server endpoints.

## 2) Non-Negotiable Architecture Rules

1. Domain layer must remain pure and independent of Web/UI/infrastructure concerns.
2. Infrastructure may depend on Domain and Application abstractions, never the reverse.
3. Presentation composes application behavior but should not contain business rules.
4. Do not bypass abstractions by calling external APIs directly from Razor components.
5. Register all concrete services through DI in Program.cs or dedicated dependency modules.

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

## 8) UI and Design System Rules

Rules below are derived from the design system and apply to all presentation screens.

1. Follow the White-Labeled Enterprise UI direction: premium, authoritative, high-signal layouts.
2. Apply the no-line rule for sectioning: use tonal surface layering instead of border-heavy grids.
3. Use dynamic brand colors from appsettings/configuration; avoid hard-coded palette divergence.
4. Keep typography aligned to the project font hierarchy for both display and dense operational data.
5. Prefer atmospheric depth with gradients, surface tiers, and subtle shadow tinting over flat blocks.
6. Maintain responsive behavior for desktop and mobile without collapsing readability.
7. **No scoped `.razor.css` files** — use only MudBlazor component API (props, variant, style classes, inline `Style=`) for all styling. Exception: global `wwwroot/app.css` only for baseline/framework-level styles (not component-specific).
8. **No hardcoded colors anywhere in `.razor` or `.cs` files** — all color values must come from `BrandingConfig` (injected via `IOptions<BrandingConfig>`) or MudBlazor theme CSS variables (`var(--mud-palette-*)`). This ensures white-labeling and automatic dark/light mode compliance.
9. **Consult MudBlazor component API first** — before implementing any component styling or behavior, review the official MudBlazor documentation (https://mudblazor.com/docs/overview) for native props/features. Use MudBlazor's built-in theming, size/color enums, variant/density controls, and spacing utilities. Only fall back to inline `Style=` when MudBlazor provides no alternative.
10. **Separate markup and logic in new components** — create `.razor.cs` code-behind files for component logic, parameters, lifecycle, and event handlers. Keep `.razor` files focused on the view template. This improves readability, testability, and maintainability. Example: `MyComponent.razor` + `MyComponent.razor.cs`.

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

## 12) Change Management Rules

1. Prefer minimal, scoped changes over broad rewrites.
2. Do not refactor unrelated areas while implementing targeted fixes.
3. Keep naming, formatting, and coding style aligned with surrounding code.
4. Document new conventions in docs only when behavior or standards materially change.
5. Prefer explicit typing and return types: avoid `var` and `void`; use them only as a last resort when no clear alternative exists.
6. Prefer .NET-native and Blazor-native implementations; use JavaScript only as a last resort when there is no practical framework-supported alternative.
7. No nested syntax: prefer flat, readable structures with guard clauses and early returns instead of deeply nested conditionals, loops, or list hierarchies.
8. AI agent compliance check is mandatory: before finalizing changes, verify the implementation aligns with `.github/copilot-instructions.md`, all relevant guidance under `.github/skills/`, and applicable guidance in `docs/`.
9. Prefer `async Task`/`async Task<T>` over `async void` by default. Use `async void` only for framework-required event handlers.
10. Use generics for reusable, type-safe abstractions; avoid duplicated type-specific implementations when a constrained generic design is appropriate.

---

## 13) Architecture Layers & Responsibilities

**Dependency flow (each layer references only layers below it):**
```
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

---

## 14) Adding a New Feature (Full Checklist)

Quick outline:

1. **Domain** — Create entity inheriting `AuditableEntity`
2. **Application** — Add `DbSet<Entity>` to `IApplicationDbContext`; create CQRS commands/queries
3. **Infrastructure** — Register `DbSet<Entity>` in `MockApplicationDbContext`
4. **API** — Create controller with `[Authorize]`, delegate to MediatR
5. **Presentation** — Create typed `BaseApiClient` subclass and Blazor pages
6. **Tests** — Add xUnit handler/validator tests using InMemory database

**Everything auto-wires** through `Program.cs` DI and MediatR assembly scanning — no manual registration needed for handlers/validators.

---

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

---

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

---

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

---

## 18) Records vs Classes Rules

1. **Prefer Records for Data**: Use `public record Name(Type Prop, ...);` for all DTOs, events, and value objects. This ensures immutability and simplifies unit test assertions.
2. **Use Classes for Services**: Use `class` for any object registered in the DI container that contains logic or manages external resources (I/O).
3. **Avoid Mixed Records**: Do not create mutable records (using `set;`). If data must change, use the `with` keyword to create a new instance.
4. **Value Equality in Tests**: When testing logic that returns data, leverage record equality for assertions (e.g., `actualRecord.Should().Be(expectedRecord);`).
5. **Brief Descriptions**: Ensure classes have clear lifecycle roles, while records remain "dumb" data holders with minimal-to-no logic.
