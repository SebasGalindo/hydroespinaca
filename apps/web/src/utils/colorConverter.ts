/**
 * Utility to convert modern CSS color functions (oklch, oklab) to rgb
 * This is needed for html2canvas compatibility since it doesn't support CSS Color Level 4
 */

/**
 * Converts OKLCH color to RGB
 * OKLCH format: oklch(L C H [/ alpha])
 * L: lightness (0-1 or 0%-100%)
 * C: chroma (0-0.4 typically)
 * H: hue (0-360 degrees)
 */
function oklchToRgb(l: number, c: number, h: number, alpha: number = 1): string {
  // Convert OKLCH to OKLAB
  const a = c * Math.cos((h * Math.PI) / 180);
  const b = c * Math.sin((h * Math.PI) / 180);

  // Convert OKLAB to linear RGB
  const l_ = l + 0.3963377774 * a + 0.2158037573 * b;
  const m_ = l - 0.1055613458 * a - 0.0638541728 * b;
  const s_ = l - 0.0894841775 * a - 1.2914855480 * b;

  const l3 = l_ * l_ * l_;
  const m3 = m_ * m_ * m_;
  const s3 = s_ * s_ * s_;

  const r = +4.0767416621 * l3 - 3.3077115913 * m3 + 0.2309699292 * s3;
  const g = -1.2684380046 * l3 + 2.6097574011 * m3 - 0.3413193965 * s3;
  const b_ = -0.0041960863 * l3 - 0.7034186147 * m3 + 1.7076147010 * s3;

  // Convert to sRGB (gamma correction)
  const toSrgb = (c: number) => {
    const abs = Math.abs(c);
    if (abs <= 0.0031308) {
      return Math.sign(c) * 12.92 * abs;
    }
    return Math.sign(c) * (1.055 * Math.pow(abs, 1 / 2.4) - 0.055);
  };

  const rSrgb = Math.max(0, Math.min(1, toSrgb(r)));
  const gSrgb = Math.max(0, Math.min(1, toSrgb(g)));
  const bSrgb = Math.max(0, Math.min(1, toSrgb(b_)));

  const r255 = Math.round(rSrgb * 255);
  const g255 = Math.round(gSrgb * 255);
  const b255 = Math.round(bSrgb * 255);

  if (alpha < 1) {
    return `rgba(${r255}, ${g255}, ${b255}, ${alpha})`;
  }
  return `rgb(${r255}, ${g255}, ${b255})`;
}

/**
 * Parse OKLCH string and convert to RGB
 * Handles formats:
 * - oklch(0.5 0.1 180)
 * - oklch(50% 0.1 180deg)
 * - oklch(0.5 0.1 180 / 0.5)
 */
function parseOklch(colorString: string): string | null {
  const match = colorString.match(
    /oklch\(\s*([0-9.]+%?)\s+([0-9.]+)\s+([0-9.]+)(?:deg)?\s*(?:\/\s*([0-9.]+%?))?\s*\)/i
  );

  if (!match) return null;

  let l = parseFloat(match[1]!);
  if (match[1]!.endsWith('%')) {
    l = l / 100;
  }

  const c = parseFloat(match[2]!);
  const h = parseFloat(match[3]!);

  let alpha = 1;
  if (match[4]) {
    alpha = parseFloat(match[4]);
    if (match[4].endsWith('%')) {
      alpha = alpha / 100;
    }
  }

  return oklchToRgb(l, c, h, alpha);
}

/**
 * Parse OKLAB string and convert to RGB
 * Handles formats:
 * - oklab(0.5 0.1 -0.1)
 * - oklab(50% 0.1 -0.1 / 0.5)
 */
function parseOklab(colorString: string): string | null {
  const match = colorString.match(
    /oklab\(\s*([0-9.-]+%?)\s+([0-9.-]+)\s+([0-9.-]+)\s*(?:\/\s*([0-9.]+%?))?\s*\)/i
  );

  if (!match) return null;

  let l = parseFloat(match[1]!);
  if (match[1]!.endsWith('%')) {
    l = l / 100;
  }

  const a = parseFloat(match[2]!);
  const b = parseFloat(match[3]!);

  let alpha = 1;
  if (match[4]) {
    alpha = parseFloat(match[4]);
    if (match[4].endsWith('%')) {
      alpha = alpha / 100;
    }
  }

  // Convert OKLAB to linear RGB (same as above but we already have a, b)
  const l_ = l + 0.3963377774 * a + 0.2158037573 * b;
  const m_ = l - 0.1055613458 * a - 0.0638541728 * b;
  const s_ = l - 0.0894841775 * a - 1.2914855480 * b;

  const l3 = l_ * l_ * l_;
  const m3 = m_ * m_ * m_;
  const s3 = s_ * s_ * s_;

  const r = +4.0767416621 * l3 - 3.3077115913 * m3 + 0.2309699292 * s3;
  const g = -1.2684380046 * l3 + 2.6097574011 * m3 - 0.3413193965 * s3;
  const b_ = -0.0041960863 * l3 - 0.7034186147 * m3 + 1.7076147010 * s3;

  // Convert to sRGB (gamma correction)
  const toSrgb = (c: number) => {
    const abs = Math.abs(c);
    if (abs <= 0.0031308) {
      return Math.sign(c) * 12.92 * abs;
    }
    return Math.sign(c) * (1.055 * Math.pow(abs, 1 / 2.4) - 0.055);
  };

  const rSrgb = Math.max(0, Math.min(1, toSrgb(r)));
  const gSrgb = Math.max(0, Math.min(1, toSrgb(g)));
  const bSrgb = Math.max(0, Math.min(1, toSrgb(b_)));

  const r255 = Math.round(rSrgb * 255);
  const g255 = Math.round(gSrgb * 255);
  const b255 = Math.round(bSrgb * 255);

  if (alpha < 1) {
    return `rgba(${r255}, ${g255}, ${b255}, ${alpha})`;
  }
  return `rgb(${r255}, ${g255}, ${b255})`;
}

/**
 * Convert any CSS color string containing oklch() or oklab() to rgb()
 * Preserves other color formats unchanged
 */
export function convertModernColorsToRgb(colorString: string): string {
  if (!colorString) return colorString;

  // Handle oklch
  if (colorString.includes('oklch(')) {
    const converted = parseOklch(colorString);
    if (converted) return converted;
  }

  // Handle oklab
  if (colorString.includes('oklab(')) {
    const converted = parseOklab(colorString);
    if (converted) return converted;
  }

  // Return unchanged if no modern color format detected
  return colorString;
}

/**
 * Process an HTMLElement to convert all oklch/oklab colors to rgb
 * Modifies the element's inline styles in place
 */
export function convertElementColors(element: HTMLElement): void {
  const style = element.style;

  // List of CSS properties that can contain colors
  const colorProperties = [
    'color',
    'backgroundColor',
    'borderColor',
    'borderTopColor',
    'borderRightColor',
    'borderBottomColor',
    'borderLeftColor',
    'outlineColor',
    'textDecorationColor',
    'fill',
    'stroke',
  ];

  colorProperties.forEach((prop) => {
    const value = style.getPropertyValue(prop);
    if (value && (value.includes('oklch(') || value.includes('oklab('))) {
      const converted = convertModernColorsToRgb(value);
      style.setProperty(prop, converted);
    }
  });
}

/**
 * Process an entire DOM tree to convert all modern color formats
 * Used before passing to html2canvas
 */
export function convertDOMColors(rootElement: HTMLElement | Document): void {
  const elements = rootElement.querySelectorAll('*');

  elements.forEach((el) => {
    if (el instanceof HTMLElement) {
      // First, try to get computed styles and apply them as inline
      try {
        const computed = window.getComputedStyle(el);

        // Convert background color
        const bgColor = computed.backgroundColor;
        if (bgColor && (bgColor.includes('oklch(') || bgColor.includes('oklab('))) {
          el.style.backgroundColor = convertModernColorsToRgb(bgColor);
        }

        // Convert text color
        const color = computed.color;
        if (color && (color.includes('oklch(') || color.includes('oklab('))) {
          el.style.color = convertModernColorsToRgb(color);
        }

        // Convert border color
        const borderColor = computed.borderColor;
        if (borderColor && (borderColor.includes('oklch(') || borderColor.includes('oklab('))) {
          el.style.borderColor = convertModernColorsToRgb(borderColor);
        }
      } catch (e) {
        // If computed styles fail, just convert inline styles
        convertElementColors(el);
      }
    }
  });
}
