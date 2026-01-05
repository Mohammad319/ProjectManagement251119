namespace ProjectManagement.Shared.Constant
{
    /// <summary>
    /// أنماط الأزرار المستخدمة في التطبيق
    /// </summary>
    public static class CssClasses
    {
        public static class Components
        {
            public const string Card = "bg-white dark:bg-gray-800 rounded-lg shadow p-3";
            public const string ChartWrap = "w-full h-full";
        }

        public static class Btn
        {
            public static class Solid
            {
                public const string Warning = "px-2 py-1 bg-yellow-400 text-black rounded hover:bg-yellow-500 dark:bg-yellow-500 dark:text-black";
                public const string Primary = "px-2 py-1 bg-blue-600 text-white rounded hover:bg-blue-700 dark:bg-blue-500";
                public const string Danger = "px-2 py-1 bg-red-600 text-white rounded hover:bg-red-700 dark:bg-red-500";
                public const string Success = "px-2 py-1 bg-green-600 text-white rounded hover:bg-green-700 dark:bg-green-500";
            }

            public const string Span =
                "text-sm md:text-base " +
                "text-gray-600 dark:text-gray-300 " +
                "p-1 rounded-md " +
                "hover:bg-gray-100 dark:hover:bg-gray-700 " +
                "transition cursor-pointer";

            public static class IconButton
            {
                public const string Base =
                    "inline-flex items-center justify-center w-7 h-7 rounded-full border text-[11px] " +
                    "transition-all duration-150 hover:scale-105 active:scale-95";

                public const string Primary =
                    Base + " border-blue-300 text-blue-600 " +
                    "hover:bg-blue-100 hover:ring-1 hover:ring-blue-300 " +
                    "dark:border-blue-400 dark:text-blue-300 dark:hover:bg-blue-900/40 dark:hover:ring-blue-500";

                public const string Danger =
                    Base + " border-red-300 text-red-600 " +
                    "hover:bg-red-100 hover:ring-1 hover:ring-red-300 " +
                    "dark:border-red-400 dark:text-red-300 dark:hover:bg-red-900/40 dark:hover:ring-red-500";

                public const string Success =
                    Base + " border-green-300 text-green-600 " +
                    "hover:bg-green-100 hover:ring-1 hover:ring-green-300 " +
                    "dark:border-green-400 dark:text-green-300 dark:hover:bg-green-900/40 dark:hover:ring-green-500";

                public const string Secondary =
                    Base + " border-gray-300 text-gray-600 " +
                    "hover:bg-gray-100 hover:ring-1 hover:ring-gray-300 " +
                    "dark:border-gray-500 dark:text-gray-300 dark:hover:bg-gray-800 dark:hover:ring-gray-500";

                public const string Warning =
                    Base + " border-yellow-300 text-yellow-600 " +
                    "hover:bg-yellow-100 hover:ring-1 hover:ring-yellow-300 " +
                    "dark:border-yellow-500 dark:text-yellow-300 dark:hover:bg-yellow-800 dark:hover:ring-yellow-500";
            }

            public static class Outline
            {
                public const string Info =
                    "px-2 py-1 border border-blue-500 text-blue-500 rounded transition " +
                    "hover:bg-blue-500 hover:text-white " +
                    "dark:border-blue-400 dark:text-blue-400 dark:hover:bg-blue-400 dark:hover:text-white ";

                public const string Secondary =
                    "px-2 py-1 border border-gray-500 text-gray-500 rounded transition " +
                    "hover:bg-gray-500 hover:text-white " +
                    "dark:border-gray-400 dark:text-gray-400 dark:hover:bg-gray-400 dark:hover:text-white ";

                public const string Warning =
                    "px-2 py-1 border border-yellow-400 text-yellow-400 rounded transition " +
                    "hover:bg-yellow-400 hover:text-black " +
                    "dark:border-yellow-300 dark:text-yellow-300 dark:hover:bg-yellow-300 dark:hover:text-black ";

                public const string Primary =
                    "px-2 py-1 border border-blue-600 text-blue-600 rounded transition " +
                    "hover:bg-blue-600 hover:text-white " +
                    "dark:border-blue-400 dark:text-blue-400 dark:hover:bg-blue-400 dark:hover:text-white ";

                public const string Danger =
                    "px-2 py-1 border border-red-600 text-red-600 rounded transition " +
                    "hover:bg-red-600 hover:text-white " +
                    "dark:border-red-400 dark:text-red-400 dark:hover:bg-red-400 dark:hover:text-white ";

                public const string Success =
                    "px-2 py-1 border border-green-600 text-green-600 rounded transition " +
                    "hover:bg-green-600 hover:text-white " +
                    "dark:border-green-400 dark:text-green-400 dark:hover:bg-green-400 dark:hover:text-white ";
            }

            public const string Link = "text-[18px] text-neutral-800 underline hover:text-neutral-900 transition dark:text-neutral-300 dark:hover:text-neutral-100 ";

            public static class Size
            {
                public const string Small = "px-1 py-0.5 text-sm";
                public const string XSmall = "px-0.5 py-0 text-sm";
            }
        }

        public static class Grid
        {
            public static class Col
            {
                public const string MD3 = "grid grid-cols-1 md:grid-cols-3 gap-2";
                public const string MD4 = "grid grid-cols-1 md:grid-cols-4 gap-2";
                public const string MD6 = "grid grid-cols-1 md:grid-cols-6 gap-2";
                public const string MD8 = "grid grid-cols-1 md:grid-cols-8 gap-2";
                public const string MD12 = "grid grid-cols-1 md:grid-cols-12 gap-2";
            }

            public static class Span
            {
                public const string MD1 = "md:col-span-1";
                public const string MD2 = "md:col-span-2";
                public const string MD3 = "md:col-span-3";
                public const string MD4 = "md:col-span-4";
                public const string MD5 = "md:col-span-5";
                public const string MD6 = "md:col-span-6";
                public const string MD9 = "md:col-span-9";
                public const string MD10 = "md:col-span-10";
            }
        }

        public static class Input
        {
            public const string Range = "w-full";
            public const string Number = "w-full border border-gray-300 rounded px-1 py-0.5 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white";
            public const string Text = "w-full border border-gray-300 rounded px-1 py-0.5 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white";
            public const string Select = "w-full border border-gray-300 rounded px-2 py-1 bg-white focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-white dark:border-gray-600";
            public const string Color = "w-10 h-10 p-0 border-2 border-gray-300 rounded focus:outline-none mt-3 dark:border-gray-600";
            public const string Date = "w-full border border-gray-300 rounded px-1 py-0.5 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-white";

            public const string Check = "h-4 w-4 text-blue-600 rounded focus:ring-blue-500 dark:text-blue-400";
            public const string Switch = "h-4 w-4 text-blue-600 rounded focus:ring-blue-500 dark:text-blue-400";
        }

        public static class Form
        {
            public const string Label = "block mb-0 font-normal text-sm text-gray-700 dark:text-gray-300";
            public const string RequiredMark = "after:content-['*'] after:ml-1 after:text-red-600 dark:after:text-red-400";
            public const string Field = "flex flex-col gap-1";
            public const string Error = "text-sm text-red-600 dark:text-red-400";

            public const string CheckWrapper = "flex items-center mt-auto gap-2";
            public const string CheckWrapperSpaced = "flex items-center gap-2 mt-1 md:mt-6";

            public const string CheckLabel = "select-none dark:text-white";
            public const string SwitchLabel = "select-none dark:text-white";

            // ✅ إبقاء الاسم القديم لمنع كسر المكونات
            public const string CheckWrapperM = CheckWrapperSpaced;
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

        public static class Spacing
        {
            public static class M
            {
                public const string All2 = "m-2";
                public const string All5 = "m-5";
                public const string Y1 = "my-1";
                public const string Right1 = "mr-1";
                public const string Top1 = "mt-1";
                public const string Top2 = "mt-2";
                public const string Top3 = "mt-3";
                public const string Top4 = "mt-4";
                public const string Bottom1 = "mb-1";
                public const string Bottom2 = "mb-2";
                public const string Bottom3 = "mb-3";
                public const string LeftAuto = "ml-auto";
                public const string Left1 = "ml-1";
                public const string Left2 = "ml-2";
            }

            public static class P
            {
                public const string All2 = "p-2";
                public const string X1 = "px-1";
                public const string Top3 = "pt-3";
                public const string Top0_5 = "pt-0.5";
            }
        }

        public static class Util
        {
            public const string ItemsCenter = "items-center";
            public const string JustifyStart = "justify-start";
            public const string JustifyEnd = "flex justify-end";
            public const string JustifyBetween = "justify-between";
            public const string JustifyCenter = "justify-center";
            public const string TextRight = "text-right";
            public const string PlaceSelfEnd = "place-self-end";
        }

        public static class Layout
        {
            public const string WideContainer = "container mx-auto px-4 max-w-7xl";
            public const string Container = "container mx-auto px-4";
            public const string ButtonGroup = "inline-flex";
            public const string Inline = "inline-flex gap-2";
            public const string FlexJustifyBetween = "flex items-center gap-1 justify-between";

            public const string FlexItemsCenter = "flex items-center gap-1";
            public const string FlexWrap = "flex flex-wrap gap-2";

            public const string FlexMinWidth280 = "flex-1 min-w-[280px]";
            public const string FlexMinWidth350 = "flex-1 min-w-[350px]";
            public const string FlexMinWidth150 = "flex-1 min-w-[150px]";
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
            public const string Base = "mt-1 text-xs";
            public const string Danger = "text-red-600 dark:text-red-400";
            public const string Primary = "text-blue-600 dark:text-blue-400";
            public const string Secondary = "text-gray-600 dark:text-gray-300";
            public const string Center = "text-center";
            public const string Error = Base + " text-rose-600";
            public const string Success = "text-green-600 dark:text-green-400";
            public const string Warning = "text-yellow-600 dark:text-yellow-400";
        }

        public static class Background
        {
            public const string Base = "inline-flex items-center px-2 py-0.5 text-xs font-medium rounded";
            public const string Primary = Base + " bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300";
            public const string White = "bg-white text-black dark:bg-gray-800 dark:text-white";
            public const string Info = "bg-cyan-600 text-white dark:bg-cyan-700 dark:text-white";
            public const string Secondary = "bg-gray-600 text-white dark:bg-gray-700 dark:text-white";
            public const string Light = "bg-gray-100 text-black dark:bg-gray-700 dark:text-white";
            public const string Success = "bg-green-100 text-white dark:bg-green-700 dark:text-white";
            public const string Danger = "bg-red-100 text-white dark:bg-red-700 dark:text-white";
        }

        public const string ActiveClasses = $"{Text.Primary} border-b-2 border-blue-600 dark:border-blue-400 bg-white dark:bg-gray-800";
        public const string InactiveClasses = $"{Text.Secondary} hover:text-black dark:hover:text-white hover:bg-gray-100 dark:hover:bg-gray-700";

        public static class Tabs
        {
            private const string Base = "inline-block px-4 py-2 text-sm font-medium transition-all rounded-t-md ";
            public const string Active = Base + "text-blue-600 border-b-2 border-blue-600 dark:text-blue-400 dark:border-blue-400 bg-white dark:bg-gray-800";
            public const string Inactive = Base + "text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-gray-700";

            public const string UL = "flex mb-1";
        }
    }
}
