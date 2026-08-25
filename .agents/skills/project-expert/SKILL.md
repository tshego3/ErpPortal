---
name: project-expert
description: Top 5 non-negotiable engineering rules, clean-layered architecture (Core/Infrastructure/Presentation), and delivery skills for the ERP Portal. Use when planning, reviewing, or starting any feature work in this repo.
---

# ERP Portal Agent Skills (.NET 10 Blazor SSR)

What an AI coding agent is expected to do well in this repository. Motto: move fast, be clear, do not overcomplicate.

> **Stack:** `ErpPortal` is a .NET 10 Blazor Web App — **SSR-only by default**, Interactive Server opt-in — with a clean-layered split (`Core`, `Infrastructure`, `Components`, `Controllers`) plus an `ErpPortal.Api` gateway fronting upstream REST services. UI is MudBlazor; auth is cookie-based via controller POST endpoints.
> **Rule priority:** the Top 5 are **MUST** (violations are blockers). Section rules are **SHOULD** — follow unless a documented exception applies. Inline "prefer"/"consider" wording is **MAY** — use judgment.

## Top 5 Non-Negotiables
Highest precedence for any feature request:
1. **SSR-first rendering discipline** — pages default to Static SSR with no `@rendermode`. Interactive Server only when real-time interaction cannot be achieved via form POST round-trips, with the justification documented in a code comment. Every Interactive Server component that loads data asynchronously renders exactly 4 states in order: Loading → Error → Data → Empty; SSR-only pages use the 2-branch pattern (HasData / Empty).
2. **`[Authorize]` on all data controllers and data pages** — no exceptions; this is a production blocker.
3. **Interface encapsulation of all business logic** — UI components call contract methods only (`Core/Contracts`); no business logic in Razor components or controllers; Infrastructure implements contracts, never the reverse. Violations are architectural blockers.
4. **100% testability goal** — every service/repository method reachable and verifiable in isolation through its interface. Design for it from the start.
5. **Domain type ownership** — all entity/DTO/record types live in `Core/Domain`; no duplicates in any other layer.

Everything below elaborates on these.

## Communication Style (Output to User)
**Use the `dev-concise` output style** (`.agents/output-styles/dev-concise.md`) — it is this repo's default and the authoritative version of the rules below. It defines the three-part report (**Done** with file:line refs · **Works?** Yes/No/Not tested plus the real command output · **Next** 1–3 items or "Nothing"), a 4-line answer ceiling unless detail is requested, ASD-STE100 plain English, and "act then report" rather than asking permission for reversible in-scope work. Crucially it also fixes what must never be shortened: errors, failures, skipped steps, partial work, assumptions, costly or irreversible actions, and uncertainty. Short success is good; short bad news is not. Summary:
1. Lead with the result, decision, or answer — not the reasoning that led to it.
2. Do not over-explain: no restating the request, narrating obvious steps, or padding with preamble/closing summaries.
3. **Never omit information the user needs** — file paths, line numbers, root causes, trade-offs, and caveats stay; only the wording around them gets trimmed.
4. Prefer short sentences and plain language over hedged, verbose, or repetitive phrasing.
5. Match length to the question: one line for a one-line question; expand only when the task requires it (multi-file changes, security implications).

## Architecture Overview
```
ErpPortal.Api (API gateway — fronts upstream REST services)
    ▲ HTTP via typed IErpHttpClient + delegating handlers
    │
ErpPortal (Blazor Web App)
    ├── Components/      Presentation — SSR pages (+ opt-in Interactive Server), MudBlazor UI
    ├── Controllers/     MVC write-path endpoints (account login/logout, antiforgery)
    ├── Infrastructure/  Repositories, typed Http clients + DelegatingHandlers, app services
    └── Core/            Domain records, Contracts, Config options, Exceptions — zero framework deps

Tests (ErpPortal.Tests — xUnit + Moq + FluentAssertions) — mock Core contracts
```
- **Golden rule:** never bypass a layer. Business logic stays behind Core contracts implemented in Infrastructure; UI holds no logic; components never construct HTTP clients themselves.
- **Domain type rule:** never create an entity/DTO/record outside `Core/Domain`. All shared types are defined once in Core and referenced directly from every layer.
- **Server state vs UI state:** server data flows through `IRepository<T>` (cacheable via output-cache policies); transient UI state lives in scoped services like `LayoutService` with an `OnChange` event. Never mix them.

## Core Delivery Skills
1. Implement full vertical features across Core, Infrastructure, Components/Controllers, and tests.
2. Work with Blazor Web App SSR (default) and Interactive Server components (justified features only) without breaking auth, antiforgery, or Enhanced Navigation.
3. Use dependency injection consistently through `Program.cs` registrations and constructor injection; `[Inject]` in components as the framework requires.
4. Build reliable HTTP integrations with the typed `IErpHttpClient`, delegating handlers (`AuthTokenHandler`, `ErrorHandlingHandler`), and centralized `AppException` error translation.
5. Preserve boundaries between business logic, infrastructure, and UI — UI calls interface methods only.
6. Apply the design system consistently with MudBlazor and configuration-driven branding (`BrandingConfig`).
7. Run a mandatory compliance pass before completion: confirm changes align with the **global-rules** skill and other `.agents/skills/` guidance.

## Blazor SSR Interaction Skills
1. Distinguish SSR-only pages from interactive pages and choose the right rendering mode (and the matching data pattern — see gold-standard-state).
2. Use controller POST endpoints for cookie sign-in/sign-out where headers must be written, with `<AntiforgeryToken />` / `[ValidateAntiForgeryToken]` on all state-changing form posts.
3. Handle route authorization with `[Authorize]` and deterministic redirects (`RedirectToLogin.razor`, `/login?returnUrl=…`); map login error query codes to user-safe messages.
4. Avoid introducing interactive behavior where server-rendered behavior suffices; prefer Enhanced Navigation, `<Virtualize>`, and `EditForm` over custom JavaScript.

## Authentication and Security Skills
1. Implement cookie-based authentication flows with explicit controller login/logout endpoints and antiforgery on every form post.
2. Preserve secure defaults: HttpOnly cookies, `SameSite=Strict`, HSTS in production, secure headers, crawler shield.
3. Keep privacy-consent workflows (login → privacy → dashboard) intact when changing middleware — no bypass paths.
4. Ensure protected pages are inaccessible to anonymous users and redirect safely to login.
5. Keep the upstream access token out of reach of JavaScript and logs; let `AuthTokenHandler` attach it server-side and sign the user out cleanly on upstream `401`.

## Data and Domain Skills
1. Add and evolve immutable domain records in `Core/Domain` without leaking infrastructure concerns into them.
2. Extend repository implementations behind their contracts while preserving existing service behavior.
3. Keep DTO mapping and external API access isolated in Infrastructure — never in controllers or Razor components.
4. Use output caching intentionally (`UsersList`, `TodosList` tag policies) and invalidate by tag when cached reads become stale.

## UI and Design System Skills
1. Implement layouts that feel premium and enterprise-grade, not generic dashboard boilerplate.
2. Follow the no-line sectioning rule: tonal surfaces over 1px border-heavy composition.
3. Keep typography aligned with the **Libre Franklin** hierarchy and high-density data readability. Libre Franklin is the *only* permitted font family — override MudBlazor's Roboto default on every typography variant; never treat the typeface as a white-label override point.
4. Read brand colors from `BrandingConfig`; no hardcoded colors in `.razor`/`.cs` — use config-bound branding and MudBlazor theme variables (`var(--mud-palette-*)`, `var(--brand-*)`).
5. Do not use scoped `.razor.css` or `<style>` blocks for styling; prefer the MudBlazor component API and global baseline styles.
6. Consult MudBlazor docs first (https://mudblazor.com/docs/overview); prefer native props/variants/density/theming before custom style logic.
7. Separate view and logic with `.razor` + `.razor.cs` code-behind, and keep control flow flat (guard clauses, early returns).

Full token, typography, spacing, elevation, and component specs: the **design-system** skill.

## Quality and Maintenance Skills
1. Build after meaningful changes; resolve compiler/analyzer errors before finishing and treat warnings as errors.
2. Add or update tests when behavior changes — especially auth flow, controllers, and repository behavior.
3. Keep edits minimal, scoped, and style-consistent with nearby code; avoid unrelated refactors. **Never auto-commit** — finish the work, report what changed, and leave staging and committing to the user.
4. **Never use `var`** — always declare the explicit type, including loop variables, LINQ results, and `new` expressions.
5. **Never use `object`/`dynamic` as a declared type** — use the explicit concrete type or a constrained generic.
6. **Enums are always non-nullable and never null** — value types with an explicit zero member so defaults are intentional.
7. Prefer `async Task`/`async Task<T>` over `async void`; `async void` only for framework-required event handlers.
8. Use generics for reusable, type-safe abstractions instead of duplicated type-specific implementations.
9. Apply DRY — extend existing related functionality before introducing duplicate paths.
10. Surface failures with a descriptive, user-safe reason and a clear next step (retry, refresh, contact support) instead of generic error text.
11. **Comments are for complex code only** — explain *why* a non-obvious choice was made, nothing else. If code is hard to understand without comments, simplify first.
12. Prefer low-boilerplate implementations: small methods, small diffs, direct control flow.
13. **Log pre- and post-action events** for every service method that performs I/O or mutates state: intent before, outcome after.

## Reliability, Testability, and Data Modeling Skills
1. Follow NASA JPL Power-of-10 constraints: simple control flow, deterministic loops, bounded function size (≤60 lines), explicit return-value handling.
2. Apply the 2 AM rules: context-rich exceptions, defensive structured logging, guard clauses, idempotency, mandatory timeouts, CancellationToken propagation, no empty catch blocks.
3. Prefer constructor injection for services/handlers; no service locator (`IServiceProvider`) in application code; Blazor components use `[Inject]`.
4. Favor composition over inheritance; seal classes by default unless extensibility is explicitly required.
5. Records for DTOs/events/value objects; classes for DI services and I/O logic; keep records immutable with `with` expressions and leverage value equality in tests.
6. Use test builders/AutoFixture patterns for resilient tests and stable constructor evolution.
7. For immutable records used by forms, define default/empty construction on the record itself (e.g. `CreateEmpty()`) and consume that factory from UI pages instead of page-level model builders.

## Typical Skill Applications
1. Implement login + privacy consent + conditional logout flow end-to-end.
2. Add dashboard widgets and list pages using existing repository/service patterns.
3. Introduce new domain entities and wire them through repositories, DI, and UI layers.
4. Apply white-label design updates from `appsettings.json` and design token guidance.

## Security & Compliance in Development
Check global-rules Section 7 for the full list — **Critical:** auth attributes, secrets management (user-secrets → Key Vault), CORS, file uploads. **High:** HttpClient pooling, rate limiting, secure headers, magic-byte upload validation. **Medium:** caching strategy, ownership checks.

Key reminders when coding:
1. All data controllers **must** have `[Authorize]`.
2. File uploads validate magic bytes, not the `Content-Type` header.
3. Use `IHttpClientFactory`-registered typed clients — never `new HttpClient()`.
4. Secrets never in `appsettings.json` — user-secrets locally, environment variables in CI/containers, Key Vault in production.
5. Error responses never leak internals — translate through `AppException` codes to user-safe messages.

## Project Knowledge & Agent Memory (Portable)

**Rule: this repo has no private agent memory.** An AI agent's per-machine memory store is local to one developer's machine — it is not in git, so it never reaches teammates, CI, or a fresh clone. Knowledge kept only there is silently lost the moment anyone else does the work.

**Therefore: every durable fact an agent would otherwise save to memory MUST be written into these committed `.agents/skills/` files instead.** A teammate then gets the full working context from `git pull` — no export, no sync step, no per-machine setup.

**What counts as durable** — record it: engineering decisions and their rationale; approved exceptions to a rule; user corrections and confirmed approaches ("prefer X over Y", "always do Z here"); conventions not derivable from the code; external references (dashboards, tickets, integration docs) the team needs.

**What does not** — leave it out: anything the repo already states (code structure, git history, past fixes); one-conversation scratch context; machine-specific values (local paths, personal credentials, secrets).

**Where each fact goes:**

| Fact type | Destination |
|---|---|
| Mandatory engineering rule, architecture decision, security/package policy, approved rule exception | **global-rules** |
| Working agreement, delivery convention, agent behavior | **project-expert** (this file) |
| Color, typography, spacing, component or branding decision | **design-system** |
| Code pattern, state-management template, testability guidance | **gold-standard-state** |

**How to record:** amend the *existing* rule the fact modifies rather than appending a parallel note — one statement per fact, no duplication across files. State the decision and its "why" in the same breath. Cross-reference sibling skills by name instead of restating their content.

**Hard limit: no skill file exceeds 200 lines.** This is a cap, not a target. Check the line count before and after editing. At the cap: fold the new fact into the line it qualifies, replace a rule the fact supersedes, or split genuinely separate material into its own reference file that stays under 200 itself. A skill nobody finishes reading changes no behavior, which is the whole reason for the cap.

**Applies to agents and humans alike:** when a decision is made in conversation and not written here, it did not happen.

## Reference Documentation
| Skill | Purpose |
|---|---|
| **global-rules** | Core engineering rules, architecture, feature checklist, security |
| **gold-standard-state** | Practical code templates, state management (4-branch + SSR 2-branch), testing |
| **design-system** | MudBlazor styling conventions and design tokens |
| **project-expert** (this file) | Top 5 non-negotiables, agent capabilities, delivery skills |

Fast onboarding: read `README.md` for setup and stack decisions → global-rules Sections 1–2 and 13–14 for platform identity, architecture, and the feature checklist → use gold-standard-state templates as copy-paste starting points → re-check global-rules Section 7 whenever touching auth, data, or file handling. These four skill files are the whole shared *rule* context — a `git pull` is the only onboarding step, and anything you decide goes back into them ("Project Knowledge & Agent Memory" above).
