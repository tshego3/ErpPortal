---
name: global-rules
description: Mandatory engineering rules for the ERP Portal — platform identity, architecture, DI, cookie auth, API gateway access, security, MudBlazor UI, performance, testing, logging, and change-management rules. Use for all feature work, bug fixes, and refactors in this repo.
---

# ERP Portal Engineering Rules (.NET 10 Blazor SSR)

Mandatory for all feature work, bug fixes, and refactors in this repository.

> **Motto:** Move fast. Be clear. Do not overcomplicate.
> **Communication style:** Use the **`dev-concise` output style** (`.agents/output-styles/dev-concise.md`) — concise and direct, three-part reports (Done / Works? / Next), no padded preamble or closing summaries, but never omit needed information (paths, line numbers, root causes, trade-offs, failures, assumptions, uncertainty). See the project-expert skill.
> **Decision guide — which rendering pattern applies?** Interactive Server pages (`@rendermode InteractiveServer`) follow the Gold Standard 4-branch pattern (Loading → Error → Data → Empty). SSR-only pages use the simplified 2-branch approach (HasData / Empty) with try/catch error handling in `OnInitializedAsync` surfaced via a message variable.

## 1) Platform Identity
1. .NET 10 + ASP.NET Core + Blazor Web App across two projects: `ErpPortal` (web app: Core, Infrastructure, Components, Controllers) and `ErpPortal.Api` (API gateway that fronts upstream REST services).
2. Rendering is **SSR-only by default** (`--interactivity None`). Interactive Server render mode is permitted only when a page requires post-render user interaction that cannot be achieved via form POST round-trips (e.g., real-time data grids with inline editing). Pages using interactivity must document the justification in a code comment.
3. UI framework is **MudBlazor**: `AddMudServices()` in `Program.cs`; `MudThemeProvider`, `MudSnackbarProvider`, `MudDialogProvider` at the app root (`ThemeProvider.razor`). No Bootstrap or other CSS framework over MudBlazor's token composition.
4. Architecture is clean-layered: **Core** (pure domain + contracts), **Infrastructure** (HTTP, repositories, services), **Presentation** (Components), with MVC **Controllers** for the auth write-path.
5. Authentication is **cookie-based** via server controller endpoints (`/account/login`, `/account/logout`) with antiforgery validation.

## 2) Non-Negotiable Architecture Rules
1. **Core must remain pure** and independent of Web/UI/infrastructure concerns: records in `Core/Domain`, interfaces in `Core/Contracts`, validated options in `Core/Config`, exceptions in `Core/Exceptions`. Zero framework dependencies beyond logging abstractions.
2. **Infrastructure may depend on Core abstractions, never the reverse.** Concrete repositories, typed HTTP clients, delegating handlers, and app services live in `Infrastructure/{Http,Repositories,Services}`.
3. **Presentation composes application behavior but contains no business rules.** Razor components call repository/service interface methods and render results.
4. Do not bypass abstractions by calling external APIs directly from Razor components or controllers — all external calls go through `IErpHttpClient` / `IRepository<T>` implementations.
5. Register all concrete services through DI in `Program.cs` (or a dedicated extension method as registrations grow).
6. **Never create duplicate entity, DTO, or record types outside `Core/Domain`.** All shared types (entities, API response shapes, value objects) are defined once in Core and referenced directly; do not create local copies in any layer.
7. Keep "server state" (data loaded via `IRepository<T>`) separate from "UI state" (`LayoutService`, component fields). Never cache API data in a UI-state service; let output caching handle read-side staleness.

## 3) Dependency Injection & Testability
> **Goal:** as close to 100% testable as possible — every piece of business logic reachable and verifiable in isolation through its interface.
1. **Constructor injection** for controllers, repositories, and services; Razor components use `[Inject]` property injection (framework requirement).
2. Scoped lifetimes for request/user-scoped services; singleton only for stateless/shared services safe across requests; transient for lightweight helpers.
3. In Development, keep DI validation enabled (`ValidateScopes` and `ValidateOnBuild`); validate options eagerly with `.ValidateDataAnnotations().ValidateOnStart()`.
4. No ad hoc service location (`IServiceProvider.GetService`) in application code unless an existing pattern requires it.
5. **Interface-based abstractions** — define an interface in `Core/Contracts` for anything performing I/O or holding volatile logic, with implementations in `Infrastructure`. All business logic lives behind interfaces.
6. **Stateless by default** — pass values as parameters, return results; high cohesion, low coupling; more than 5 constructor dependencies means split the class.
7. Never instantiate dependencies (`new HttpClient()`, `new DbContext()`) inside a class — require them from DI. Use `IHttpClientFactory`-registered typed clients exclusively.

## 4) Blazor SSR and Routing Rules
1. Default all pages/components to SSR and opt in to interactivity only when a clear requirement exists.
2. Pages that require HTTP header writes during request processing (auth POST flows) must use SSR patterns.
3. For auth forms, use POST endpoints with antiforgery tokens (`<AntiforgeryToken />`, `[ValidateAntiForgeryToken]`) rather than interactive event handlers.
4. Keep login/logout flows controller-based to avoid response-header write timing issues; the login page opts out of interactive routing.
5. Use `[Authorize]` on protected pages and enforce deterministic redirects (`RedirectToLogin.razor` → `/login?returnUrl=…`).
6. **Manage async component state by render mode:** Interactive Server components that fetch data implement the Gold Standard 4-branch pattern (Loading → HasError → HasSourceData → Empty). SSR-only pages use the 2-branch approach (HasData / Empty) with error handling via a message variable rendered on the single server pass.
7. Prefer .NET-native mechanisms — Enhanced Navigation, `<Virtualize>`, `EditForm` — over custom JavaScript; JS interop only when no framework-supported alternative exists.

## 5) Authentication and Session Rules
1. Cookie authentication is configured in `Program.cs`: HttpOnly cookies, `SameSite=Strict`, sensible expiration/sliding behavior. Preserve secure defaults and avoid weakening them without explicit approval.
2. Keep login path, logout path, expiration, and sliding expiration coherent with current app policy ("Remember me" extends persistence deliberately).
3. The portal JWT/upstream token is stored as a claim on the cookie and attached to outgoing API calls by the `AuthTokenHandler` delegating handler — never exposed to JavaScript or logs.
4. On upstream `401 Unauthorized`, `AuthTokenHandler` signs the user out and redirects to `/login?error=session_expired`; map error query codes to user-safe messages on the login page.
5. Never expose tokens, passwords, secrets, or sensitive claims in logs.
6. Preserve any privacy acceptance / consent flow end-to-end: login leads through consent before dashboard access; never bypass it when wiring new pages.

## 6) API and Repository Rules
1. External API access goes through typed HTTP clients (`IErpHttpClient`) and repository/service abstractions registered via `IHttpClientFactory`.
2. Use delegating handlers for cross-cutting concerns — one concern per handler: `AuthTokenHandler` (token forwarding + 401 sign-out), `ErrorHandlingHandler` (error translation to `AppException`).
3. Keep error translation centralized: status codes map to coded `AppException`s (`AUTH_401`, `NETWORK_ERROR`, …); callers translate codes to user-safe messages. **Never return raw exception messages to users.**
4. Avoid scattering endpoint URLs — read base URLs and options from `IOptions<T>` classes (`ApiSettings`, `BrandingConfig`) bound and validated at startup.
5. Apply cancellation tokens on async API operations exposed from controller actions and page handlers.
6. **Integration compatibility is mandatory** — when modernizing legacy routes, preserve old endpoint paths and request/response shapes through backward-compatible route aliases so existing integrations keep working unchanged.
7. **No duplicate APIs for legacy compatibility** — add legacy route aliases directly on the existing controller action instead of copy-pasting logic into a parallel endpoint.
8. **No phantom or unreferenced routes** — every route must have a known caller. Use absolute route templates when an action must sit outside its controller's route prefix. Confirm at least one client references a route before adding it.

## 7) Security and Compliance Rules
1. Keep standard security headers active (CSP, X-Content-Type-Options, X-Frame-Options, etc.) and HSTS in production.
2. Keep crawler-shield/meta-robots behavior unless explicitly changed by product/security decision.
3. Preserve antiforgery for all state-changing form actions.
4. Never hardcode secrets, credentials, or environment-sensitive data: `dotnet user-secrets` locally, environment variables (`Section__Key`) in containers/CI, Azure Key Vault (+ Managed Identity) in production.
5. **Critical: `[Authorize]` every data controller and data page** — this is a production blocker.
6. **High: magic byte validation for file uploads** — never trust the client `Content-Type` header.
7. **High: restrict CORS to specific origins** — never `AllowAnyOrigin()` with credentials.
8. **Medium: rate-limit sensitive endpoints** (login, OTP/magic-link) to prevent credential stuffing and mail flooding.
9. **Vet all dependencies before adoption** — verify existence on nuget.org, legitimate publisher, no known CVEs, active maintenance, clear license. Never blindly trust AI-suggested package names.

## 8) UI (MudBlazor)
1. MudBlazor components and theme tokens for layout, spacing, styling; branding is configuration-driven (design-system skill). **Libre Franklin is the only permitted font family** — override MudBlazor's Roboto default on every typography variant; no second family anywhere.
2. Prefer the component API (props, variants, density) and theme variables (`var(--mud-palette-*)`, `var(--brand-*)`) over custom CSS. **No scoped `.razor.css` files and no `<style>` blocks in `.razor` components**; global `wwwroot/app.css` is reserved for baseline/framework-level styles only.
3. **No hardcoded colors anywhere in `.razor` or `.cs` files** — all color values come from `BrandingConfig` or MudBlazor palette variables. This ensures white-labeling and dark-mode compliance.
4. Separate markup and logic for non-trivial components: `.razor` view + `.razor.cs` code-behind holding parameters, lifecycle, handlers.
5. Maintain responsive behavior via `MudGrid`, breakpoint-aware tables (`Breakpoint.Sm`), and drawer variants.
6. Filter UX stays recoverable — render search/filter controls from the *unfiltered* source collection so a zero-match filter can't hide the controls needed to clear it; rely on in-table empty messaging for zero-match filters.
7. Consult MudBlazor docs first (https://mudblazor.com/docs/overview); use built-in theming, size/color enums, variant/density controls before falling back to inline `Style=`.

## 9) Performance, Caching, Testing & Validation
1. Use output caching policies (`AddOutputCache` with tags such as `users`, `todos`) for list/read-heavy endpoints and pages where applicable; invalidate by tag when mutating data affects cached reads. Disable per-environment for debugging rather than deleting policy wiring.
2. Avoid expensive re-renders — keep component state scoped and purposeful; never call `StateHasChanged()` manually except for UI-state service subscriptions (`OnChange += StateHasChanged`) outside normal lifecycle.
3. Build affected projects after changes; resolve compile errors and analyzer warnings introduced by the change (`TreatWarningsAsErrors` is on).
4. Add tests to the xUnit project (`ErpPortal.Tests`) — xUnit + Moq + FluentAssertions; never real HTTP or databases in unit tests.
5. Validate authentication regressions whenever auth code is touched (cookie flows, antiforgery, redirects), and privacy/redirect flows end-to-end when those areas change.

## 10) Logging and Observability
1. Log meaningful operational events (login success/failure, critical exceptions) via `ILogger<T>` with structured message templates — never interpolation.
2. Log intent before acting and outcome after, for every service method performing I/O or mutating state; keep it high-signal — no noisy multi-line blocks.
3. Never log passwords, tokens, or raw personal information.
4. Always give users a descriptive, actionable failure reason: what failed and the next step, without exposing internals (stack traces, `ex.Message`, file paths).

## 11) Change Management & Code Style
1. Minimal, scoped changes; no unrelated refactors; naming/formatting aligned with surrounding code. **Never commit, amend, or push unless explicitly asked.**
2. Prefer explicit typing and return types; avoid `void` for I/O; **never use `var`** — declare the explicit type, including loop variables, LINQ results, and `new` expressions.
3. Prefer `async Task`/`async Task<T>` over `async void`; `async void` only for framework-required event handlers.
4. **Never use `object`/`dynamic` as a declared type** — use the explicit concrete type or a constrained generic. If a payload shape is genuinely open, introduce a typed record.
5. **Enums are non-nullable value types** with an explicit zero member (`None`/`Unknown`) and a meaningful default — applies to records, DTOs, and contracts alike.
6. **Records for data, classes for services** — immutable records for DTOs/domain entities (`with` expressions for changes), classes for DI-registered logic/I/O. Leverage record value equality in test assertions.
7. No nested syntax — flat structures with guard clauses and early returns; avoid unnecessary `if` statements (prefer ternaries, pattern matching, `??`/`??=`).
8. **AI agent compliance check before finalizing changes** touching auth, file uploads, or external network calls — re-check Section 7: publishers legit, deps maintained, no CVEs, no hardcoded secrets, HTTPS/timeouts/input validation on network calls, no internal details in error messages.
9. Apply DRY — extend existing related functionality before creating parallel implementations; reuse compact helpers/constants for repeated error text.
10. Keep code simple; comments are for complex or non-obvious decisions only — explain *why*, never narrate *what*.
11. **Hardcoded mock/placeholder data is temporary scaffolding** — replace with parameterized bindings or clearly marked `// TODO:` stubs returning empty collections before completion.
12. **Code is liability, not an asset** — prefer deleting over adding; pursue the smallest diff that solves the problem.

## 12) Reliability: Power of 10 + The 2 AM Rules
1. Simple control flow, deterministic loops, small functions (≤60 lines), checked return values — never ignore a `Task`.
2. Context-rich exceptions — throw specific types carrying state (`throw new AppException($"Failed to load users for tenant {tenantId}", …)`), never bare `Exception("Failed")`.
3. Fail fast — guard clauses validating nulls, empty strings, invalid ranges at method entry.
4. Idempotency by default; **CancellationToken propagation** through every async path.
5. No "ghost" errors — never empty `catch {}`; if ignoring, log at Information/Debug with a comment explaining why it is safe.
6. Timeouts are mandatory on every external call (HTTP, database, cache).
7. Pure logic separation — business rules as pure functions apart from I/O; trivially unit-testable. No static mutable/global state.
8. Minimal preprocessor use; no `unsafe` without cause; compiler warnings treated as errors.

## 13) Project Layout
```
ErpPortal.Api   ──►  upstream REST services (API gateway; its own Core/Infrastructure folders)

Browser ──SSR──► ErpPortal (Blazor Web App)
                   ├── Core/            Domain records, Contracts, Config options, Exceptions (pure)
                   ├── Infrastructure/  Http (typed clients, handlers), Repositories, Services
                   ├── Components/      Pages, Layout, ThemeProvider, Routes, App.razor
                   └── Controllers/     MVC write-path endpoints (account login/logout)
```
- **Core** — `Domain/` records (`User`, `Todo`), `Contracts/` interfaces (`IRepository<T>`, `IErpHttpClient`, `IAuthService`, `INotificationService`), `Config/` validated options (`ApiSettings`, `BrandingConfig`), `Exceptions/AppException`.
- **Infrastructure** — `Http/AuthTokenHandler`, `Http/ErrorHandlingHandler`, `Http/ErpHttpClient`, `Repositories/*Repository`, `Services/AuthService`, `LayoutService`, `UserProfileService`, notification implementation.
- **Presentation** — `Components/Pages/**` (Dashboard, Login, Users, Tasks), `Components/Layout/**`, `wwwroot/`.
- **Controllers** — thin MVC endpoints for cookie header writes; delegate to services/repositories.
- **Tests** — `ErpPortal.Tests` mirrors Infrastructure/Core structure; mocks injected via constructor.

## 14) Adding a New Feature (Full Checklist)
Quick outline:
1. **Core** — define the domain record in `Core/Domain` (immutable, explicit types) if it doesn't exist; add/extend the contract in `Core/Contracts`.
2. **Infrastructure** — implement a repository behind the contract using `IErpHttpClient`; register `services.AddScoped<IRepository<TEntity>, EntityRepository>()` in `Program.cs`; add an output-cache policy if read-heavy.
3. **Presentation** — build the page under `Components/Pages/<Area>/`; SSR-only pages use the 2-branch pattern, Interactive Server pages the 4-branch Gold Standard pattern (gold-standard-state skill).
4. **Controllers** — only for flows needing header writes or antiforgery form posts; keep thin, `[ValidateAntiForgeryToken]`, redirect with error codes.
5. **Tests** — add xUnit coverage mocking the contract interfaces (`NullLogger<T>` for loggers).
Everything auto-wires through `Program.cs` DI — no manual registration beyond the one `AddScoped` line.

## 15) Package Selection Policy
1. **Microsoft-first** — prefer Microsoft-published packages (`Microsoft.Extensions.*`, `System.*`, framework-built-ins like `<Virtualize>`, Output Caching, QuickGrid) before third-party alternatives.
2. **Verified FOSS only** otherwise: on nuget.org, legitimate publisher, no known CVEs, actively maintained, OSI-approved license.
3. **Implement ourselves** when nothing suitable exists — a small focused helper beats a large dependency for one feature.
4. **Use native capabilities first** — DI, factories, extension methods, `System.Net.Http`, LINQ. Serilog and MudBlazor are the approved exceptions already in the stack.
5. **No AI-suggested package names** — verify independently on nuget.org before adding, and re-apply vetting on every dependency add or upgrade.
