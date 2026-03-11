/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    './**/*.{razor,cs,html}',
    '../ProjectManagement.Client/**/*.{razor,cs,html}',
    '../../ProjectManagement.Shared/**/*.{cs,razor,html}',
    '../../BlazorMHD.UI/**/*.{razor,cs,html}'
  ],
  safelist: [
    'text-primary',
    'text-success',
    'text-danger',
    'sm:w-[110px]',
    'sm:w-[130px]',
    'sm:w-[160px]',
    'sm:w-[200px]',
    'min-w-[150px]',
    'min-w-[280px]',
    'min-w-[350px]',
    'w-[110px]',
    'w-[130px]',
    'w-[160px]',
    'w-[200px]',
    'dark:bg-gray-800/80',
    'dark:bg-slate-900/95',
    'dark:bg-yellow-500',
    'dark:bg-rose-500',
    'hover:bg-blue-600',
    'hover:bg-green-600',
    'hover:bg-indigo-50',
    'hover:bg-rose-50',
    'hover:bg-rose-700',
    'hover:bg-slate-600',
    'hover:bg-yellow-500',
    'focus:ring-blue-500/40',
    'focus:ring-cyan-500/40',
    'focus:ring-indigo-500/40',
    'focus:ring-rose-500/40',
    'focus:ring-slate-500/40',
    "after:content-['*']",
    {
      pattern: /(bg|text|border|ring)-(slate|gray|blue|indigo|emerald|rose|red|yellow)-(50|100|200|300|400|500|600|700|800|900|950)/,
      variants: ['hover', 'focus', 'active', 'dark', 'dark:hover', 'dark:focus']
    },
    {
      pattern: /bg-(white|black|transparent)/,
      variants: ['hover', 'focus', 'dark', 'dark:hover']
    }
  ],
  theme: {
    extend: {}
  },
  plugins: []
};
