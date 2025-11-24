# UI/UX Guidelines for E-Ink Devices

## Overview

This document provides comprehensive design guidelines for creating a user interface optimized for E-Ink displays, older browser engines, and paper tablet devices.

## E-Ink Display Characteristics

### Technical Constraints

**Refresh Rates**:

- Full refresh: 300-1000ms
- Partial refresh: 150-300ms
- Text rendering: Generally fast
- Ghosting effect: Previous content may leave traces

**Color Capabilities**:

- Most E-Ink: Grayscale only (16 shades)
- Some newer models: 4096 colors (Kaleido)
- High contrast required for readability

**Resolution**:

- Typical: 1404×1872 (6.8"), 1264×1680 (7.8")
- 227-300 DPI
- Sharp text rendering

**Browser Performance**:

- JavaScript execution: 2-5x slower than LCD devices
- CSS rendering: Limited support for modern features
- WebKit/Chromium versions: Often 2-5 years behind

## Design Principles

### 1. Minimize Screen Refreshes

**Why**: E-Ink refreshes are slow and cause visual disruption

**How**:

- **Static Content**: Prefer server-rendered static pages
- **No Animations**: Avoid CSS animations, transitions, transforms
- **No Auto-Refresh**: Disable auto-updating content
- **Pagination Over Scrolling**: Reduce continuous scrolling
- **Deliberate Navigation**: Clear page transitions, no dynamic content loading

**Examples**:

✅ **Good**: Button click → Full page reload → New content
❌ **Bad**: Infinite scroll, live updates, animated transitions

### 2. High Contrast Design

**Why**: E-Ink has limited gray levels

**How**:

- **Black text on white background** (primary)
- Avoid subtle grays (use #000000 and #FFFFFF)
- Clear visual hierarchy through size and weight, not color
- Bold text for emphasis
- No gradients or shadows
- Thick, solid borders instead of subtle shadows

**Color Palette**:

```css
/* Primary colors */
--color-background: #FFFFFF;
--color-text: #000000;
--color-border: #000000;

/* Secondary (use sparingly) */
--color-gray-light: #E5E5E5;
--color-gray-medium: #999999;
--color-gray-dark: #333333;

/* Avoid */
/* No subtle grays, no colors, no gradients */
```

### 3. Clear Visual Hierarchy

**Why**: Color-based hierarchy doesn't work well on grayscale

**How**:

- **Typography Scale**: Clear size differences
  - Headings: 1.5×, 1.25×, 1.125× base size
  - Body: 16px minimum
  - Small text: 14px minimum
- **Font Weight**: Regular (400) vs Bold (700), skip medium weights
- **Spacing**: Generous whitespace for grouping
- **Borders**: Thick borders (2px+) for sections
- **Lists**: Clear bullets and numbering

### 4. Touch-Friendly Interactions

**Why**: E-Ink tablets are touch-based

**How**:

- **Minimum Touch Target**: 44×44px (iOS guideline)
- **Generous Spacing**: 8px minimum between interactive elements
- **Large Buttons**: Prefer large, clear buttons over small links
- **No Hover States**: Touch devices don't hover
- **Clear Active States**: Show feedback on tap
- **Avoid Drag Gestures**: Simple tap interactions only

### 5. Simplified Layouts

**Why**: Complex layouts render slowly and poorly

**How**:

- **Single Column on Mobile**: Avoid multi-column layouts
- **CSS Grid/Flexbox**: Use for simple layouts only
- **No Fixed Positioning**: Avoid fixed headers/footers (causes refresh issues)
- **Linear Reading Flow**: Top to bottom, left to right
- **Minimal Nesting**: Shallow DOM depth

### 6. Typography Best Practices

**Font Selection**:

- **Serif fonts**: Good for body text (Georgia, Merriweather)
- **Sans-serif fonts**: Good for UI (Arial, Helvetica, system fonts)
- **System fonts**: Best performance
- **Web fonts**: Use sparingly, load blocking

**Recommended Fonts**:

```css
/* Body text - serif for readability */
font-family: Georgia, Cambria, "Times New Roman", Times, serif;

/* UI elements - sans-serif */
font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, 
             "Helvetica Neue", Arial, sans-serif;

/* Monospace - code or tabular data */
font-family: "Courier New", Courier, monospace;
```

**Type Settings**:

```css
body {
  font-size: 16px; /* Minimum 16px */
  line-height: 1.6; /* Generous line height */
  font-weight: 400; /* Regular weight */
  color: #000000;
  background: #FFFFFF;
}

h1 { font-size: 2rem; font-weight: 700; margin: 1.5rem 0 1rem; }
h2 { font-size: 1.5rem; font-weight: 700; margin: 1.25rem 0 0.75rem; }
h3 { font-size: 1.25rem; font-weight: 700; margin: 1rem 0 0.5rem; }

p { margin: 0 0 1rem; }
```

## Layout Patterns

### Page Layout

```
┌─────────────────────────────────────┐
│  Header (Logo, Account Info)        │
├─────────────────────────────────────┤
│  Navigation (Folders, Actions)      │
├─────────────────────────────────────┤
│                                      │
│  Main Content Area                  │
│  (Email List, Reading Pane, etc.)   │
│                                      │
├─────────────────────────────────────┤
│  Pagination / Actions                │
├─────────────────────────────────────┤
│  Footer (Minimal)                    │
└─────────────────────────────────────┘
```

### Email List Pattern

```html
<div class="email-list">
  <article class="email-item" style="border-bottom: 2px solid #000; padding: 16px 0;">
    <div class="email-header" style="display: flex; justify-content: space-between; margin-bottom: 8px;">
      <strong class="sender">John Doe</strong>
      <time class="date">Today, 2:30 PM</time>
    </div>
    <h3 class="subject" style="font-size: 18px; margin: 0 0 8px;">Email Subject Line</h3>
    <p class="preview" style="color: #333; margin: 0;">Email preview text goes here...</p>
    <div class="meta" style="margin-top: 8px; font-size: 14px; color: #666;">
      <span class="has-attachment">📎 Attachment</span>
    </div>
  </article>
</div>
```

### Reading View Pattern

```html
<article class="email-detail">
  <header class="email-header" style="border-bottom: 2px solid #000; padding-bottom: 16px; margin-bottom: 24px;">
    <h1 style="font-size: 24px; margin: 0 0 16px;">Email Subject</h1>
    <dl class="email-meta" style="font-size: 14px;">
      <dt style="font-weight: 700; display: inline;">From:</dt>
      <dd style="display: inline; margin: 0 16px 0 4px;">sender@example.com</dd>
      
      <dt style="font-weight: 700; display: inline;">To:</dt>
      <dd style="display: inline; margin: 0 16px 0 4px;">you@example.com</dd>
      
      <dt style="font-weight: 700; display: inline;">Date:</dt>
      <dd style="display: inline; margin: 0;">Nov 24, 2025, 2:30 PM</dd>
    </dl>
  </header>
  
  <div class="email-body" style="line-height: 1.6;">
    <!-- Email content -->
  </div>
  
  <footer class="email-actions" style="border-top: 2px solid #000; padding-top: 16px; margin-top: 24px;">
    <button style="min-width: 120px; padding: 12px 24px; margin-right: 8px;">Reply</button>
    <button style="min-width: 120px; padding: 12px 24px; margin-right: 8px;">Forward</button>
    <button style="min-width: 120px; padding: 12px 24px;">Delete</button>
  </footer>
</article>
```

### Form Pattern (Compose Email)

```html
<form class="compose-form" style="max-width: 100%;">
  <div class="form-group" style="margin-bottom: 16px;">
    <label for="to" style="display: block; font-weight: 700; margin-bottom: 4px;">To:</label>
    <input 
      type="email" 
      id="to" 
      name="to"
      style="width: 100%; padding: 12px; border: 2px solid #000; font-size: 16px;"
    />
  </div>
  
  <div class="form-group" style="margin-bottom: 16px;">
    <label for="subject" style="display: block; font-weight: 700; margin-bottom: 4px;">Subject:</label>
    <input 
      type="text" 
      id="subject" 
      name="subject"
      style="width: 100%; padding: 12px; border: 2px solid #000; font-size: 16px;"
    />
  </div>
  
  <div class="form-group" style="margin-bottom: 24px;">
    <label for="body" style="display: block; font-weight: 700; margin-bottom: 4px;">Message:</label>
    <textarea 
      id="body" 
      name="body" 
      rows="12"
      style="width: 100%; padding: 12px; border: 2px solid #000; font-size: 16px; line-height: 1.6; font-family: inherit;"
    ></textarea>
  </div>
  
  <div class="form-actions">
    <button type="submit" style="min-width: 120px; padding: 12px 24px; margin-right: 8px; background: #000; color: #FFF; border: 2px solid #000; font-weight: 700;">Send</button>
    <button type="button" style="min-width: 120px; padding: 12px 24px; border: 2px solid #000; background: #FFF;">Save Draft</button>
  </div>
</form>
```

## Component Design Guidelines

### Buttons

**Primary Button**:

```css
.btn-primary {
  min-width: 120px;
  min-height: 44px;
  padding: 12px 24px;
  background: #000000;
  color: #FFFFFF;
  border: 2px solid #000000;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
}

/* No hover effects, only active/focus */
.btn-primary:active,
.btn-primary:focus {
  outline: 4px solid #000000;
  outline-offset: 2px;
}
```

**Secondary Button**:

```css
.btn-secondary {
  min-width: 120px;
  min-height: 44px;
  padding: 12px 24px;
  background: #FFFFFF;
  color: #000000;
  border: 2px solid #000000;
  font-size: 16px;
  font-weight: 400;
  cursor: pointer;
}
```

### Input Fields

```css
input[type="text"],
input[type="email"],
textarea {
  width: 100%;
  padding: 12px;
  border: 2px solid #000000;
  font-size: 16px;
  font-family: inherit;
  background: #FFFFFF;
  color: #000000;
}

input:focus,
textarea:focus {
  outline: 4px solid #000000;
  outline-offset: 2px;
}
```

### Links

```css
a {
  color: #000000;
  text-decoration: underline;
  text-decoration-thickness: 2px;
  text-underline-offset: 2px;
}

a:focus {
  outline: 2px solid #000000;
  outline-offset: 4px;
}

/* Avoid :hover - doesn't work on touch */
```

### Checkboxes and Radio Buttons

```css
input[type="checkbox"],
input[type="radio"] {
  width: 24px;
  height: 24px;
  border: 2px solid #000000;
  /* Use native controls for better compatibility */
}
```

### Navigation

```html
<nav class="folder-nav" style="border-bottom: 2px solid #000; padding-bottom: 16px; margin-bottom: 16px;">
  <ul style="list-style: none; padding: 0; margin: 0;">
    <li style="margin-bottom: 8px;">
      <a href="/mail/inbox" style="display: block; padding: 12px; font-size: 18px; text-decoration: none; border: 2px solid #000;">
        <strong>Inbox</strong> <span style="float: right;">(5)</span>
      </a>
    </li>
    <li style="margin-bottom: 8px;">
      <a href="/mail/sent" style="display: block; padding: 12px; font-size: 18px; text-decoration: none; border: 2px solid transparent;">
        Sent
      </a>
    </li>
    <li style="margin-bottom: 8px;">
      <a href="/mail/drafts" style="display: block; padding: 12px; font-size: 18px; text-decoration: none; border: 2px solid transparent;">
        Drafts <span style="float: right;">(2)</span>
      </a>
    </li>
  </ul>
</nav>
```

### Pagination

```html
<nav class="pagination" style="display: flex; justify-content: space-between; align-items: center; padding: 16px 0; border-top: 2px solid #000;">
  <a href="?page=1" class="btn-secondary" style="min-width: 100px; text-align: center;">← Previous</a>
  <span style="font-size: 16px; font-weight: 700;">Page 2 of 10</span>
  <a href="?page=3" class="btn-secondary" style="min-width: 100px; text-align: center;">Next →</a>
</nav>
```

## Responsive Design

### Breakpoints

```css
/* Mobile first approach */

/* Base: 320px+ (all E-Ink devices) */
body {
  padding: 16px;
}

/* Small tablets: 600px+ */
@media (min-width: 600px) {
  body {
    padding: 24px;
  }
}

/* Larger tablets: 900px+ */
@media (min-width: 900px) {
  .container {
    max-width: 800px;
    margin: 0 auto;
  }
}
```

### Layout Adjustments

- **Mobile (< 600px)**: Single column, full width
- **Tablet (600-900px)**: Single column with margins
- **Large (> 900px)**: Centered content, max-width container

## TailwindCSS Configuration

### Custom Configuration for E-Ink

```javascript
// tailwind.config.js
module.exports = {
  theme: {
    extend: {
      colors: {
        // Grayscale only
        'black': '#000000',
        'white': '#FFFFFF',
        'gray': {
          100: '#F5F5F5',
          300: '#E5E5E5',
          500: '#999999',
          700: '#333333',
          900: '#000000',
        }
      },
      fontFamily: {
        'sans': ['-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'Arial', 'sans-serif'],
        'serif': ['Georgia', 'Cambria', 'Times New Roman', 'Times', 'serif'],
        'mono': ['Courier New', 'Courier', 'monospace'],
      },
      fontSize: {
        'base': '16px',
        'lg': '18px',
        'xl': '20px',
        '2xl': '24px',
        '3xl': '28px',
      },
      spacing: {
        // Generous spacing
        '2': '8px',
        '3': '12px',
        '4': '16px',
        '6': '24px',
        '8': '32px',
      },
      borderWidth: {
        DEFAULT: '2px',
        '0': '0px',
        '2': '2px',
        '4': '4px',
      },
    },
  },
  corePlugins: {
    // Disable unnecessary features
    animation: false,
    backdropBlur: false,
    backdropBrightness: false,
    backdropContrast: false,
    backdropFilter: false,
    backdropGrayscale: false,
    backdropHueRotate: false,
    backdropInvert: false,
    backdropOpacity: false,
    backdropSaturate: false,
    backdropSepia: false,
    backgroundOpacity: false,
    blur: false,
    brightness: false,
    contrast: false,
    dropShadow: false,
    gradientColorStops: false,
    grayscale: false,
    hueRotate: false,
    invert: false,
    mixBlendMode: false,
    saturate: false,
    sepia: false,
    transitionDelay: false,
    transitionDuration: false,
    transitionProperty: false,
    transitionTimingFunction: false,
  }
}
```

## JavaScript Usage Guidelines

### When to Use Alpine.js

**Acceptable Use Cases**:

- Form validation feedback (before submit)
- Dropdown menus (folder selection)
- Modal dialogs (confirmations)
- Checkbox selection (bulk actions)
- Character count for composition

**Implementation**:

```html
<!-- Dropdown example -->
<div x-data="{ open: false }">
  <button 
    @click="open = !open"
    type="button"
    class="btn-secondary"
  >
    Move to Folder
  </button>
  
  <ul 
    x-show="open"
    x-cloak
    style="border: 2px solid #000; background: #FFF; margin-top: 4px;"
  >
    <li><a href="/mail/move?folder=inbox" style="display: block; padding: 12px;">Inbox</a></li>
    <li><a href="/mail/move?folder=archive" style="display: block; padding: 12px;">Archive</a></li>
  </ul>
</div>

<!-- Hide elements until Alpine loads -->
<style>
  [x-cloak] { display: none !important; }
</style>
```

### Performance Considerations

- Keep Alpine.js usage minimal
- Ensure all functionality works without JavaScript (progressive enhancement)
- Use Alpine.js only for enhancement, not core functionality
- Test with JavaScript disabled

## Accessibility

### ARIA Labels

```html
<button aria-label="Mark email as read">
  <!-- Icon or text -->
</button>

<nav aria-label="Folder navigation">
  <!-- Navigation items -->
</nav>

<main role="main" aria-label="Email list">
  <!-- Email list content -->
</main>
```

### Semantic HTML

```html
<!-- Good semantic structure -->
<article class="email">
  <header>
    <h1>Email Subject</h1>
  </header>
  <section>
    <!-- Email body -->
  </section>
  <footer>
    <!-- Actions -->
  </footer>
</article>
```

### Keyboard Navigation

- All interactive elements must be keyboard accessible
- Logical tab order
- Visible focus indicators (thick outlines)
- Skip links for navigation

```html
<a href="#main-content" class="skip-link" style="position: absolute; top: 0; left: -9999px;">
  Skip to main content
</a>
```

## Performance Optimization

### Image Handling

**Default**: No images loaded automatically

**Implementation**:

```html
<img 
  data-src="image.jpg" 
  alt="Description"
  style="display: none;"
/>
<button onclick="loadImages()">Load Images</button>
```

### CSS Optimization

- Inline critical CSS (above-the-fold)
- Defer non-critical CSS
- Minimize CSS file size (< 30KB)
- Use CSS purging (TailwindCSS)

### HTML Optimization

- Minimize DOM depth
- Clean, semantic markup
- No unnecessary divs
- Keep page size < 50KB

## Testing Checklist

- [ ] Test on actual E-Ink device (Kindle Scribe, Kobo)
- [ ] Verify with JavaScript disabled
- [ ] Check contrast ratios (black on white)
- [ ] Test with screen reader
- [ ] Verify keyboard navigation
- [ ] Test touch interactions (44×44px targets)
- [ ] Check page load time (< 2s on 3G)
- [ ] Verify no animations or transitions
- [ ] Test pagination (no infinite scroll)
- [ ] Verify readable font sizes (16px+)

## Common Pitfalls to Avoid

❌ **Don't**:

- Use subtle grays or low contrast
- Animate anything
- Use hover effects
- Implement infinite scroll
- Use complex JavaScript frameworks
- Load images automatically
- Use fixed positioning
- Create multi-column layouts on mobile
- Use small fonts (< 14px)
- Use small touch targets (< 44×44px)

✅ **Do**:

- Use black and white primarily
- Keep layouts simple and static
- Use pagination
- Test on actual devices
- Prioritize content over design
- Use generous spacing
- Make touch targets large
- Use semantic HTML
- Optimize for performance
- Provide keyboard navigation

## Design Review Checklist

Before implementing any UI component, verify:

- [ ] High contrast (black on white)
- [ ] No animations or transitions
- [ ] Touch-friendly (44×44px minimum)
- [ ] Works without JavaScript
- [ ] Semantic HTML structure
- [ ] Keyboard accessible
- [ ] Simple, linear layout
- [ ] Large, readable text (16px+)
- [ ] Generous spacing
- [ ] Fast rendering (static content)
- [ ] Clear visual hierarchy
- [ ] No complex CSS effects

## Resources

- **Test Devices**: Kindle Scribe, Kobo Elipsa, reMarkable
- **Browser Testing**: Older WebKit/Chromium versions
- **Accessibility**: WAVE, axe DevTools
- **Performance**: Lighthouse, WebPageTest
- **Color Contrast**: WebAIM Contrast Checker
