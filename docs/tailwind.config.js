/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**.html",
  ],
  theme: {
    extend: {
        fontFamily: {
            'fake': [
                'Inter var',
                'ui-sans-serif',
                'system-ui',
                '-apple-system',
                'BlinkMacSystemFont','Segoe UI'
                ,'Roboto',
                'Helvetica Neue',
                'Arial',
                'Noto Sans',
                'sans-serif',
                'Apple Color Emoji',
                'Segoe UI Emoji',
                'Segoe UI Symbol',
                'Noto Color Emoji'
            ],
          },
    },
  },
  plugins: [
    require('@tailwindcss/typography'),
  ],
}
