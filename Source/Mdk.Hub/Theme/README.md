# MDK Hub Styling System

## Architecture

The styling system uses a strict two-layer architecture:

**Layer 1: Colors** (`Colors.axaml`) define the raw color palette using semantic names that describe what the color represents. Examples: `AccentPrimaryColor`, `SuccessColor`, `TextBodyColor`. These names answer "what is this color?"

**Layer 2: Brushes** (`Brushes.axaml`) define usage-specific brushes that reference colors. The brush name indicates where the brush should be used. Examples: `BackgroundAccentBrush`, `ForegroundAccentBrush`, `BorderAccentBrush`. These names answer "where does this go?"

**Layer 3: Styles** reference brushes, never colors. This separation ensures the entire application can be re-themed by editing only `Colors.axaml`.

## Naming Conventions

### Colors

Color names are semantic and describe the color's purpose in the design system, not how it will be used:
- `AccentPrimaryColor` - the primary accent color for important actions
- `SuccessColor` - indicates successful operations
- `DangerColor` - indicates destructive or error states
- `TextBodyColor` - the standard body text color
- `BackgroundPrimaryColor` - the main window background

State variants add suffixes: `AccentPrimaryHoverColor`, `AccentPrimaryPressedColor`.

### Brushes

Brush names use prefixes that indicate which property type they're designed for:
- `Foreground*Brush` - for text, icons, and any foreground element
- `Background*Brush` - for fills and background properties
- `Border*Brush` - for borders and strokes
- `Overlay*Brush` - for transparent layers and hover effects

The prefix tells you the intended use. A brush like `BackgroundAccentBrush` references `AccentPrimaryColor` but makes it clear this is meant for backgrounds. Meanwhile `ForegroundAccentBrush` might reference `AccentInteractiveColor` (a brighter blue) since text needs different treatment than button backgrounds.

This means the same semantic color can feed multiple brushes with different purposes, and different colors can be used for the same semantic purpose depending on context.

## When to Add New Resources

### Adding Colors

Think twice before adding a new color. The palette is intentionally limited to maintain visual consistency. Add a color only when:

1. **New semantic purpose**: You need to represent a concept not covered by existing colors (warning, info, disabled, etc.)
2. **Accessibility requirement**: Existing colors don't meet contrast requirements for a specific use case
3. **Design system expansion**: Adding a new theme variant (light mode) that needs parallel color definitions

Don't add a color just because you need a slightly different shade for one component. Use the existing semantic colors and let the brush layer handle any necessary adjustments.

### Adding Brushes

Adding brushes is more permissible than adding colors, but still requires justification. Add a brush when:

1. **Usage divergence**: You need the same semantic color for different property types. For example, `AccentPrimaryColor` might be used by both `BackgroundAccentBrush` and `BorderAccentBrush`.

2. **State management**: You need to manage hover/pressed states. Create `BackgroundAccentHoverBrush` and `BackgroundAccentPressedBrush` that reference the hover/pressed color variants.

3. **Semantic clarity**: You want to make it obvious what a brush is for. `BackgroundCardBrush`, `BackgroundCardHoverBrush`, and `BackgroundCardSelectedBrush` all exist even though they reference the same base color with different opacities, because the names communicate intent.

Don't add a brush that's semantically identical to an existing one just to avoid typing. If you find yourself creating `MyComponentBackgroundBrush` that references the same color as `BackgroundPrimaryBrush`, just use `BackgroundPrimaryBrush`.

## The No-Duplication Rule

Each brush should reference a color from `Colors.axaml`. Each style should reference a brush from `Brushes.axaml`. Styles must never reference colors directly, and they must never contain hardcoded color values.

This rule exists to enable complete theme switching. If we decide to add light mode, we should only need to edit `Colors.axaml` and all 10,000+ lines of XAML should adapt automatically. Breaking this rule makes that impossible.

The only files that may contain hardcoded hex color values are:
- `Colors.axaml` - the color definitions themselves
- `BaseStyles.axaml` - FluentTheme overrides that must use inline brushes

Everywhere else uses brush references.

## Component Styles and FluentTheme

Components that override FluentTheme defaults (Button, TextBox, ComboBox, RadioButton, CheckBox) do so in `BaseStyles.axaml` using ThemeDictionaries. These overrides must still follow the same rules: reference colors from `Colors.axaml`, never hardcode.

The system-level `SystemAccentColor` in BaseStyles controls the accent color for native Fluent controls. This should reference the appropriate color from your palette to maintain consistency.

## Common Mistakes

**Using colors directly in styles**: A style that sets `Background="{StaticResource AccentPrimaryColor}"` bypasses the brush layer and breaks the theme switching guarantee. Use `Background="{StaticResource BackgroundAccentBrush}"` instead.

**Hardcoded colors for "just this one thing"**: There's no such thing as a one-off color. Even Easter eggs should use color resources. If light mode ever happens, that hardcoded `#4da6ff` will look wrong and you won't find it.

**Over-specific brush names**: A brush called `ProjectCardTitleSelectedHoverBackgroundBrush` is probably too specific. If it's just a background for a hover state, `BackgroundCardHoverBrush` works fine. Save the specificity for when brushes genuinely diverge.

**Semantic confusion**: Don't use `ForegroundAccentBrush` for a background just because it's the right color. The name carries meaning. If you need that color for a background, create `BackgroundSomethingBrush` or use an existing background brush.

## File Organization

All color and brush definitions live in the `Theme` folder. Component-specific styles also live here and are loaded globally from `App.axaml`. This means:

- `Theme/Colors.axaml` - color palette only
- `Theme/Brushes.axaml` - brush definitions only
- `Theme/BaseStyles.axaml` - FluentTheme overrides and base control styles
- `Theme/SemanticStyles.axaml` - utility styles (.accent, .muted, .danger, etc.)
- `Theme/Typography.axaml` - text styles and typography scale
- `Theme/ProjectOverviewStyles.axaml` - styles specific to project overview
- `Theme/ProjectActionsStyles.axaml` - styles specific to project actions
- etc.

Component views don't include their own styles. Everything is loaded globally, making styles available everywhere and ensuring consistency.

## Philosophy

This system optimizes for future flexibility over present convenience. It's slightly more verbose to maintain the color → brush → style indirection, but it makes the codebase resilient to major design changes. We can experiment with different color schemes, add light mode, or rebrand the entire application by editing a single 70-line file.

That tradeoff is worth it.
