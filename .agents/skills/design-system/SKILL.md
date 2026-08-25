---
name: design-system
description: MudBlazor theming, color conventions, typography, spacing, and component styling rules for the white-label ERP Portal design system. Use when styling components, choosing colors, or applying consistent UI patterns.
---

# Design System: ERP Portal (MudBlazor)

> **Motto:** Move fast. Be clear. Do not overcomplicate.
> **Engineering alignment:** UI components contain no business logic — calculations, validation, and domain branching belong in injected repository/service interfaces. Style tokens come from configuration, never per-component literals (global-rules Sections 3 and 8).
> **Token precedence:** `BrandingConfig` (bound from the `Branding` section of `appsettings.json`, validated at startup) is the **authoritative** source at runtime. The YAML below documents the **shipped defaults** before any deployment override (`Branding__PrimaryColor` style environment variables). Where prose disagrees with config or YAML, config wins.
> **File limit:** this skill, like every skill file, is **capped at 200 lines**. At the cap, fold a new fact into the line it qualifies or split it into a reference file; never append past 200.

## Theme Tokens (Shipped Defaults)

```yaml
name: Enterprise White-Label MudBlazor Admin Dashboard
branding:                      # ← these four come from Core/Config/BrandingConfig.cs
  company-name: Acme Corp ERP  #   Branding:CompanyName
  logo-url: <per-deployment>   #   Branding:LogoUrl   ([Required, Url])
  primary: '#0052cc'           #   Branding:PrimaryColor  — brand actions, app bar
  secondary: '#172b4d'         #   Branding:SecondaryColor — drawer, structural contrast
  accent: '#ffab00'            #   Branding:AccentColor    — highlights, badges
palette:                       # ← MudBlazor PaletteLight defaults; do not diverge ad hoc
  surface: '#FFFFFF'
  background: '#FAFAFA'
  on-surface: '#1C1B1F'
  outline: '#79747E'
  divider: '#E0E0E0'
  error: '#B3261E'
  success: '#2E7D32'
  warning: '#ED6C02'
  info: '#0288D1'
css-variables:                 # synced at document root by Components/Layout/ThemeProvider.razor
  --brand-primary:   Branding:PrimaryColor
  --brand-secondary: Branding:SecondaryColor
  --brand-accent:    Branding:AccentColor
typography:
  font-family: Libre Franklin  # THE ONLY permitted family — see Typography; enforced globally
  h1: { fontSize: 32px, fontWeight: '700', lineHeight: '1.2', letterSpacing: -0.01em }
  h2: { fontSize: 24px, fontWeight: '600', lineHeight: '1.3' }
  h3: { fontSize: 20px, fontWeight: '500', lineHeight: '1.4' }
  body-1: { fontSize: 14px, fontWeight: '400', lineHeight: '1.6' }
  body-2: { fontSize: 12px, fontWeight: '400', lineHeight: '1.6' }
  button: { fontSize: 14px, fontWeight: '600', letterSpacing: 0.05em }
  caption: { fontSize: 12px, fontWeight: '400', letterSpacing: 0.02em }
rounded: { sm: 0.25rem, DEFAULT: 0.5rem, md: 0.75rem, lg: 1rem, xl: 1.5rem, full: 9999px }
spacing: { base: 8px, xs: 4px, sm: 8px, md: 16px, lg: 24px, xl: 40px }
```

## Brand Identity & Visual Language

A generic, high-performance enterprise admin dashboard built for scalability and clarity across any industry, using MudBlazor's structural patterns with configuration-driven corporate branding.

**Corporate Modern** aesthetic: balances a data-heavy dashboard's utilitarian needs with a clean professional look — Material Design principles (depth, clear hierarchy) refined with reduced visual noise via generous whitespace, high-contrast typography, and purposeful color.

**White-label by construction:** every deployment rebrands through one `Branding` config section (company name, logo URL, three colors). Nothing else changes — no page edits, no palette forks. The font family is the single deliberate exception: it is fixed, not an override point.

## Runtime Branding Pipeline (how a color reaches the screen)

1. `appsettings.json` → `"Branding": { CompanyName, LogoUrl, PrimaryColor, SecondaryColor, AccentColor }`.
2. `Core/Config/BrandingConfig.cs` binds it with `.ValidateDataAnnotations().ValidateOnStart()` — a missing required value crashes fast instead of rendering unstyled pages.
3. `Components/Layout/ThemeProvider.razor` injects `IOptions<BrandingConfig>`, builds a `MudTheme` (`PaletteLight`: `Primary`, `Secondary`, `AppbarBackground` = Primary, `DrawerBackground` = Secondary, `DrawerText` = white) and renders `<HeadContent>` with the `--brand-*` CSS custom properties.
4. Components consume either MudBlazor enums (`Color.Primary`) or CSS variables (`var(--brand-primary)`, `var(--mud-palette-*)`) — never raw hex.

Because the `<head>` styles render server-side, third-party components and legacy stylesheets inherit brand colours with **no flash of unstyled content (FOUC)**.

## Color Usage

Centered on the configured **Primary** (`#0052cc` shipped default) — trustworthy, action-oriented blue. Cohesive across long working sessions.

- **Primary** — primary buttons, active states, app bar background, links, progress indicators. **Secondary** (`#172b4d`) — drawer/navigation background, structural contrast, dense operational headers. **Accent** (`#ffab00`) — sparing highlights, badges, attention cues; never a competing action colour.
- **Functional:** Success, Warning, Error, Info map to MudBlazor `Severity`/`Color` enums so snackbars, alerts, and status chips stay predictable.
- **Neutral:** Surface `#FFFFFF` (cards/paper), Background near-white canvas, Divider `#E0E0E0`.
- **Semantic:** Outline for borders/dividers; On-Surface for text; On-Surface-Variant for secondary text/icons.

### Tonal Separation Rules

Use tonal layering to separate dense dashboard areas before reaching for hard borders.

- Base canvas → workspace sections → nested regions → white cards, each step a subtle tone shift.
- 1px borders are secondary support only — accessibility or dense tabular scanning.
- Keep tokens centralized; no one-off per-page color constants anywhere in `.razor` or `.cs`.

## Typography

Font: **Libre Franklin — the only font family permitted in this solution.** No second family anywhere: not for headings, code, numerics, icons-as-text, charts, or emails. It is applied once via the **universal selector in `wwwroot/app.css`** (`* { font-family: 'Libre Franklin', sans-serif; }`) so MudBlazor components and third-party markup all inherit it.

Why not `MudTheme.Typography`? MudBlazor v9's Typography slot class names collide with C# identifiers under this project's `TreatWarningsAsErrors=true` (CS0246). The global CSS baseline is the sanctioned enforcement point — do not reintroduce per-variant font overrides.

Base size 14px (optimized for data-heavy layouts). H1 32px/Bold (page titles) · H2 24px/Semi-bold (section headers) · H3 20px/Medium (card titles) · Body 1 14px/Regular · Body 2 12px/Regular (table content, captions) · Button 14px/Semi-bold (ls 0.05em) · Caption 12px/Regular (labels, metadata). Reference the webfont once in `App.razor`; fallbacks limited to generic CSS families (`sans-serif`).

## Layout, Spacing & Density

12-column fluid grid (`MudGrid`/`MudItem`) on an **8px base unit**, wrapped in `MudContainer MaxWidth.ExtraLarge`. Card padding 16px (md); gutters between major sections 24px (lg); data-heavy tables may drop to 4px (xs) vertical rhythm.

Scale: xs 4px (dense) · sm 8px (minimal) · md 16px (card padding) · lg 24px (gutters) · xl 40px (large sections).

**Density strategy:** desktop-first for workstation efficiency; responsive via `Breakpoint`-aware tables and the responsive drawer variant without collapsing readability.

## Elevation & Shape

Hierarchy comes from ambient shadows and tonal layering — soft diffused shadows, never heavy black.

- **L0 Flat:** page canvas. **L1 Low:** cards, `MudPaper Elevation="2"` panels. **L2 Medium:** hover states. **L3 High:** modals (`MudDialog`), dropdowns.

Rounded shape language: cards and tables 8px; primary buttons 4–6px; form inputs 8px; chips and badges 4px.

## Components

- **App Bar:** Primary brand background, white text/icons, elevation 1–4. `CompanyName` from `BrandingConfig` as the title; menu toggle wired to `LayoutService.ToggleSidebar()`; profile/notifications right.
- **Navigation Drawer:** responsive variant bound to `LayoutService.IsSidebarOpen` (`@bind-Open`); Secondary brand background with light text; auto-closes on navigation (`Nav.LocationChanged`); nav items use `MudNavMenu`/`MudNavLink` with Material icons.
- **Data Tables (`MudTable<T>`):** header bold on a neutral tier; rows separated by thin dividers — no zebra striping unless requested; `RowsPerPage` + `MudTablePager`; `Dense="true"` and `Breakpoint.Sm` for mobile stacking with `DataLabel`s. Status chips: small, rounded, semantic colours on tinted backgrounds.
- **Cards & Surfaces:** `MudPaper` 8px radius; elevation 1–2 or flat with a 1px divider-colour border; padding 24px standard / 16px compact; tone contrast before hard borders.
- **Buttons:** Primary = filled brand Primary, elevation 1 → 2 hover. Secondary = outlined Primary text/border. Destructive = filled Error, always behind a confirmation dialog. Radius 4–6px.
- **Form Fields:** "Outlined" variant; `EditForm` + `DataAnnotationsValidator` for type-safe submission; error states use semantic red for border and helper text; submit buttons disable while `_isSubmitting`.
- **Dialogs & Feedback:** `MudDialog @bind-Visible` for create/delete confirmations (`DialogOptions CloseOnEscapeKey`); notifications go through the injected `INotificationService` (MudSnackbar implementation) — never `ISnackbar` directly in components.
- **Empty/Loading/Error states:** Interactive Server pages follow the 4-branch Gold Standard (`MudProgressCircular` → `MudAlert`+retry → table → `MudAlert` guidance); SSR-only pages render skeleton/alert alternatives in a single pass. See gold-standard-state skill.

## MudBlazor v9 Gotchas (binding for this codebase)

1. Use `MudTable<T>`, not QuickGrid — both libraries export `TemplateColumn` and Razor cannot disambiguate them under shared `_Imports.razor` (RZ9985).
2. `MudAvatar` rejects the `Image` attribute (MUD0002) — use initials on a coloured avatar instead.
3. `@bind-Open` on `MudDrawer` requires a public setter — back `LayoutService.IsSidebarOpen` with a field whose setter fires `OnChange` (CS0272 fix).
4. `MudColor` lives in `MudBlazor.Utilities` — `@using MudBlazor.Utilities` where constructed from config strings.

## Styling Boundaries (Non-Negotiable)

1. **No scoped `.razor.css` files and no `<style>` blocks in `.razor` components.** Exceptions only: global `wwwroot/app.css` (baseline/framework-level styles, including the font selector) and the `:root` variable sync inside `ThemeProvider.razor`.
2. **No hardcoded colors anywhere in `.razor` or `.cs`** — use `BrandingConfig`, MudBlazor `Color`/`Severity` enums, `var(--mud-palette-*)`, or `var(--brand-*)`.
3. Consult MudBlazor docs first (https://mudblazor.com/docs/overview); prefer native props/variants/density/theming before inline `Style=`.
4. Do not layer external CSS frameworks over MudBlazor's token composition.

## Regional Formatting

Locale, time zone, currency, and date format are **configurable per deployment** — never hard-coded in components. Financial values render as localized currency; operational dates share one formatter; due-date urgency states are visually distinct.

## Information Architecture & Guardrails

1. Login screens use the same theme tokens as the authenticated shell — one `ThemeProvider` for all pages.
2. Dashboard content is componentized (`MudGrid` widget cards), not hard-wired into one page block.
3. Grid-heavy views include consistent toolbar affordances: search, reset filters; filter UX stays recoverable (global-rules 8.6).
4. Role-gated sections hide unavailable tabs/actions without breaking layout rhythm.
5. Clarity over decoration; contextual feedback in Primary; uniform radii/padding across buttons, inputs, chips.
6. Treat `docs/design-system.md` and this file as the styling baseline; if references conflict, runtime `BrandingConfig` takes precedence.
