/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./**/*.{razor,html,cshtml,cs}",
        "../ProjectManagement/**/*.{razor,html,cshtml,cs}",
        "../ProjectManagement.Client/**/*.{razor,html,cshtml,cs}",
        "../ProjectManagement.Shared/**/*.{razor,html,cshtml,cs}",
        "../BlazorMHD.UI/**/*.{razor,html,cshtml,cs}"
    ],
    darkMode: "class",
    safelist: [
        // arbitrary width/min-width
        "min-w-[150px]", "min-w-[280px]", "min-w-[350px]",
        "w-[110px]", "w-[130px]", "w-[160px]", "w-[200px]",

        // content utilities
        "after:content-['*']",
    ],
    theme: {
        extend: {},
    },
    plugins: [],
};
