/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./**/*.{razor,html,cshtml,cs}",
    ],
    darkMode: "class",
    safelist: [
        "w-[90vw]",
        "h-[90vh]",
        "h-[92vh]",
        "w-[222px]",
        "max-w-5xl",
        "max-w-[1800px]",
        "tracking-[0.2em]",
        "backdrop-blur-[2px]",
    ],
    theme: {
        extend: {},
    },
    plugins: [],
};
