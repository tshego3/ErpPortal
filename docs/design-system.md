---
name: Midnight Emerald MudBlazor Admin Dashboard
colors:
  surface: '#0D1117'
  surface-dim: '#080C0F'
  surface-bright: '#161B22'
  surface-container-lowest: '#050A08'
  surface-container-low: '#0D1117'
  surface-container: '#161B22'
  surface-container-high: '#1C2128'
  surface-container-highest: '#21262D'
  on-surface: '#E6EDF3'
  on-surface-variant: '#8B949E'
  inverse-surface: '#E6EDF3'
  inverse-on-surface: '#0D1117'
  outline: '#30363D'
  outline-variant: '#21262D'
  surface-tint: '#00FF8C'
  primary: '#00FF8C'
  on-primary: '#050A08'
  primary-container: '#1A4D38'
  on-primary-container: '#00FF8C'
  inverse-primary: '#1A4D38'
  secondary: '#1A4D38'
  on-secondary: '#E6EDF3'
  secondary-container: '#0D2818'
  on-secondary-container: '#00FF8C'
  tertiary: '#1A4D38'
  on-tertiary: '#E6EDF3'
  tertiary-container: '#0D2818'
  on-tertiary-container: '#00FF8C'
  error: '#E53935'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#00FF8C'
  primary-fixed-dim: '#00CC70'
  on-primary-fixed: '#050A08'
  on-primary-fixed-variant: '#1A4D38'
  secondary-fixed: '#1A4D38'
  secondary-fixed-dim: '#0D2818'
  on-secondary-fixed: '#E6EDF3'
  on-secondary-fixed-variant: '#1A4D38'
  tertiary-fixed: '#1A4D38'
  tertiary-fixed-dim: '#0D2818'
  on-tertiary-fixed: '#E6EDF3'
  on-tertiary-fixed-variant: '#1A4D38'
  background: '#0D1117'
  on-background: '#E6EDF3'
  surface-variant: '#161B22'
  success: '#00FF8C'
  warning: '#FFB545'
  info: '#58A6FF'
  app-bar: '#050A08'
  drawer-bg: '#050A08'
  divider: '#21262D'
typography:
  h1:
    fontFamily: Public Sans
    fontSize: 32px
    fontWeight: '700'
    lineHeight: '1.2'
    letterSpacing: -0.01em
  h2:
    fontFamily: Public Sans
    fontSize: 24px
    fontWeight: '600'
    lineHeight: '1.3'
  h3:
    fontFamily: Public Sans
    fontSize: 20px
    fontWeight: '500'
    lineHeight: '1.4'
  body-1:
    fontFamily: Public Sans
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.6'
  body-2:
    fontFamily: Public Sans
    fontSize: 12px
    fontWeight: '400'
    lineHeight: '1.6'
  button:
    fontFamily: Public Sans
    fontSize: 14px
    fontWeight: '600'
    letterSpacing: 0.05em
  caption:
    fontFamily: Public Sans
    fontSize: 12px
    fontWeight: '400'
    letterSpacing: 0.02em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 8px
  xs: 4px
  sm: 8px
  md: 16px
  lg: 24px
  xl: 40px
---

# Design System: Midnight Emerald MudBlazor Admin Dashboard

## Brand Identity & Visual Language
A dark-mode-first, high-performance admin dashboard design system built for scalability and clarity across any industry. It utilizes the MudBlazor UI framework's structural patterns with a custom noir-inspired color palette.

The design system is defined by a **Dark Noir** aesthetic that balances the utilitarian requirements of a data-heavy dashboard with a striking neon-on-dark color palette. It is designed to evoke feelings of precision, sophistication, and cutting-edge technology. The visual language adheres to Material Design principles—using depth and clear hierarchy—but refines them with a "Cyber-Tech" approach: reducing visual noise through deep dark surfaces, high-contrast neon typography, and purposeful use of color.

This design system is also **white-label ready** for enterprise scenarios. Brand tokens (logo, primary, accent, surface, and dark-mode defaults) should be resolved from runtime configuration (for example `appsettings.json`) rather than hard-coded into individual pages.

## Color Palette

The palette is centered around **Midnight Emerald**, a piercing neon-tinted green set against deep noir backgrounds. This system is designed for extended use, with high contrast where it matters and restful dark surfaces everywhere else.

### Primary Colors
- **Primary:** `#00FF8C` (A piercing, neon-tinted green that cuts through the dark. Used for primary actions and "neon-sign" highlights.)
- **Primary Container:** `#1A4D38` (A desaturated, murky forest green. Used for secondary actions and subtle, atmospheric accents.)
- **Accent/Deep:** `#050A08` (An "almost-black" with a faint green tint. Used for heavy shadows, backgrounds, and deep noir typography.)

### Functional Colors
- **Success:** `#00FF8C` (Aliased to Primary for brand consistency)
- **Warning:** `#FFB545`
- **Error:** `#E53935`
- **Info:** `#58A6FF`

### Neutral Palette
- **Surface:** `#0D1117` (Card and paper backgrounds — deep dark)
- **Background:** `#0D1117` (Main application background)
- **App Bar:** `#050A08` (The header uses the accent/deep color for noir identity)
- **Drawer/Nav:** `#050A08` (Sidebar background for a deep, immersive feel)
- **Divider:** `#21262D`

### Extended Semantic Colors
- **Outline:** `#30363D` (Border and divider color)
- **On-Surface:** `#E6EDF3` (Text on dark backgrounds)
- **On-Surface-Variant:** `#8B949E` (Secondary text and icons)

### Tokenized Surface Tiers
Use tonal layering to separate dense dashboard areas before reaching for hard borders.

- **Surface (Base Canvas):** `#0D1117`
- **Surface Container Low (Workspace Sections):** `#161B22`
- **Surface Container High (Nested Regions):** `#1C2128`
- **Surface Container Lowest (Cards):** `#050A08`

### Atmospheric Rules
- Prefer **tonal separation first**: section boundaries should primarily come from surface tier changes.
- Use 1px borders as secondary support only where needed for accessibility or dense tabular scanning.
- Keep color tokens centralized and configuration-driven; avoid one-off per-page color constants.

## Typography

**Font Family:** 'Public Sans'
**Base Font Size:** 14px (Optimized for data-heavy dashboard layouts)

The scale prioritizes a clear vertical rhythm. Headlines use tighter letter spacing and heavier weights to establish immediate hierarchy. Body text uses a standard 1rem base with generous line-height to maintain readability in long-form reports. Controls utilize uppercase styling and increased letter spacing to distinguish UI elements from static content.

- **Headings:**
    - H1: 32px / Bold (Page titles) — Line-height: 1.2
    - H2: 24px / Semi-bold (Section headers) — Line-height: 1.3
    - H3: 20px / Medium (Card titles) — Line-height: 1.4
- **Body:**
    - Body 1: 14px / Regular (Standard text) — Line-height: 1.6
    - Body 2: 12px / Regular (Secondary information, table content) — Line-height: 1.6
- **Button:** 14px / Semi-bold (Uppercase or Title Case) — Letter-spacing: 0.05em
- **Caption:** 12px / Regular (Labels and metadata) — Letter-spacing: 0.02em

## Layout & Spacing

This design system utilizes a **12-column fluid grid** system built on an **8px base unit**. 

- **Containers:** All dashboard widgets and cards should snap to the 8px grid.
- **Rhythm:** Internal card padding is set to 16px (md), while the gap between major layout sections (gutters) is set to 24px (lg).
- **Density:** For data-heavy views like the MudDataGrid, the vertical spacing may be reduced to 4px (xs) to allow for more information density without sacrificing clarity.

### Spacing Scale
- **xs:** 4px (Compact spacing for dense layouts)
- **sm:** 8px (Minimal spacing)
- **md:** 16px (Standard padding for cards and containers)
- **lg:** 24px (Gutters and major section gaps)
- **xl:** 40px (Large section spacing)

### Density Strategy
- **Desktop-first:** prioritize workstation efficiency for operations teams.
- **Tablet/mobile:** maintain responsive behavior while preserving information hierarchy.
- **High-density views:** combine compact row spacing with stronger headings and clear action placement.

## Elevation & Depth

Hierarchy is established through **Ambient Shadows** and tonal layering. This system avoids heavy black shadows in favor of soft, diffused shadows tinted with the Accent/Deep (`#050A08`) at very low opacities (5-10%).

- **Level 0 (Flat):** Used for the main background and flat surfaces.
- **Level 1 (Low):** Used for standard dashboard cards and data containers. This provides a subtle "lift" from the background.
- **Level 2 (Medium):** Used for interactive elements on hover (buttons, clickable cards).
- **Level 3 (High):** Reserved for modals, dropdowns, and floating action buttons (FABs).

This approach creates a sense of "physicality" where the most important information or interactive elements appear closest to the user.

## Shapes & Border Radius

The design system employs a **Rounded** shape language to soften the industrial nature of a dashboard. 

- **Cards & Data Grids:** Use a standard 8px (0.5rem) corner radius to match the base spacing unit.
- **Buttons:** Primary buttons follow the standard 4-6px roundedness, while secondary "ghost" buttons may use pill-shape (full rounding) to differentiate action types.
- **Form Inputs:** Input fields should maintain the 8px radius to ensure a consistent, modern look across all interactive surfaces.
- **Small Elements:** Chips and badges use 4px radius for subtler roundedness.

## Components & Elements

### App Bar (Header)
- **Background:** Accent/Deep (`#050A08`)
- **Text/Icons:** Primary (`#00FF8C`)
- **Elevation:** 4 (MudBlazor standard)
- **Features:** Brand logo on left, search bar (optional), theme toggle, notifications, and user profile on right.
- **White-label:** support runtime logo switching (`companyLogoLight` / `companyLogoDark`).

### Navigation Drawer (Sidebar)
- **Width:** 240px
- **Style:** Flat, bordered on the right.
- **Nav Links:**
    - Default: Text color `#8B949E`, Transparent background.
    - Active: Text color Primary (`#00FF8C`), Background color Primary with 10% opacity. Left-border accent optional.

### Data Tables
- **Header:** Dark surface background (`#161B22`), bold typography.
- **Rows:** Alternating "Zebra" striping (optional) or dark background with thin dividers.
- **Density:** 'Compact' or 'Default' depending on data volume.
- **Status Chips:** Small, rounded chips using functional colors with tinted background (e.g., 'Paid' = Green/Primary tint).

### Cards & Surfaces
- **Border Radius:** 8px
- **Elevation:** 1 (Subtle shadow) or 0 with a 1px border (`#21262D`) for a flatter, modern look.
- **Padding:** 24px (standard), 16px (compact).
- **Tone First:** prefer card/background tone contrast before introducing hard borders.

### Buttons
- **Primary:** Filled, Background Primary (`#00FF8C`), Dark text (`#050A08`). Use Elevation 1 and transition to Elevation 2 on hover.
- **Secondary:** Outlined, Border Primary (`#00FF8C`), Text Primary (`#00FF8C`). Transparent background.
- **Accent:** Filled, Background Primary Container (`#1A4D38`), Light text (`#E6EDF3`) (for critical or unique actions).
- **Radius:** 4px to 6px.

### Form Fields
- Use the "Outlined" variant for MudTextField and form inputs.
- The label should use the Primary Container (`#1A4D38`) when focused to provide clear visual feedback.
- Error states must use the semantic red (`#E53935`) for both the border and the helper text.
- Standard input height: 40px; padding: 12px; border-radius: 8px.

### Empty, Loading, and Feedback States
- Every data-heavy page must include explicit empty states.
- Use consistent loading placeholders/skeletons for cards and grid regions.
- Use MudSnackbar and MudAlert for system feedback, aligned to semantic colors.

### Navigation Drawer (Sidebar) Enhanced
- **Background:** Use the Accent/Deep Color (`#050A08`) for the drawer to provide strong structural contrast against surfaces.
- **Active State:** Use a left-side border-indicator (4px width) in Primary (`#00FF8C`) and a subtle background highlight.
- **Text Color:** Light text (`#E6EDF3`) on dark background for contrast.

### Data Grids (MudDataGrid) Enhanced
- **Header:** Use the dark surface background (`#161B22`) with uppercase typography style.
- **Rows:** Avoid alternating "Zebra" striping; use subtle 1px dividers in Neutral color (`#21262D`) instead.
- **Selection:** Use a light tint of Primary Container (`#1A4D38` at 10% opacity) for selected rows.

### Cards Enhanced
- Standard cards must have a dark surface, Elevation 1, and a 1px border using the outline color (`#30363D`) to define edges on dark backgrounds.
- Maintain consistent padding of 24px (lg) for standard cards, 16px (md) for compact layouts.

## MudBlazor Theme Implementation (Runtime Branding)

### Non-Negotiable Requirements
1. Use `MudThemeProvider` with both light and dark `MudTheme` variants.
2. Resolve brand values from configuration (`appsettings.json` or equivalent), not hard-coded literals.
3. Persist and restore theme mode preference using browser storage (`localStorage` or `sessionStorage`).
4. Register `MudThemeProvider` and `MudSnackbarProvider` at the app root (`App.razor` or root layout).
5. Use MudBlazor palette/typography/spacing tokens in components; avoid inline style drift for core surfaces and text.
6. Do not layer external CSS frameworks over MudBlazor core token composition.

### Theme Service Responsibilities
1. Load active tenant/customer branding.
2. Build light/dark `MudTheme` objects from runtime brand inputs.
3. Expose current theme mode and toggle behavior.
4. Persist and restore mode preference.
5. Publish change notifications so shared layout/components refresh consistently.

### Suggested Settings Contract
- `companyLogoLight`: string
- `companyLogoDark`: string
- `primaryColor`: string
- `accentColor`: string
- `surfaceColor`: string
- `isDarkModeDefault`: bool

## Regional Formatting & Data Presentation

Use consistent regional formatting across all modules.

1. Locale: configurable per deployment
2. Time zone: configurable per deployment
3. Currency: configurable per deployment
4. Date format: configurable per deployment

Formatting requirements:
1. Financial values must always render as localized currency.
2. Operational dates (created, due, start) must use one shared formatter.
3. Due-date urgency states (overdue, due today, upcoming) must be visually distinct.

## Information Architecture Patterns

1. Login and privacy-acceptance screens must use the same theme tokens as the authenticated shell.
2. Dashboard content should be tab-driven and componentized, not hard-wired into one page block.
3. Grid-heavy views must include consistent toolbar affordances: search, reset filters, export.
4. Status chips and urgency indicators must map to semantic palette roles (success/warning/error/info).
5. Role-gated sections should hide unavailable tabs/actions without breaking layout rhythm.

## Implementation Guardrails

1. Keep visual tokens centralized and configuration-driven.
2. Reuse this typography scale instead of ad hoc font sizing.
3. Treat this document as the baseline for all new MudBlazor web UI screens.
4. If references conflict, runtime brand configuration and this document take precedence.

## Design Principles
1. **Clarity over Decoration:** Use whitespace and alignment to define structure rather than heavy borders or shadows.
2. **Contextual Feedback:** Use the primary color to indicate progress or successful actions.
3. **Information Density:** Balance whitespace with data accessibility; use MudBlazor's 'Dense' property for tables and lists where necessary.
4. **Consistency:** Elements like buttons, inputs, and chips must have uniform border radii and padding across all screens.
5. **Depth & Hierarchy:** Use elevation levels strategically to guide user attention and establish visual hierarchy.
6. **Color Semantics:** Ensure functional colors (success, warning, error, info) are used consistently across all components for predictable user interactions.
7. **Runtime Theming:** Branding and theme mode behavior must be token-driven and centrally managed.
