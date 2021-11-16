module.exports = {
  purge: {
    enabled: true,
    content: [
      './**/*.html',
      './**/*.razor',
      './**/*.razor.cs',
      './**/*.svg',
      '../**/*.html',
      '../**/*.cshtml',
    ],
    safelist: ['mud-input-label', 'mud-select-input', 'mud-input', 'mud-input-slot' , 'mud-select', 'hsky-select-item', 'hsky-select', 'hsky-textfield', 'mud-input-adornment','mud-tabs-toolbar', 'mud-tooltip-root', 'mud-tabs-toolbar-wrapper', 'mud-tooltip-inline', 'mud-dialog'], // put dynamic class here
  },
  darkMode: 'class', // or 'media' or 'class'
  mode: 'jit',
  important: true,
  theme: {
    extend: {
      colors: {
        'aph-darkblue': '#121921',
        'transparent-dark': 'rgba(46,57,73,0.6)',
        'dark-color': 'rgba(46,57,73)',
        'aph-indigo': '#7A60D4',
        'aph-indigo-darker': '#5B4CA5',
        'aph-blue': '#4C4DF1',
        'aph-blue-darker': '#383EAF',
        'currency': 'rgb(240, 185, 11)',
        'row-color': '#394049',
        'selected-row-color':'#4b3f91'
      },
      fontFamily: {
        'poppins': ['Poppins', 'sans-serif']
      },
      maxWidth: {
        'header': '1024px',
        'base-content': '480px',
      }
    },
  },
  variants: {
    extend: {},
  },
  plugins: [
    require('autoprefixer'),
  ],
}
