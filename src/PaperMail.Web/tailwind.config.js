/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Pages/**/*.{cshtml,cs}",
    "./Views/**/*.{cshtml,cs}",
  ],
  theme: {
    extend: {
      colors: {
        'eink': {
          'black': '#000000',
          'white': '#FFFFFF',
          'gray-light': '#CCCCCC',
          'gray': '#808080',
          'gray-dark': '#404040',
        }
      },
      fontFamily: {
        'sans': ['system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'sans-serif'],
        'mono': ['Consolas', 'Monaco', 'Courier New', 'monospace'],
      },
      spacing: {
        'touch': '44px', // Minimum touch target for accessibility
      },
    },
  },
  // Disable unused features to reduce CSS size
  corePlugins: {
    animation: false,
    backdropBlur: false,
    backdropBrightness: false,
    backdropContrast: false,
    backdropGrayscale: false,
    backdropHueRotate: false,
    backdropInvert: false,
    backdropOpacity: false,
    backdropSaturate: false,
    backdropSepia: false,
    blur: false,
    brightness: false,
    contrast: false,
    dropShadow: false,
    grayscale: false,
    hueRotate: false,
    invert: false,
    saturate: false,
    sepia: false,
    transitionProperty: false,
    transitionDelay: false,
    transitionDuration: false,
    transitionTimingFunction: false,
  },
  plugins: [],
}
