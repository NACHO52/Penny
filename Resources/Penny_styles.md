---
name: Modern Fintech Analytics
colors:
  surface: '#f8f9ff'
  surface-dim: '#cbdbf5'
  surface-bright: '#f8f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#eff4ff'
  surface-container: '#e5eeff'
  surface-container-high: '#dce9ff'
  surface-container-highest: '#d3e4fe'
  on-surface: '#0b1c30'
  on-surface-variant: '#45464d'
  inverse-surface: '#213145'
  inverse-on-surface: '#eaf1ff'
  outline: '#76777d'
  outline-variant: '#c6c6cd'
  surface-tint: '#565e74'
  primary: '#000000'
  on-primary: '#ffffff'
  primary-container: '#131b2e'
  on-primary-container: '#7c839b'
  inverse-primary: '#bec6e0'
  secondary: '#006c4a'
  on-secondary: '#ffffff'
  secondary-container: '#82f5c1'
  on-secondary-container: '#00714e'
  tertiary: '#000000'
  on-tertiary: '#ffffff'
  tertiary-container: '#40000d'
  on-tertiary-container: '#f23d5c'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#dae2fd'
  primary-fixed-dim: '#bec6e0'
  on-primary-fixed: '#131b2e'
  on-primary-fixed-variant: '#3f465c'
  secondary-fixed: '#85f8c4'
  secondary-fixed-dim: '#68dba9'
  on-secondary-fixed: '#002114'
  on-secondary-fixed-variant: '#005137'
  tertiary-fixed: '#ffdadb'
  tertiary-fixed-dim: '#ffb2b7'
  on-tertiary-fixed: '#40000d'
  on-tertiary-fixed-variant: '#92002a'
  background: '#f8f9ff'
  on-background: '#0b1c30'
  surface-variant: '#d3e4fe'
typography:
  display-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 36px
    fontWeight: '700'
    lineHeight: 44px
    letterSpacing: -0.025em
  display-lg-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 28px
    fontWeight: '700'
    lineHeight: 36px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
    letterSpacing: -0.02em
  headline-lg-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 22px
    fontWeight: '600'
    lineHeight: 28px
    letterSpacing: -0.015em
  headline-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
    letterSpacing: -0.015em
  headline-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 24px
    letterSpacing: -0.01em
  body-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
    letterSpacing: -0.005em
  body-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
    letterSpacing: 0em
  body-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
    letterSpacing: 0.005em
  metric-stat:
    fontFamily: Plus Jakarta Sans
    fontSize: 30px
    fontWeight: '700'
    lineHeight: 36px
    letterSpacing: -0.03em
  data-mono:
    fontFamily: JetBrains Mono
    fontSize: 13px
    fontWeight: '500'
    lineHeight: 18px
    letterSpacing: -0.01em
  label-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.02em
  label-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 11px
    fontWeight: '600'
    lineHeight: 14px
    letterSpacing: 0.03em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  gutter: 1.5rem
  gutter-sm: 1rem
  margin: 2rem
  margin-sm: 1rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 1rem
  space-lg: 1.5rem
  space-xl: 2rem
---

## Brand & Style

This design system embodies the precision, clarity, and institutional trust of next-generation modern financial software. Tailored for collaborative finance teams, founders, and controllers, the interface delivers institutional-grade functionality wrapped in an effortless consumer-grade clarity.

The visual direction draws from **Corporate / Modern** principles fused with **Refined Minimalism**:
- **Confidence & Precision:** Crisp line work, meticulous tabular alignment, and structured data density without visual clutter.
- **Calm Authority:** Slate and cool-neutral grounds allow high-intent financial statuses (positive inflow, outflow, review triggers) to communicate instantly.
- **Effortless Density:** High information ratio per square inch, utilizing disciplined typography and layered white surfaces rather than heavy borders or aggressive decorative elements.

## Colors

The palette balances deep architectural slates with laser-focused semantic indicators:

- **Primary (`#0f172a`):** Deep Navy/Slate 900. Anchors key structural layouts (collapsible primary navigation, primary action controls, strong heading text).
- **Secondary (`#059669`):** Emerald 600. Reserved strictly for positive balances, profit deltas, upward momentum, verified statuses, and settlement confirmations.
- **Tertiary / Outflow (`#f43f5e`):** Rose 500. Encodes spend velocity, negative variance, refunds, cancellations, and active alerts.
- **Neutral Accent (`#64748b`):** Slate 500. Governs structural utility copy, metric captions, and inactive indicator states.
- **Warning / Pending Accent (`#f59e0b`):** Amber 500. Used exclusively for uncleared ledger items, approvals awaiting signatures, or rate caps.
- **Surfaces & Borders:** Base app canvas sits on Slate 50 (`#f8fafc`) with active structural workspaces in Slate 100 (`#f1f5f9`). Data containers, modals, and insight cards rest on pure white (`#ffffff`) surrounded by a hairline Slate 200 (`#e2e8f0`) stroke.

## Typography

Typography prioritizes scannability, structural hierarchy, and numeric stability:

- **Primary Typeface:** `Plus Jakarta Sans` delivers a clean, modern aesthetic with geometric balance and humanist readability.
- **Tabular Figures & Metrics:** Every numerical display, balance statement, and data table must enforce `font-feature-settings: "tnum" 1, "cv05" 1, "cv11" 1` to ensure perfect column tracking across real-time price changes.
- **Monospace Data:** `JetBrains Mono` handles secondary metadata, transaction IDs, routing numbers, and inline logs to provide instant technical context.
- **Optical Hierarchy:** Large balances pair bold display weights with tight tracking (`-0.03em`), while metadata uppercase labels require expanded tracking (`0.02em` to `0.03em`) for legibility at small scale.

## Layout & Spacing

The layout is constructed on a disciplined **8pt baseline grid** (with a 4pt sub-grid for micro-components like badges and inputs):

- **Grid Structure:** A 12-column fluid grid system across desktop screens with an integrated, fixed 260px collapsible command sidebar. Gutters hold strictly at `1.5rem` (24px) to retain air between metric cards and analytic charts.
- **Breakpoints & Adaptation:**
  - **Desktop (≥ 1280px):** Multi-pane split views, 4-column KPI rows, full interactive transaction tables with real-time detail panels.
  - **Tablet (768px – 1279px):** Sidebar collapses to an icon rail (64px), 2-column KPI layouts, table views prioritize essential columns with drawer expansions.
  - **Mobile (< 768px):** Margins drop to `1rem` (16px), navigation slides behind a bottom sheet or header drawer, and metric cards convert to single-column swipe carousels or stacked vertical modules.

## Elevation & Depth

Visual hierarchy uses a refined combination of **low-contrast hairlines**, **tonal layering**, and **ambient slate shadows**:

- **Hairline Outlines:** All elevated containers leverage a 1px border using `rgba(226, 232, 240, 0.8)` (Slate 200). Heavy or high-contrast frames are forbidden.
- **Elevation Layers:**
  - **Layer 0 (Canvas):** Base background tinted with Slate 50 (`#f8fafc`).
  - **Layer 1 (Card & Content Blocks):** Pure white surface with an ambient shadow: `box-shadow: 0 1px 3px 0 rgba(15, 23, 42, 0.03), 0 1px 2px -1px rgba(15, 23, 42, 0.02)`.
  - **Layer 2 (Hovered Cards & Flyouts):** Slight uplift: `box-shadow: 0 4px 6px -1px rgba(15, 23, 42, 0.05), 0 2px 4px -2px rgba(15, 23, 42, 0.03)`.
  - **Layer 3 (Modals, Context Menus, Date Pickers):** Elevated floating planes: `box-shadow: 0 20px 25px -5px rgba(15, 23, 42, 0.08), 0 8px 10px -6px rgba(15, 23, 42, 0.04)`.
- **Modals & Overlays:** Underlaid with a soft frosted glass shield using `background-color: rgba(15, 23, 42, 0.4)` paired with `backdrop-filter: blur(4px)`.

## Shapes

The shape architecture delivers an approachable, contemporary finish balanced against institutional stability:

- **Base Radius (0.5rem / 8px):** Applied to form controls, inputs, dropdown selectors, list rows, and contextual action menus.
- **Large Radius (1rem / 16px):** Standard across all analytical dashboards, metric summary cards, financial chart containers, and modal sheets.
- **Extra Large Radius (1.5rem / 24px):** Reserved for multi-tenant onboarding trays, hero balance modules, and floating toolbar docks.
- **Pill Radius (Full `9999px`):** Strict convention for transactional status chips, user avatars, pill badges, and segmented toggles.

## Components

### Buttons
- **Primary:** Background Slate 900 (`#0f172a`), foreground White, `rounded-md` (8px), height 40px (padding 0 16px). Hover state transitions cleanly to `#1e293b`.
- **Secondary / Subtle:** Background White, border 1px solid Slate 200 (`#e2e8f0`), text Slate 700 (`#334155`). Hover introduces a Slate 50 background wash.
- **Destructive:** Borderless or tinted Rose 50 background with text Rose 600. Active states shift to solid Rose 600 with white text.

### Modal Forms

- Show field validation messages in red directly below the affected control. Link the message to that control and focus the first invalid field when submission fails.

### Metric & KPI Cards
- Housed in pure white `rounded-xl` containers bordered by 1px Slate 200.
- Top section holds the small uppercase label in Slate 500 alongside contextual icons or secondary action dots.
- Value displayed in bold `metric-stat` tabular typography with strict currency alignment.
- Trend delta tagged at bottom: a pill badge with `+4.2%` using soft Emerald 50 fill and Emerald 700 text, or `-1.8%` with soft Rose 50 fill and Rose 700 text.

### Data Tables
- Row heights fixed at 52px for high legibility, with a dense 40px variant available for transaction auditing.
- Table header: Slate 50 background, uppercase `label-sm` text in Slate 500, bordered by a bottom-only hairline Slate 200 rule.
- Interactive states: Hovering a row yields a subtle Slate 50/50 wash with an instant fade transition. Currency amounts right-aligned in `data-mono` or tabular Plus Jakarta Sans.

### Chips & Status Tags
- Full pill rounding (`rounded-full`), height 24px, horizontal padding 8px.
- **Success / Inflow:** Emerald 50 background, Emerald 700 text, accompanied by an optional 6px solid Emerald 500 dot.
- **Pending / Scheduled:** Amber 50 background, Amber 700 text.
- **Expense / Action Required:** Rose 50 background, Rose 700 text.
- **Neutral / Draft:** Slate 100 background, Slate 600 text.

### Form Inputs & Selectors
- Height 40px, `rounded-md`, 1px solid Slate 200 border, background White.
- Focused state eliminates harsh outlines in favor of a crisp 1px Slate 900 border and an ambient `0 0 0 2px rgba(15, 23, 42, 0.08)` focus ring.
- Numeric and currency inputs incorporate pinned left adornments (e.g., `$` in Slate 400) locked with vertical rule dividers.

### Collaborative Multi-User Badges
- Overlapping stacked avatar circles (28px) with 2px white borders.
- Real-time viewing indicators: A pulsing 6px Emerald dot pinned to the bottom-right corner of active member avatars currently reviewing the balance sheet or transaction block.
