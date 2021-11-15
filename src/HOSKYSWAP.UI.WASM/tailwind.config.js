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
    safelist: [], // put dynamic class here
  },
  darkMode: 'class', // or 'media' or 'class'
  mode: 'jit',
  important: true,
  theme: {
    extend: {},
  },
  variants: {
    extend: {},
  },
  plugins: [
    require('autoprefixer'),
  ],
}
