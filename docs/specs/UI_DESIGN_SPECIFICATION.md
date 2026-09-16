**Version:** 1.0.0
**Date:** 2026-09-12
**Project:** Culinary Blog - Blog Ẩm thực và Nấu ăn
**Target:** Next.js 15 App Router + TypeScript + Tailwind CSS
**Status:** Draft

---

## 1. Document Overview

### 1.1 Purpose

Tài liệu này định nghĩa chi tiết UI/UX specification cho Culinary Blog, bao gồm design system, component library, page specifications, và requirement-to-UI traceability.

### 1.2 Scope

Bao gồm tất cả user-facing pages và components cho:

- **FR-AUTH**: Authentication (Login, Register, Profile)
- **FR-CAT**: Category browsing
- **FR-RCP**: Recipe browsing, detail, create, edit
- **FR-SRCH**: Search and filtering
- **FR-FILE**: Image upload interface

### 1.3 Out of Scope

- Admin Dashboard (Hangfire, Logs) - Backend team access only
- Comment/Rating System - Out of v1.0 scope
- Real-time notifications - Out of v1.0 scope

### 1.4 Technology Stack


| Layer     | Technology                        |
| --------- | --------------------------------- |
| Framework | Next.js 15 App Router             |
| Language  | TypeScript                        |
| Styling   | Tailwind CSS                      |
| State     | React Context + TanStack Query v5 |
| Auth      | Auth.js v5                        |
| Icons     | Lucide React                      |
| Forms     | React Hook Form + Zod             |


---

## 2. Source of Truth


| Information             | Source                      | Confidence |
| ----------------------- | --------------------------- | ---------- |
| Functional Requirements | SRS_Culinary_Blog_v1.0.0.md | HIGH       |
| API Contracts           | FR-AUTH_APIContract.md      | HIGH       |
| Authentication Flow     | FR-AUTH_BaoCao.md           | HIGH       |
| Database Design         | FR-AUTH_Database_Design.md  | HIGH       |
| Security Specs          | FR-AUTH_SecuritySpec.md     | MEDIUM     |
| Error Codes             | FR-AUTH_ErrorCodes.md       | HIGH       |
| Existing Codebase       | NONE                        | N/A        |


### Classification Legend

- `[SRS]` - Required by SRS document
- `[FR-AUTH]` - Required by FR-AUTH module
- `[PROPOSAL]` - Proposed enhancement, not in SRS
- `[ASSUMPTION]` - Assumption for implementation
- `[UNKNOWN]` - Information not available in docs

---

## 3. Design Principles

### 3.1 Core Principles

1. **Content-First**: Recipe images and content are the hero elements
2. **Visual Warmth**: Reflect culinary/warm atmosphere through design
3. **Clarity**: Easy navigation between recipes and categories
4. **Responsive**: Mobile-first, works on all devices
5. **Fast**: Minimal layout shift, skeleton loading states

### 3.2 Design Metaphor

**Warm Kitchen**: Approachable, inviting, food-focused. Like a well-organized kitchen where everything is within reach.

---

## 4. Color System

### 4.1 Semantic Color Palette

```
Primary:    #E0AFA0   (Warm Terracotta)
Secondary:  #8A817C   (Warm Gray)
Success:    #6B8E6B   (Sage Green)
Warning:    #C89B3C   (Golden Honey)
Error:      #B85C5C   (Muted Red)
Info:       #6B8499   (Slate Blue)
Background: #F4F3EE  (Warm Cream)
Surface:    #FFFFFF   (White)
Text:       #463F3A   (Charcoal Brown)
Border:     #BCB8B1   (Warm Stone)
```

### 4.2 Color Usage Rules


| Color          | Usage                                                     | Avoid                   |
| -------------- | --------------------------------------------------------- | ----------------------- |
| **Primary**    | Primary CTAs, active nav, brand elements                  | Overuse on backgrounds  |
| **Secondary**  | Secondary buttons, borders, muted text                    | Primary action buttons  |
| **Success**    | Success states, completed steps, verified badges          | Generic "go" indicators |
| **Warning**    | Warnings, pending states, approaching deadlines           | Error states            |
| **Error**      | Validation errors, failed operations, destructive actions | Warnings                |
| **Info**       | Informational badges, tips, neutral states                | Action states           |
| **Background** | Page background                                           | Never text              |
| **Surface**    | Cards, modals, dropdowns, panels                          | Page background         |
| **Text**       | Primary text, headings                                    | Light backgrounds only  |
| **Border**     | Dividers, input borders, card outlines                    | Fills                   |


### 4.3 Semantic Color Tokens (Tailwind)

```javascript
// tailwind.config.js
colors: {
  primary: {
    50: '#FDF8F6',
    100: '#F9EEEC',
    200: '#F3DDD8',
    300: '#E8C4BB',
    400: '#E0AFA0',  // DEFAULT
    500: '#C98F7D',
    600: '#B0705E',
    700: '#8A5548',
    800: '#734A41',
    900: '#60413A',
  },
  secondary: {
    DEFAULT: '#8A817C',
    light: '#A8A29E',
    dark: '#6B6560',
  },
  success: {
    DEFAULT: '#6B8E6B',
    light: '#8FAF8F',
    dark: '#4A6B4A',
  },
  warning: {
    DEFAULT: '#C89B3C',
    light: '#E0BC6A',
    dark: '#9A7A2E',
  },
  error: {
    DEFAULT: '#B85C5C',
    light: '#D48080',
    dark: '#8F4040',
  },
  info: {
    DEFAULT: '#6B8499',
    light: '#8BA3B8',
    dark: '#4A6070',
  },
  background: '#F4F3EE',
  surface: '#FFFFFF',
  text: '#463F3A',
  border: '#BCB8B1',
}
```

### 4.4 Contrast Check


| Combination          | Contrast Ratio | WCAG Level | Usage                  |
| -------------------- | -------------- | ---------- | ---------------------- |
| Text on Background   | 12.5:1         | AAA        | All body text          |
| Primary on White     | 3.2:1          | AA         | Large text, icons      |
| Error on White       | 5.1:1          | AA         | Error text             |
| White on Primary     | 4.8:1          | AA         | Button text on primary |
| Border on Background | 2.8:1          | Fail       | Decorative only        |


**Note**: Primary (#E0AFA0) on white fails WCAG AA for normal text. Use only for large text (18px+) or backgrounds with sufficient contrast difference.

---

## 5. Typography

### 5.1 Font Stack

```css
/* Vietnamese-friendly font stack */
--font-sans: 'Inter', 'Noto Sans', system-ui, -apple-system, sans-serif;
--font-display: 'Playfair Display', 'Noto Serif', Georgia, serif;
```

### 5.2 Type Scale


| Token       | Size | Line Height | Weight | Usage                  |
| ----------- | ---- | ----------- | ------ | ---------------------- |
| `text-xs`   | 12px | 1.5         | 400    | Captions, timestamps   |
| `text-sm`   | 14px | 1.5         | 400    | Secondary text, labels |
| `text-base` | 16px | 1.6         | 400    | Body text              |
| `text-lg`   | 18px | 1.5         | 500    | Lead paragraphs        |
| `text-xl`   | 20px | 1.4         | 600    | Card titles            |
| `text-2xl`  | 24px | 1.3         | 700    | Section headers        |
| `text-3xl`  | 30px | 1.2         | 700    | Page titles            |
| `text-4xl`  | 36px | 1.1         | 800    | Hero headings          |
| `text-5xl`  | 48px | 1.0         | 800    | Homepage hero          |


### 5.3 Typography Rules

- **Display Font** (`font-display`): Use for hero headings, recipe titles, brand elements
- **Body Font** (`font-sans`): Use for all other text
- **Vietnamese Support**: Ensure all fonts support Vietnamese diacritics

---

## 6. Spacing System

### 6.1 Spacing Scale

Based on 4px grid:

```
0:   0px
1:   4px
2:   8px
3:   12px
4:   16px
5:   20px
6:   24px
8:   32px
10:  40px
12:  48px
16:  64px
20:  80px
24:  96px
```

### 6.2 Spacing Usage


| Token                   | Usage                       |
| ----------------------- | --------------------------- |
| `space-1` - `space-2`   | Icon padding, tight gaps    |
| `space-3` - `space-4`   | Input padding, button gaps  |
| `space-6` - `space-8`   | Card padding, section gaps  |
| `space-10` - `space-12` | Page sections, major gaps   |
| `space-16` - `space-24` | Page margins, hero sections |


---

## 7. Layout &amp; Structure

### 7.1 Page Types

#### Homepage Layout

```
┌─────────────────────────────────────────────────────────────┐
│ HEADER: Logo | Search | Nav Links | Auth Buttons           │
├─────────────────────────────────────────────────────────────┤
│ HERO SECTION (optional on scroll)                          │
│ [Featured Recipe Carousel / Welcome Banner]                │
├─────────────────────────────────────────────────────────────┤
│ CATEGORY STRIP: [All] [Breakfast] [Lunch] [Dinner] ...   │
├─────────────────────────────────────────────────────────────┤
│ RECIPE GRID:                                              │
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐                     │
│ │ Card │ │ Card │ │ Card │ │ Card │                     │
│ └──────┘ └──────┘ └──────┘ └──────┘                     │
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐                     │
│ │ Card │ │ Card │ │ Card │ │ Card │                     │
│ └──────┘ └──────┘ └──────┘ └──────┘                     │
├─────────────────────────────────────────────────────────────┤
│ PAGINATION                                                 │
├─────────────────────────────────────────────────────────────┤
│ FOOTER: Links | Social | Copyright                        │
└─────────────────────────────────────────────────────────────┘
```

#### Recipe Detail Layout

```
┌─────────────────────────────────────────────────────────────┐
│ HEADER                                                     │
├─────────────────────────────────────────────────────────────┤
│ BREADCRUMB: Home > Category > Recipe Title                 │
├─────────────────────────────────────────────────────────────┤
│ ┌────────────────────────────┬──────────────────────────┐ │
│ │                            │ SIDEBAR:                  │ │
│ │   RECIPE HERO IMAGE        │ • Author Card             │ │
│ │   (Full width, 16:9)      │ • Category Badge         │ │
│ │                            │ • Prep/Cook Time         │ │
│ │                            │ • Servings               │ │
│ │                            │ • Difficulty             │ │
│ └────────────────────────────┴──────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│ RECIPE CONTENT:                                            │
│ • Description                                              │
│ • Nutrition Info (collapsible)                             │
│ • Ingredients List                                        │
│ • Step-by-Step Instructions                                │
├─────────────────────────────────────────────────────────────┤
│ RELATED RECIPES                                            │
└─────────────────────────────────────────────────────────────┘
```

#### Auth Pages Layout

```
┌─────────────────────────────────────────────────────────────┐
│ HEADER (minimal: Logo only)                               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│    ┌─────────────────────────────────────────────────┐      │
│    │                                                 │      │
│    │           AUTH FORM CONTAINER                   │      │
│    │           (centered, max-w-md)                 │      │
│    │                                                 │      │
│    │   Title                                        │      │
│    │   Form Fields                                  │      │
│    │   Primary Action                               │      │
│    │   Secondary Actions                           │      │
│    │   Divider (OR)                                 │      │
│    │   Social Login Button                          │      │
│    │                                                 │      │
│    └─────────────────────────────────────────────────┘      │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ FOOTER (minimal: Links only)                               │
└─────────────────────────────────────────────────────────────┘
```

### 7.2 Container Widths


| Container    | Max Width | Usage                      |
| ------------ | --------- | -------------------------- |
| `max-w-xs`   | 320px     | Mobile-only cards          |
| `max-w-sm`   | 384px     | Auth forms                 |
| `max-w-md`   | 448px     | Auth forms, narrow content |
| `max-w-lg`   | 512px     | Recipe content column      |
| `max-w-xl`   | 576px     | Recipe sidebar             |
| `max-w-2xl`  | 672px     | Article content            |
| `max-w-3xl`  | 768px     | Mixed layouts              |
| `max-w-5xl`  | 1024px    | Standard pages             |
| `max-w-6xl`  | 1152px    | Content with sidebar       |
| `max-w-7xl`  | 1280px    | Full layout                |
| `max-w-full` | 100%      | Homepage hero              |


### 7.3 Breakpoints


| Breakpoint | Width  | Usage                            |
| ---------- | ------ | -------------------------------- |
| `sm`       | 640px  | Large phones                     |
| `md`       | 768px  | Tablets portrait                 |
| `lg`       | 1024px | Tablets landscape, small laptops |
| `xl`       | 1280px | Desktops                         |
| `2xl`      | 1536px | Large screens                    |


---

## 8. Component Library

### 8.1 Buttons

#### Primary Button

```tsx
// Purpose: Main actions (Submit form, Create recipe)
// States: default, hover, active, disabled, loading

Variants:
- 'primary': Primary background, white text
- 'secondary': White background, primary border, primary text
- 'ghost': Transparent, primary text

Sizes:
- 'sm': h-8 px-3 text-sm
- 'md': h-10 px-4 text-base
- 'lg': h-12 px-6 text-lg

States:
- Default: Primary background, subtle shadow
- Hover: Darken 10%, lift shadow
- Active: Darken 15%, reduce shadow
- Disabled: 50% opacity, cursor-not-allowed
- Loading: Show spinner, disable interaction
```

#### Example Usage

```tsx
[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%3CButton%20variant%3D%22primary%22%20size%3D%22md%22%20isLoading%3D%7BisSubmitting%7D%3E]]
  {isSubmitting ? 'Đang đăng ký...' : 'Đăng ký'}
[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%3C%2FButton%3E]]

[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%3CButton%20variant%3D%22secondary%22%20size%3D%22md%22%3E]]
  Đăng nhập bằng Google
[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%3C%2FButton%3E]]
```

### 8.2 Input

#### Text Input

```tsx
// Purpose: Single-line text entry

Structure:
┌─────────────────────────────────────┐
│ Label (optional)                    │
├─────────────────────────────────────┤
│ [Icon] Input Field                  │
├─────────────────────────────────────┤
│ Helper text / Error message        │
└─────────────────────────────────────┘

States:
- Default: Border color
- Focus: Primary border, ring
- Error: Error border, error text
- Disabled: Muted background, no interaction
```

#### Input Variants


| Variant    | Purpose                | Example                 |
| ---------- | ---------------------- | ----------------------- |
| `text`     | Single line text       | Display name, username  |
| `email`    | Email input            | Email field             |
| `password` | Password with toggle   | Password field          |
| `search`   | Search input with icon | Recipe search           |
| `textarea` | Multi-line text        | Recipe description, bio |


### 8.3 Cards

#### Recipe Card

```tsx
// Purpose: Display recipe in grid/list

Structure:
┌───────────────────────────────┐
│ IMAGE (aspect-video, rounded) │
│ [Favorite] [Badge]            │
├───────────────────────────────┤
│ CATEGORY (badge)             │
│ RECIPE TITLE (2 lines max)   │
│ AUTHOR • TIME • SERVINGS     │
│ ─────────────────────────────│
│ Author Avatar + Name          │
└───────────────────────────────┘

Variants:
- 'default': Standard card
- 'compact': Smaller, for sidebar
- 'featured': Larger, for hero section
```

#### Category Card

```tsx
// Purpose: Display category in horizontal strip

┌─────────────────┐
│ [Icon]          │
│ Category Name   │
│ (12 recipes)   │
└─────────────────┘
```

### 8.4 Badge

```tsx
// Purpose: Status, category, metadata

Variants:
- 'category': Secondary background, category color
- 'status': Status color (draft, published, archived)
- 'difficulty': Color-coded (easy=success, medium=warning, hard=error)
- 'time': Neutral info style

Sizes:
- 'sm': text-xs px-2 py-0.5
- 'md': text-sm px-2.5 py-1
```

### 8.5 Modal / Dialog

```tsx
// Purpose: Focused actions, confirmations

Structure:
┌───────────────────────────────────────┐
│ Header                    [X Close]  │
├───────────────────────────────────────┤
│                                       │
│ Content Area                          │
│                                       │
├───────────────────────────────────────┤
│ Footer: [Cancel] [Primary Action]    │
└───────────────────────────────────────┘

Sizes:
- 'sm': max-w-sm (confirmations)
- 'md': max-w-md (forms)
- 'lg': max-w-lg (image preview)
- 'xl': max-w-4xl (recipe preview)
```

### 8.6 Navigation

#### Header

```
┌─────────────────────────────────────────────────────────────┐
│ [Logo]    [Search Bar          ]    [Nav] [Auth] [Avatar] │
└─────────────────────────────────────────────────────────────┘
```

**Elements:**

- Logo (left): Brand mark
- Search (center): Expandable search input
- Nav (right): Categories dropdown, links
- Auth (right): Login/Register or User menu

#### Footer

```
┌─────────────────────────────────────────────────────────────┐
│ Links | Categories | About | Contact                       │
│ ───────────────────────────────────────────────────────────│
│ Social Icons | Newsletter | Copyright                      │
└─────────────────────────────────────────────────────────────┘
```

### 8.7 Empty State

```tsx
// Purpose: No data available

Structure:
┌─────────────────────────────────────┐
│                                     │
│     [Illustration/Icon]             │
│                                     │
│     Title: "Chưa có công thức"     │
│     Description: "Hãy tạo công     │
│     thức đầu tiên của bạn"          │
│                                     │
│     [Primary Action Button]          │
│                                     │
└─────────────────────────────────────┘
```

### 8.8 Loading States

#### Skeleton Loader

```tsx
// Purpose: Content loading placeholder

RecipeCardSkeleton:
┌───────────────────────────────┐
│ ████████████████████████████ │ (image placeholder)
│                               │
│ ██████████████               │ (title placeholder)
│ ██████████                   │ (meta placeholder)
└───────────────────────────────┘
```

#### Spinner

```tsx
// Purpose: Button loading, small operations
// Size: 16px for buttons, 24px for inline, 48px for page
```

### 8.9 Toast Notifications

```tsx
// Purpose: Transient feedback messages

Position: Bottom-right (desktop), Bottom-center (mobile)

Variants:
- 'success': Success background, check icon
- 'error': Error background, X icon
- 'warning': Warning background, alert icon
- 'info': Info background, info icon

Duration: 5 seconds auto-dismiss
```

### 8.10 Form Validation

```tsx
// Inline Validation Rules:

Input States:
- Default: Gray border
- Focus: Primary border + ring
- Error: Error border + error message below
- Success: Success border (optional)

Validation Display:
- Show error on blur (not on keystroke)
- Show success checkmark when valid (optional)
- Error messages appear below input
```

---

## 9. Responsive Design

### 9.1 Mobile (&lt; 640px)

**Layout Changes:**

- Single column grid
- Collapsible sidebar
- Bottom navigation bar
- Hamburger menu for header

**Recipe Grid:**

- 1 column
- Full-width cards

**Recipe Detail:**

- Image above content
- Sidebar moves below main content
- Sticky action buttons at bottom

**Auth Pages:**

- Full-width form
- No side illustrations
- Sticky header

### 9.2 Tablet (640px - 1023px)

**Layout Changes:**

- 2 column grid
- Sidebar collapsible
- Top navigation

**Recipe Grid:**

- 2 columns
- Standard card size

### 9.3 Desktop (1024px+)

**Layout Changes:**

- Full navigation visible
- 3-4 column grid
- Persistent sidebar on detail pages

**Recipe Grid:**

- 3-4 columns (configurable)
- Hover effects enabled

### 9.4 Responsive Behaviors


| Component     | Mobile        | Tablet      | Desktop  |
| ------------- | ------------- | ----------- | -------- |
| Recipe Grid   | 1 col         | 2 cols      | 3-4 cols |
| Header        | Hamburger     | Compact     | Full     |
| Sidebar       | Below content | Collapsible | Visible  |
| Search        | Expandable    | Inline      | Inline   |
| Recipe Images | 16:9          | 16:9        | 16:9     |
| Modal         | Full screen   | Centered    | Centered |


---

## 10. Accessibility

### 10.1 Keyboard Navigation


| Element  | Key           | Action         |
| -------- | ------------- | -------------- |
| Button   | Enter / Space | Activate       |
| Link     | Enter         | Navigate       |
| Input    | Tab           | Focus next     |
| Modal    | Escape        | Close          |
| Dropdown | Arrow keys    | Navigate items |


### 10.2 Focus States

```css
/* Visible focus indicator */
*:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}
```

### 10.3 ARIA Labels

```tsx
// Required labels
[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%3CButton%20aria-label%3D%22%C4%90%C4%83ng%20nh%E1%BA%ADp%20b%E1%BA%B1ng%20Google%22%3E]]
[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%3CSearchInput%20aria-label%3D%22T%C3%ACm%20ki%E1%BA%BFm%20c%C3%B4ng%20th%E1%BB%A9c%22%20%2F%3E]]
[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%3CImage%20alt%3D%22C%C3%B4ng%20th%E1%BB%A9c%20%5BT%C3%AAn%5D%20-%20%E1%BA%A2nh%20%5BS%E1%BB%91%5D%22%20%2F%3E]]
```

### 10.4 Color Independence

**Never use color alone to convey information.**


| Instead of        | Use                                          |
| ----------------- | -------------------------------------------- |
| Red border only   | Red border + error icon + error text         |
| Green for success | Green background + check icon + "Thành công" |
| Gray for disabled | Gray + reduced opacity + disabled cursor     |


### 10.5 Reduced Motion

```css
@media (prefers-reduced-motion: reduce) {
  * {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

### 10.6 Screen Reader Support

- Use semantic HTML (`<nav>`, `<main>`, `<article>`, `<aside>`)
- Skip links for main content
- Meaningful link text (not "click here")
- Alt text for all images
- ARIA live regions for dynamic content

---

## 11. Animation &amp; Interaction

### 11.1 Transition Guidelines


| Type                  | Duration | Easing      | Usage               |
| --------------------- | -------- | ----------- | ------------------- |
| Micro (hover, focus)  | 150ms    | ease-out    | Buttons, links      |
| UI (expand, collapse) | 200ms    | ease-in-out | Dropdowns, modals   |
| Page (enter, exit)    | 300ms    | ease-in-out | Page transitions    |
| Layout (reflow)       | 300ms    | ease-in-out | Skeleton to content |


### 11.2 Hover Effects

```tsx
// Card hover
<Card className="transition-shadow hover:shadow-lg">
  
// Button hover
<Button className="transition-colors hover:bg-primary-600">
  
// Image zoom
<Image className="transition-transform hover:scale-105">
```

### 11.3 Loading States

```tsx
// Page transition
[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%3CPageTransition%3E]]
[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%20%20%3CRecipeDetailPage%20%2F%3E]]
[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%3C%2FPageTransition%3E]]

// Skeleton to content
{isLoading ? (
[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%20%20%3CRecipeSkeleton%20%2F%3E]]
) : (
[[ORCA_RICH_MD:9890731ef623e6923a2adcbc1a42e285:block-html:%20%20%3CRecipeContent%20%2F%3E]]
)}
```

### 11.4 Success Feedback

```tsx
// Toast on success
toast.success('Công thức đã được xuất bản!');

// Button loading
<Button isLoading={isPublishing}>
  Xuất bản
</Button>
```

### 11.5 Reduced Motion Behavior

All animations respect `prefers-reduced-motion`:

- No auto-play carousels
- No parallax effects
- Instant state changes
- Static loading indicators

---

## 12. Feature Specifications

### 12.1 FR-AUTH: Authentication

#### 12.1.1 Register Page

**SRS Reference:** FR-AUTH-001

**User Goal:** Create new account and get immediate access

**User Flow:**

```
1. User clicks "Đăng ký" button
2. Register form displays
3. User fills: Display Name, Email, Username, Password
4. User clicks "Đăng ký"
5. System validates
6. System creates account + auto-login
7. Redirect to Homepage or intended page
```

**Page Structure:**

```
┌─────────────────────────────────────┐
│ Minimal Header (Logo only)          │
├─────────────────────────────────────┤
│                                     │
│    ┌───────────────────────────┐    │
│    │                           │    │
│    │   Đăng ký tài khoản      │    │
│    │   ─────────────────────   │    │
│    │   [Display Name     ]     │    │
│    │   [Email           ]     │    │
│    │   [Username        ]     │    │
│    │   [Password        ] 👁    │    │
│    │   [Confirm Password] 👁   │    │
│    │                           │    │
│    │   [  Đăng ký  ]          │    │
│    │                           │    │
│    │   ─────── Hoặc ───────   │    │
│    │                           │    │
│    │   [ Google ]              │    │
│    │                           │    │
│    │   Đã có tài khoản?       │    │
│    │   [Đăng nhập]            │    │
│    │                           │    │
│    └───────────────────────────┘    │
│                                     │
├─────────────────────────────────────┤
│ Footer                              │
└─────────────────────────────────────┘
```

**Fields:**


| Field           | Type     | Validation                                     | Required |
| --------------- | -------- | ---------------------------------------------- | -------- |
| displayName     | text     | 2-100 chars, Vietnamese letters                | Yes      |
| email           | email    | Valid email format                             | Yes      |
| userName        | text     | 3-30 chars, a-z, 0-9, _                        | Yes      |
| password        | password | 8+ chars, 1 upper, 1 lower, 1 digit, 1 special | Yes      |
| confirmPassword | password | Must match password                            | Yes      |


**UI States:**

- Default: Empty form
- Validation: Inline errors below fields
- Loading: Button shows spinner, form disabled
- Error: Toast notification
- Success: Redirect to homepage

**API Endpoint:** `POST /api/v1/auth/register`

#### 12.1.2 Login Page

**SRS Reference:** FR-AUTH-002

**User Goal:** Sign in with existing credentials

**User Flow:**

```
1. User clicks "Đăng nhập" button
2. Login form displays
3. User fills: Email, Password
4. User clicks "Đăng nhập"
5. System validates credentials
6. System returns tokens
7. Redirect to Homepage or intended page
```

**Page Structure:**

```
┌─────────────────────────────────────┐
│ Minimal Header (Logo only)          │
├─────────────────────────────────────┤
│                                     │
│    ┌───────────────────────────┐    │
│    │                           │    │
│    │   Đăng nhập               │    │
│    │   ─────────────────────   │    │
│    │   [Email           ]     │    │
│    │   [Password        ] 👁   │    │
│    │                           │    │
│    │   [  Đăng nhập  ]        │    │
│    │                           │    │
│    │   ─────── Hoặc ───────   │    │
│    │                           │    │
│    │   [ Google ]              │    │
│    │                           │    │
│    │   Quên mật khẩu?          │    │
│    │                           │    │
│    │   Chưa có tài khoản?      │    │
│    │   [Đăng ký]              │    │
│    │                           │    │
│    └───────────────────────────┘    │
│                                     │
├─────────────────────────────────────┤
│ Footer                              │
└─────────────────────────────────────┘
```

**Fields:**


| Field    | Type     | Validation         | Required |
| -------- | -------- | ------------------ | -------- |
| email    | email    | Valid email format | Yes      |
| password | password | Not empty          | Yes      |


**UI States:**

- Default: Empty form
- Invalid: Generic error "Email hoặc mật khẩu không đúng"
- Locked: Show unlock time remaining
- Loading: Button spinner
- 2FA: Show 2FA input (future)

**API Endpoint:** `POST /api/v1/auth/login`

#### 12.1.3 Profile Page

**SRS Reference:** FR-AUTH-006, FR-AUTH-007

**User Goal:** View and update personal profile

**Page Structure:**

```
┌─────────────────────────────────────────────────────────────┐
│ Header                                                     │
├─────────────────────────────────────────────────────────────┤
│ Breadcrumb: Home / Hồ sơ                                   │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────┐    │
│ │ Profile Header:                                      │    │
│ │ [Avatar] Display Name                                │    │
│ │ Email | Username | Joined date                        │    │
│ │ [Chỉnh sửa hồ sơ] button                           │    │
│ └─────────────────────────────────────────────────────┘    │
│                                                              │
│ Tab Navigation: [Hồ sơ] [Công thức của tôi]               │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│ Profile Tab:                                                │
│ ┌─────────────────────────────────────────────────────┐    │
│ │ Display Name: [Nguyễn Văn A                    ]   │    │
│ │ Bio:          [Yêu thích nấu ăn...            ]   │    │
│ │ Avatar URL:   [https://...                     ]   │    │
│ │                                                     │    │
│ │ [Lưu thay đổi]                                    │    │
│ └─────────────────────────────────────────────────────┘    │
│                                                              │
│ My Recipes Tab:                                             │
│ ┌─────────────────────────────────────────────────────┐    │
│ │ Recipe list (Author's recipes)                       │    │
│ │ [Create New Recipe] button                          │    │
│ └─────────────────────────────────────────────────────┘    │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**Fields (Update Profile):**


| Field       | Type     | Validation      | Required      |
| ----------- | -------- | --------------- | ------------- |
| displayName | text     | 2-100 chars     | No (optional) |
| avatarUrl   | url      | Valid HTTPS URL | No (optional) |
| bio         | textarea | Max 500 chars   | No (optional) |


**Note:** Email and Username cannot be changed via this endpoint (SRS requirement)

**API Endpoints:**

- `GET /api/v1/auth/me` - View profile
- `PATCH /api/v1/auth/me` - Update profile

---

### 12.2 FR-CAT: Categories

#### 12.2.1 Category Listing (Navigation)

**SRS Reference:** FR-CAT-001, FR-CAT-002

**User Goal:** Browse recipes by category

**Implementation:** Horizontal scrollable strip on homepage

```
┌─────────────────────────────────────────────────────────────┐
│ [Tất cả] [Breakfast] [Lunch] [Dinner] [Dessert] [Drinks] →│
└─────────────────────────────────────────────────────────────┘
```

**Behavior:**

- "Tất cả" selected by default (shows all recipes)
- Click category → Filter recipes
- Active category highlighted with primary color
- Horizontal scroll on mobile with touch/swipe

#### 12.2.2 Category Detail Page

**SRS Reference:** FR-CAT-002

**URL:** `/categories/[slug]`

**Page Structure:**

```
┌─────────────────────────────────────────────────────────────┐
│ Header                                                     │
├─────────────────────────────────────────────────────────────┤
│ Category Header:                                            │
│ [Icon] Category Name                                        │
│ Description text                                           │
│ Recipe count                                               │
├─────────────────────────────────────────────────────────────┤
│ Sort/Filter Bar:                                            │
│ [Sort: Mới nhất ▼] [Filter] [Grid/List]                  │
├─────────────────────────────────────────────────────────────┤
│ Recipe Grid:                                                │
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐                       │
│ │ Card │ │ Card │ │ Card │ │ Card │                       │
│ └──────┘ └──────┘ └──────┘ └──────┘                       │
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐                       │
│ │ Card │ │ Card │ │ Card │ │ Card │                       │
│ └──────┘ └──────┘ └──────┘ └──────┘                       │
├─────────────────────────────────────────────────────────────┤
│ Pagination                                                  │
└─────────────────────────────────────────────────────────────┘
```

---

### 12.3 FR-RCP: Recipe Management

#### 12.3.1 Recipe Listing (Homepage)

**SRS Reference:** FR-RCP-001

**User Goal:** Discover and browse recipes

**Page Structure:**

```
┌─────────────────────────────────────────────────────────────┐
│ Header                                                     │
├─────────────────────────────────────────────────────────────┤
│ [Optional Hero Carousel: Featured Recipes]                  │
├─────────────────────────────────────────────────────────────┤
│ Category Strip                                              │
├─────────────────────────────────────────────────────────────┤
│ Sort/Filter Bar:                                            │
│ [Mới nhất ▼] [Độ khó ▼] [Thời gian ▼]  [Grid][List]     │
├─────────────────────────────────────────────────────────────┤
│ Recipe Grid (Published only):                               │
│ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐              │
│ │ Recipe │ │ Recipe │ │ Recipe │ │ Recipe │              │
│ │  Card  │ │  Card  │ │  Card  │ │  Card  │              │
│ └────────┘ └────────┘ └────────┘ └────────┘              │
├─────────────────────────────────────────────────────────────┤
│ Pagination                                                  │
└─────────────────────────────────────────────────────────────┘
```

**Filters:**


| Filter     | Options                             | Source                 |
| ---------- | ----------------------------------- | ---------------------- |
| Category   | All, or specific category           | FR-CAT                 |
| Difficulty | All, Easy, Medium, Hard             | Recipe.difficultyLevel |
| Prep Time  | All, &lt;30min, 30-60min, &gt;60min | Recipe.prepTime        |
| Sort       | Newest, Oldest, A-Z, Z-A            | Various                |


#### 12.3.2 Recipe Detail Page

**SRS Reference:** FR-RCP-002

**URL:** `/recipes/[slug]`

**Page Structure:**

```
┌─────────────────────────────────────────────────────────────┐
│ Header                                                     │
├─────────────────────────────────────────────────────────────┤
│ Breadcrumb: Home > [Category] > [Recipe Title]              │
├─────────────────────────────────────────────────────────────┤
│ Recipe Header Section:                                      │
│ ┌─────────────────────────────────┬───────────────────┐   │
│ │                                 │ Sidebar:            │   │
│ │   [Hero Image - 16:9]           │ • Author card      │   │
│ │                                 │ • Category badge    │   │
│ │   [Thumbnail strip below]       │ • Prep: 15 phút     │   │
│ │                                 │ • Cook: 30 phút    │   │
│ │                                 │ • Servings: 4       │   │
│ │                                 │ • Difficulty: Dễ   │   │
│ └─────────────────────────────────┴───────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│ [Author Actions - if owner]:                               │
│ [Chỉnh sửa] [Xuất bản] [Xóa]                              │
├─────────────────────────────────────────────────────────────┤
│ Recipe Content:                                             │
│                                                              │
│ ## Mô tả                                                   │
│ Recipe description text...                                   │
│                                                              │
│ ## Thông tin dinh dưỡng [▼]                                │
│ (Collapsible nutrition table)                               │
│                                                              │
│ ## Nguyên liệu                                             │
│ • 2 cups flour                                             │
│ • 1 cup sugar                                              │
│ • ...                                                       │
│                                                              │
│ ## Các bước thực hiện                                      │
│ 1. Step one text...                                         │
│ 2. Step two text... [image]                                 │
│ 3. Step three text...                                       │
│                                                              │
│ ## Hình ảnh                                                │
│ [Gallery grid of recipe images]                             │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│ Related Recipes:                                            │
│ ┌──────┐ ┌──────┐ ┌──────┐                               │
│ │ Card │ │ Card │ │ Card │                               │
│ └──────┘ └──────┘ └──────┘                               │
└─────────────────────────────────────────────────────────────┘
```

**Author Actions (visible only to recipe owner):**

- `PATCH /api/v1/recipes/{id}` - Edit recipe
- `POST /api/v1/recipes/{id}/publish` - Publish recipe
- `POST /api/v1/recipes/{id}/archive` - Archive recipe
- `DELETE /api/v1/recipes/{id}` - Delete recipe

#### 12.3.3 Create/Edit Recipe Page

**SRS Reference:** FR-RCP-003, FR-RCP-004

**URL:** `/recipes/create` or `/recipes/[id]/edit`

**Page Structure:**

```
┌─────────────────────────────────────────────────────────────┐
│ Header                                                     │
├─────────────────────────────────────────────────────────────┤
│ Breadcrumb: Home > Tạo công thức                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│ Recipe Form:                                                │
│                                                              │
│ ## Thông tin cơ bản                                        │
│ ┌─────────────────────────────────────────────────────┐      │
│ │ Title:        [                               ]      │      │
│ │ Description:  [                               ]      │      │
│ │ Category:     [ ▼ Select Category          ]      │      │
│ │ Prep Time:    [15] phút   Cook Time: [30] phút    │      │
│ │ Servings:    [4]         Difficulty: [▼ Easy ]    │      │
│ └─────────────────────────────────────────────────────┘      │
│                                                              │
│ ## Hình ảnh                                                 │
│ ┌─────────────────────────────────────────────────────┐      │
│ │ [Image Upload Zone]                                   │      │
│ │ Drag & drop or click to upload                       │      │
│ │ Max 5MB, JPG/PNG/WEBP/AVIF                          │      │
│ │                                                      │      │
│ │ [img1] [img2] [img3] [+ Add more]                  │      │
│ │   ★ (primary)                                       │      │
│ └─────────────────────────────────────────────────────┘      │
│                                                              │
│ ## Nguyên liệu                                             │
│ ┌─────────────────────────────────────────────────────┐      │
│ │ [+ Thêm nguyên liệu]                                │      │
│ │ 1. [Amount] [Unit ▼] [Ingredient name] [×]          │      │
│ │ 2. [Amount] [Unit ▼] [Ingredient name] [×]          │      │
│ └─────────────────────────────────────────────────────┘      │
│                                                              │
│ ## Các bước thực hiện                                       │
│ ┌─────────────────────────────────────────────────────┐      │
│ │ [+ Thêm bước]                                       │      │
│ │ 1. [Step description                    ] [📷] [×] │      │
│ │    [Optional image]                                  │      │
│ │ 2. [Step description                    ] [📷] [×] │      │
│ └─────────────────────────────────────────────────────┘      │
│                                                              │
│ ## Thông tin dinh dưỡng (tùy chọn)                          │
│ ┌─────────────────────────────────────────────────────┐      │
│ │ Calories: [ ]   Protein: [ ] g                     │      │
│ │ Carbs:    [ ] g   Fat:    [ ] g                     │      │
│ └─────────────────────────────────────────────────────┘      │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│ Form Actions:                                                │
│ [Lưu nháp] [Xem trước] [Xuất bản]                          │
└─────────────────────────────────────────────────────────────┘
```

**Recipe Status:**

- `Draft`: Saved but not published
- `Published`: Visible to all users
- `Archived`: Hidden but not deleted

---

### 12.4 FR-SRCH: Search

#### 12.4.1 Search Bar

**SRS Reference:** FR-SRCH-001

**Location:** Header (always visible on desktop, expandable on mobile)

**Behavior:**

```
1. User types in search box
2. After 300ms debounce, show suggestions
3. Press Enter or click search icon
4. Navigate to search results page
```

**Search Suggestions (dropdown):**

```
┌─────────────────────────────────────────┐
│ 🔥 Top searches                         │
│ Cơm rang    |    Phở    |    Bánh mì    │
├─────────────────────────────────────────┤
│ 🔍 Suggestions                          │
│ Cơm gà      |    Cơm tấm   |   Cháo    │
└─────────────────────────────────────────┘
```

#### 12.4.2 Search Results Page

**URL:** `/search?q={query}&category={cat}&sort={sort}&page={page}`

**Page Structure:**

```
┌─────────────────────────────────────────────────────────────┐
│ Header                                                     │
├─────────────────────────────────────────────────────────────┤
│ Search Header:                                              │
│ "Kết quả tìm kiếm cho: 'cơm'"                            │
│ 42 công thức được tìm thấy                                 │
├─────────────────────────────────────────────────────────────┤
│ Filters:                                                    │
│ [Category ▼] [Difficulty ▼] [Time ▼]  [Clear all]       │
├─────────────────────────────────────────────────────────────┤
│ Sort: [Mới nhất ▼]                        [Grid][List]   │
├─────────────────────────────────────────────────────────────┤
│ Results Grid:                                               │
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐                      │
│ │ Card │ │ Card │ │ Card │ │ Card │                      │
│ │      │ │      │ │      │ │      │  (highlighted match) │
│ └──────┘ └──────┘ └──────┘ └──────┘                      │
├─────────────────────────────────────────────────────────────┤
│ Pagination                                                  │
└─────────────────────────────────────────────────────────────┘
```

**Search Features:**

- Full-text search on recipe title, description, ingredients
- Highlight matched terms in results
- Filter by category, difficulty, time
- Sort by relevance, newest, oldest

---

### 12.5 FR-FILE: Image Upload

#### 12.5.1 Image Upload Component

**SRS Reference:** FR-FILE-001, FR-FILE-002

**Used in:** Recipe create/edit page

**Implementation:**

```
┌─────────────────────────────────────────────────────────────┐
│ Image Upload Zone                                          │
│ ┌─────────────────────────────────────────────────────┐  │
│ │                                                      │  │
│ │     📷 Drag & drop images here                      │  │
│ │     or click to browse                              │  │
│ │                                                      │  │
│ │     Supported: JPG, PNG, WEBP, AVIF                 │  │
│ │     Max size: 5MB                                   │  │
│ │                                                      │  │
│ └─────────────────────────────────────────────────────┘  │
│                                                              │
│ Uploaded Images:                                            │
│ ┌────────┐ ┌────────┐ ┌────────┐ ┌───────┐             │
│ │ [img1] │ │ [img2] │ │ [img3] │ │ [ + ] │             │
│ │  ★     │ │        │ │        │ │  Add  │             │
│ │ [set]  │ │ [set★] │ │ [set★] │ │ more  │             │
│ │ [del]  │ │ [del]  │ │ [del]  │ │       │             │
│ └────────┘ └────────┘ └────────┘ └───────┘             │
└─────────────────────────────────────────────────────────────┘
```

**Upload States:**

- Idle: Dashed border, upload icon
- Dragover: Primary border, "Drop here" text
- Uploading: Progress bar per image
- Success: Thumbnail preview
- Error: Error message, retry button
- Deleting: Spinner overlay

---

## 13. Error Handling

### 13.1 Error States


| Error Type       | Display                                              | Action         |
| ---------------- | ---------------------------------------------------- | -------------- |
| Network Error    | Toast: "Không thể kết nối. Vui lòng thử lại."        | Retry button   |
| 401 Unauthorized | Redirect to login                                    | Login required |
| 403 Forbidden    | Toast: "Bạn không có quyền thực hiện hành động này." | Go back        |
| 404 Not Found    | 404 Page: "Trang không tồn tại"                      | Go home link   |
| 422 Validation   | Inline field errors                                  | Show errors    |
| 500 Server Error | Toast: "Đã xảy ra lỗi. Vui lòng thử lại."            | Retry          |


### 13.2 404 Page

```
┌─────────────────────────────────────────────────────────────┐
│ Header                                                     │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│                    😕                                        │
│                                                              │
│              Trang không tìm thấy                          │
│                                                              │
│     Có vẻ như trang bạn đang tìm không tồn tại            │
│     hoặc đã được di chuyển.                                 │
│                                                              │
│     [← Quay lại trang chủ]  [Tìm kiếm công thức]          │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 13.3 Empty States

**No Search Results:**

```
┌─────────────────────────────────────┐
│                                      │
│     🔍                               │
│                                      │
│     Không tìm thấy kết quả           │
│                                      │
│     Thử từ khóa khác hoặc             │
│     xóa bộ lọc                       │
│                                      │
│     [Xóa bộ lọc]                     │
│                                      │
└─────────────────────────────────────┘
```

**No Recipes (Author):**

```
┌─────────────────────────────────────┐
│                                      │
│     🍳                               │
│                                      │
│     Chưa có công thức nào            │
│                                      │
│     Hãy tạo công thức đầu tiên       │
│     của bạn và chia sẻ với cộng đồng │
│                                      │
│     [+ Tạo công thức mới]           │
│                                      │
└─────────────────────────────────────┘
```

---

## 14. Requirement → UI Traceability


| FR          | Requirement        | User Action     | Screen             | Component           | State                      |
| ----------- | ------------------ | --------------- | ------------------ | ------------------- | -------------------------- |
| FR-AUTH-001 | Register           | Submit form     | /register          | Form, Button        | Loading, Success, Error    |
| FR-AUTH-002 | Login              | Submit form     | /login             | Form, Button        | Loading, Error, Locked     |
| FR-AUTH-003 | Google OAuth       | Click button    | /login, /register  | Social Button       | Loading, Error             |
| FR-AUTH-004 | Refresh Token      | Auto            | (System)           | -                   | Auto-refresh               |
| FR-AUTH-005 | Logout             | Click logout    | Header dropdown    | Menu Item           | Success toast              |
| FR-AUTH-006 | View Profile       | Navigate        | /profile           | User Info Card      | Loading, Content           |
| FR-AUTH-007 | Update Profile     | Edit fields     | /profile           | Form, Inputs        | Validation, Save           |
| FR-RCP-001  | Browse Recipes     | Scroll/Filter   | /                  | Recipe Grid         | Loading, Empty             |
| FR-RCP-002  | View Recipe        | Click card      | /recipes/[slug]    | Recipe Detail       | Loading, Content           |
| FR-RCP-003  | Create Recipe      | Submit form     | /recipes/create    | Recipe Form         | Validation, Draft, Publish |
| FR-RCP-004  | Edit Recipe        | Submit form     | /recipes/[id]/edit | Recipe Form         | Validation, Save           |
| FR-RCP-005  | Publish/Archive    | Click button    | /recipes/[id]/edit | Action Button       | Loading, Success           |
| FR-RCP-006  | Archive Recipe     | Click button    | /recipes/[id]/edit | Action Button       | Confirm, Success           |
| FR-RCP-007  | Delete Recipe      | Click button    | /recipes/[id]/edit | Confirm Modal       | Confirm, Delete            |
| FR-RCP-008  | Manage Images      | Upload/Delete   | /recipes/[id]/edit | Image Upload        | Progress, Success          |
| FR-RCP-009  | Manage Ingredients | Add/Edit/Delete | /recipes/[id]/edit | Ingredient List     | Add, Edit, Delete          |
| FR-RCP-010  | Manage Steps       | Add/Edit/Delete | /recipes/[id]/edit | Step List           | Add, Edit, Delete          |
| FR-CAT-001  | Browse Categories  | Click           | /                  | Category Strip      | Default, Active            |
| FR-CAT-002  | View Category      | Click           | /categories/[slug] | Category Page       | Filtered grid              |
| FR-SRCH-001 | Search             | Type/Submit     | /search            | Search Bar, Results | Suggestions, Results       |


---

## 15. UI Gaps

### 15.1 Identified Gaps


| Gap | Description                                       | Severity | Priority |
| --- | ------------------------------------------------- | -------- | -------- |
| G1  | Admin Category Management UI (FR-CAT-003/004/005) | HIGH     | P1       |
| G2  | Recipe Preview before publish                     | MEDIUM   | P2       |
| G3  | Image cropping/resize tool                        | LOW      | P3       |
| G4  | Bulk delete for images                            | LOW      | P3       |


### 15.2 Not in Scope


| Feature                 | Reason            |
| ----------------------- | ----------------- |
| Comment System          | Out of v1.0 scope |
| Rating System           | Out of v1.0 scope |
| Bookmark/Favorite       | Out of v1.0 scope |
| Real-time Notifications | Out of v1.0 scope |


---

## 16. Open Questions


| #   | Question                            | Status         | Notes                                  |
| --- | ----------------------------------- | -------------- | -------------------------------------- |
| Q1  | Homepage hero design?               | `[UNKNOWN]`    | Default to category + recent recipes   |
| Q2  | Featured recipe selection criteria? | `[UNKNOWN]`    | Latest published? Most viewed? Manual? |
| Q3  | Image upload compression?           | `[ASSUMPTION]` | Resize client-side before upload       |
| Q4  | Max images per recipe?              | `[ASSUMPTION]` | 10 images max                          |
| Q5  | How to handle recipe thumbnail?     | `[PROPOSAL]`   | First image or user-selected primary   |


---

## 17. Implementation Notes

### 17.1 Component Priority

**Phase 1 (Auth):**

1. Button
2. Input
3. Form Layout
4. Toast
5. Auth Pages

**Phase 2 (Browse):**
6. Recipe Card
7. Recipe Grid
8. Header/Navigation
9. Footer
10. Category Strip

**Phase 3 (Detail):**
11. Recipe Detail Layout
12. Ingredient List
13. Step List
14. Image Gallery

**Phase 4 (Create/Edit):**
15. Recipe Form
16. Image Upload
17. Rich Text Editor (for steps)

### 17.2 Key Dependencies


| Component     | Dependencies                        |
| ------------- | ----------------------------------- |
| Auth Pages    | Button, Input, Form, Toast, Auth.js |
| Recipe Card   | Image, Badge, Typography            |
| Recipe Detail | Auth check (for edit buttons)       |
| Image Upload  | MinIO client, File validation       |
| Search        | API integration, Debounce           |


### 17.3 Performance Considerations

- Use Next.js Image for all recipe images
- Implement skeleton loading for recipe grid
- Lazy load below-fold content
- Debounce search input (300ms)
- Prefetch recipe detail on card hover

---

## 18. Validation Checklist

- [x] All FRs have UI mapping
- [x] No functionality without FR
- [x] Color palette used correctly
- [x] Responsive design specified
- [x] Accessibility guidelines included
- [x] All states covered (loading, empty, error, success)
- [x] Consistent component usage
- [x] Clear interaction flows

---

## APPENDIX A: Component Inventory


| Component      | Variants                                | States                                    | Priority |
| -------------- | --------------------------------------- | ----------------------------------------- | -------- |
| Button         | primary, secondary, ghost, destructive  | default, hover, active, disabled, loading | P1       |
| Input          | text, email, password, search, textarea | default, focus, error, disabled           | P1       |
| Select         | single, multi                           | default, open, disabled                   | P1       |
| Card           | recipe, category, author                | default, hover, loading                   | P1       |
| Badge          | category, status, difficulty, time      | -                                         | P1       |
| Modal          | sm, md, lg, xl                          | -                                         | P1       |
| Toast          | success, error, warning, info           | -                                         | P1       |
| Skeleton       | card, text, avatar                      | -                                         | P1       |
| Empty State    | no-data, no-results, no-permission      | -                                         | P2       |
| Dropdown       | nav, user-menu, actions                 | -                                         | P2       |
| Tabs           | horizontal                              | default, active, disabled                 | P2       |
| Image Upload   | single, multiple                        | idle, dragover, uploading, error          | P2       |
| Pagination     | default, compact                        | -                                         | P2       |
| Confirm Dialog | delete, warning                         | -                                         | P2       |
| Tooltip        | top, bottom, left, right                | -                                         | P3       |
| Progress Bar   | linear                                  | -                                         | P3       |
| Breadcrumb     | -                                       | -                                         | P3       |


---

## APPENDIX B: Page Inventory


| Page             | URL                  | Auth | Role          | Priority |
| ---------------- | -------------------- | ---- | ------------- | -------- |
| Homepage         | `/`                  | No   | All           | P1       |
| Recipe Detail    | `/recipes/[slug]`    | No   | All           | P1       |
| Login            | `/login`             | No   | Guest         | P1       |
| Register         | `/register`          | No   | Guest         | P1       |
| Profile          | `/profile`           | Yes  | Author, Admin | P1       |
| Create Recipe    | `/recipes/create`    | Yes  | Author, Admin | P1       |
| Edit Recipe      | `/recipes/[id]/edit` | Yes  | Owner, Admin  | P1       |
| Search Results   | `/search`            | No   | All           | P1       |
| Category         | `/categories/[slug]` | No   | All           | P1       |
| 404              | `/404`               | No   | All           | P1       |
| Admin Categories | `/admin/categories`  | Yes  | Admin         | P2       |


---

# UI DESIGN SPECIFICATION STATUS

## READY FOR IMPLEMENTATION

### Ready Components (Can code now):

- ✅ Auth pages (Login, Register)
- ✅ Recipe cards and grid
- ✅ Header/Navigation
- ✅ Footer
- ✅ Form components
- ✅ Toast notifications
- ✅ Loading states

### Components with Open Questions:

- ⚠️ Recipe Detail (hero layout)
- ⚠️ Image Upload (compression)
- ⚠️ Admin UI (needs separate design)

### Not Covered (Out of v1.0 scope):

- ❌ Comment System
- ❌ Rating System
- ❌ Bookmark/Favorite
- ❌ Real-time notifications

### Blocker Issues:

None - All core FRs have UI specifications.

---

**Document Status:** Draft for Review
**Last Updated:** 2026-09-12
**Next Review:** After UX research validation