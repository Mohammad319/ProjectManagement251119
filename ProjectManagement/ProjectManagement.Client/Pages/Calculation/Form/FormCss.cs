namespace ProjectManagement.Shared.Constant;

public static class FormCss
{
    public static class Btn
    {
        public static class Solid
        {
            public const string Primary = "px-2 py-1 bg-blue-600 text-white rounded hover:bg-blue-700 dark:bg-blue-500";
            public const string Danger = "px-2 py-1 bg-red-600 text-white rounded hover:bg-red-700 dark:bg-red-500";
            public const string Success = "px-2 py-1 bg-green-600 text-white rounded hover:bg-green-700 dark:bg-green-500";
        }

        public static class Outline
        {
            public const string Primary = "px-2 py-1 border border-blue-600 text-blue-600 rounded transition hover:bg-blue-600 hover:text-white dark:border-blue-400 dark:text-blue-400 dark:hover:bg-blue-400 dark:hover:text-white ";
            public const string Secondary = "px-2 py-1 border border-gray-500 text-gray-500 rounded transition hover:bg-gray-500 hover:text-white dark:border-gray-400 dark:text-gray-400 dark:hover:bg-gray-400 dark:hover:text-white ";
            public const string Danger = "px-2 py-1 border border-red-600 text-red-600 rounded transition hover:bg-red-600 hover:text-white dark:border-red-400 dark:text-red-400 dark:hover:bg-red-400 dark:hover:text-white ";
            public const string Success = "px-2 py-1 border border-green-600 text-green-600 rounded transition hover:bg-green-600 hover:text-white dark:border-green-400 dark:text-green-400 dark:hover:bg-green-400 dark:hover:text-white ";
        }
    }

    public static class Grid
    {
        public static class Col
        {
            public const string MD4 = "grid grid-cols-1 md:grid-cols-4 gap-2";
            public const string MD6 = "grid grid-cols-1 md:grid-cols-6 gap-2";
        }

        public static class Span
        {
            public const string MD2 = "md:col-span-2";
            public const string MD3 = "md:col-span-3";
        }
    }

    public static class Input
    {
        public const string Range = "w-full";
        public const string Number = "w-full border border-gray-300 rounded px-1 py-0.5 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white";
        public const string Text = "w-full border border-gray-300 rounded px-1 py-0.5 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white";
        public const string Select = "w-full border border-gray-300 rounded px-2 py-1 bg-white focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-white dark:border-gray-600";
        public const string Date = "w-full border border-gray-300 rounded px-1 py-0.5 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white";
        public const string Check = "h-4 w-4 text-blue-600 rounded focus:ring-blue-500 dark:text-blue-400";
    }

    public static class Form
    {
        public const string Label = "block mb-0 font-normal text-sm text-gray-700 dark:text-gray-300";
        public const string CheckWrapper = "flex items-center mt-auto gap-2";
        public const string CheckWrapperM = "flex items-center gap-2 mt-1 md:mt-6";
        public const string CheckLabel = "select-none dark:text-white";
    }

    public static class Table
    {
        public const string Tab = "min-w-full divide-y divide-gray-200 dark:divide-gray-600";
        public const string Thead = "bg-gray-100 dark:bg-gray-700";
        public const string TH = "p-1 text-left text-sm font-semibold";
        public const string TBody = "divide-y divide-gray-100 hover:divide-gray-200 dark:divide-gray-700";
        public const string Tr = "p-2 hover:bg-gray-100 dark:hover:bg-gray-800";
        public const string Td = "p-1 text-sm text-gray-800 dark:text-gray-300";
    }

    public static class Util
    {
        public const string JustifyCenter = "justify-center";
    }

    public static class Layout
    {
        public const string ButtonGroup = "inline-flex";
        public const string FlexItemsCenter = "flex items-center gap-1";
        public const string FlexWrap = "flex flex-wrap gap-2";
        public const string FlexMinWidth350 = "flex-1 min-w-[350px]";
        public const string FlexWidth160 = "w-[160px]";
        public const string FlexWidth130 = "w-[130px]";
        public const string FlexWidth110 = "w-[110px]";
        public const string FlexWidth200 = "w-[200px]";
    }

    public static class Modal
    {
        public const string Body = "space-y-0 ";
        public const string Footer = "mt-1 flex justify-end gap-1";
    }

    public static class Text
    {
        public const string Danger = "text-red-600 dark:text-red-400";
        public const string Warning = "text-yellow-600 dark:text-yellow-400";
    }
}
