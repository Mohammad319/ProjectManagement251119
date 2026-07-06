namespace ProjectManagement.Client.Shared.Constants
{
    public static class Icons
    {
        const string SVGHeader = "xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 24 24'";
        // Task
        public const string NewTask = "🧩";            // U+1F4DD
        public const string NewSubTask = "🧩";         // U+1F9E9
        public const string NewResource = "👷";       // مزيج من U+1F464 + ➕
        public const string Legal = "📜";       // مزيج من U+1F464 + ➕

        // Import
        public const string ImportFromFile = @$"<svg {SVGHeader} fill='currentColor'>
      <path d='M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6zM13 3.5L18.5 9H13V3.5zM12 19l-5-5h3v-4h4v4h3l-5 5z'/>
    </svg>";
        public const string ImportFromCloud = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2'
             stroke-linecap='round' stroke-linejoin='round'><path d='M20.88 18.09A5 5 0 0 0 18 9h-1.26A8 8 0 1 0 4 16.29' />
                <line x1='12' y1='12' x2='12' y2='21' /><polyline points='8 17 12 21 16 17' /></svg>";

        // Price lists: a price-tag glyph (no cloud/sync/upload connotations).
        public const string PriceTag = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
                <path d='M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z' />
                <line x1='7' y1='7' x2='7.01' y2='7' /></svg>";

        // Support / help (question mark in a circle).
        public const string Support = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
                <circle cx='12' cy='12' r='10' />
                <path d='M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3' />
                <line x1='12' y1='17' x2='12.01' y2='17' /></svg>";
        // Permissions
        public const string PermissionKey = "🔑";      // U+1F511
        public const string PermissionShield = "🛡️";  // U+1F6E1
        public const string Tender = "📨";   // U+1F513
        public const string Copy = "📋";                // U+1F4CB
        public const string Cut = "✂️";                 // U+2702
        public const string Paste = "📥";               // U+1F4E5
        public const string Duplicate = "🔁";           // U+1F501
        public const string SaveCloud = @$"<svg {SVGHeader} fill='currentColor'>
      <path d='M17.657 16H19a3 3 0 0 0 0-6 4.992 4.992 0 0 0-4.9-4A6 6 0 0 0 4 10a4 4 0 0 0 0 8h5v-2H4a2 2 0 1 1 0-4h.586l.707-.707A6.002 6.002 0 0 1 17.657 16zm-5.657 0v-4h2l-3-3-3 3h2v4h2zm-2 2v2h4v-2h-4z'/></svg>";
        public const string ReorderRows = @$"<svg {SVGHeader} fill='none'>
  <path d='M12 4L12 20M12 4L18 10M12 4L6 10M12 20L6 14M12 20L18 14' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/>
</svg>";
        public const string HumanResources = "👥";       // U+1F465
        public const string Machines = "🛠️";            // U+1F6E0
        public const string Materials = "🧱";            // U+1F9F1
        public const string Edit = "✏️";                 // U+270F (Pencil)
        public const string Delete = "❌";//"🗑️";               // U+1F5D1 (Trash Bin)
        public const string Details = "ℹ️";              // U+2139 (Information)
        public const string DeleteSVG = @"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' class='w-5 h-5'>
  <g fill='none' stroke='currentColor' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'>
    <path d='M8 7.2A1.2 1.2 0 0 1 9.2 6h5.6A1.2 1.2 0 0 1 16 7.2V8H8v-.8z' />
    <path d='M4.5 8h15' />
    <path d='M7.2 8.5 8 18.7A2 2 0 0 0 10 20.5h4a2 2 0 0 0 2-1.8l.8-10.2' />
    <path d='M11 11v5.5' />
    <path d='M13 11v5.5' />
  </g>
</svg>";
        public const string Template = "🎨";              // U+1F5C2 (Template)
        public const string Refresh = "🔄";              // U+1F504 (Refresh) 🔄
        public const string Refresh2 = "↻";              // U+1F504 (Refresh) ↻
        public const string RefreshSvg = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <path d='M21 12a9 9 0 0 1-15.3 6.4' />
  <path d='M3 12A9 9 0 0 1 18.3 5.6' />
  <polyline points='21 5 21 11 15 11' />
  <polyline points='3 19 3 13 9 13' />
</svg>";
        public const string Active = "🟢";              // U+1F7E2 (Refresh) ↻
        public const string NotActive = "🔴";              // U+1F534 (Refresh) ↻
        public const string NotSelected = "⬜";              // U+1F534 (NotSelected) ⬜
        public const string ResetQuantity = "🔢";              // U+1F522 (ResetQuantity) 🔢
        public const string List = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <line x1='8' y1='6' x2='21' y2='6' />
  <line x1='8' y1='12' x2='21' y2='12' />
  <line x1='8' y1='18' x2='21' y2='18' />
  <line x1='3' y1='6' x2='3' y2='6' />
  <line x1='3' y1='12' x2='3' y2='12' />
  <line x1='3' y1='18' x2='3' y2='18' />
</svg>";
        public const string Person = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <path d='M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2' />
  <circle cx='12' cy='7' r='4' />
</svg>";
        public const string Settings = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <circle cx='12' cy='12' r='3' />
  <path d='M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09a1.65 1.65 0 0 0-1-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09a1.65 1.65 0 0 0 1.51-1 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9c0 .66.39 1.26 1 1.51H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z' />
</svg>";
        public const string lightMode = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <circle cx='12' cy='12' r='5' />
  <line x1='12' y1='1' x2='12' y2='3' />
  <line x1='12' y1='21' x2='12' y2='23' />
  <line x1='4.22' y1='4.22' x2='5.64' y2='5.64' />
  <line x1='18.36' y1='18.36' x2='19.78' y2='19.78' />
  <line x1='1' y1='12' x2='3' y2='12' />
  <line x1='21' y1='12' x2='23' y2='12' />
  <line x1='4.22' y1='19.78' x2='5.64' y2='18.36' />
  <line x1='18.36' y1='5.64' x2='19.78' y2='4.22' />
</svg>";
        public const string DarkMode = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <path d='M21 12.79A9 9 0 1 1 11.21 3a7 7 0 0 0 9.79 9.79z' />
</svg>";

        public const string Sun = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <circle cx='12' cy='12' r='5' />
  <line x1='12' y1='1' x2='12' y2='3' />
  <line x1='12' y1='21' x2='12' y2='23' />
  <line x1='4.22' y1='4.22' x2='5.64' y2='5.64' />
  <line x1='18.36' y1='18.36' x2='19.78' y2='19.78' />
  <line x1='1' y1='12' x2='3' y2='12' />
  <line x1='21' y1='12' x2='23' y2='12' />
  <line x1='4.22' y1='19.78' x2='5.64' y2='18.36' />
  <line x1='18.36' y1='5.64' x2='19.78' y2='4.22' />
</svg>";
        public const string Moon = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <path d='M21 12.79A9 9 0 1 1 11.21 3a7 7 0 0 0 9.79 9.79z' />
</svg>";
        public const string Logout = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <path d='M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4' />
  <polyline points='16 17 21 12 16 7' />
  <line x1='21' y1='12' x2='9' y2='12' />
</svg>";
        public const string ChevronTop = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linejoin='round'>
  <polyline points='18 15 12 9 6 15' />
</svg>";
        public const string ChevronBottom = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linejoin='round'>
  <polyline points='6 9 12 15 18 9' />
</svg>";
        public const string Check = @"
<svg xmlns='http://www.w3.org/2000/svg'
     viewBox='0 0 20 20'
     fill='currentColor'
     class='w-4 h-4'>
  <path fill-rule='evenodd'
        d='M16.707 5.293a1 1 0 0 1 0 1.414l-7.25 7.25a1 1 0 0 1-1.414 0l-3.25-3.25a1 1 0 1 1 1.414-1.414L8.5 11.586l6.543-6.543a1 1 0 0 1 1.414 0z'
        clip-rule='evenodd' />
</svg>";
        public const string Plus = @$"<svg {SVGHeader}
    
     fill='none' stroke='currentColor'
     stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <line x1='12' y1='5' x2='12' y2='19' />
  <line x1='5' y1='12' x2='19' y2='12' /></svg>";              // U+1F522 (ResetQuantity) 🔢
        public const string ChevronRight = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <polyline points='9 18 15 12 9 6' />
</svg>";
        public const string Filter = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <polygon points='22 3 2 3 10 12 10 19 14 21 14 12 22 3' />
</svg>";
        public const string FilterOff = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <path d='M22 3 13.5 12v7L10 21v-9L2 3h20z' />
  <line x1='4' y1='20' x2='20' y2='4' />
</svg>";

        public const string Back = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <polyline points='15 18 9 12 15 6' />
</svg>";
        public const string EyeIcon = @"
<svg viewBox='0 0 24 24' width='24' height='24'>
    <path d='M2 12C4.5 7 8 4 12 4s7.5 3 10 8c-2.5 5-6.5 8-10 8S4.5 17 2 12z'
          fill='none' stroke='currentColor' stroke-width='2' />
    <circle cx='12' cy='12' r='3'
            fill='none' stroke='currentColor' stroke-width='2' />
</svg>";
        public const string EyeOffIcon = @"<svg viewBox='0 0 24 24' width='24' height='24'>
        <path d='M2 12C4.5 7 8 4 12 4s7.5 3 10 8c-2.5 5-6.5 8-10 8S4.5 17 2 12z'
              fill='none' stroke='currentColor' stroke-width='2' />
        <circle cx='12' cy='12' r='3'
                fill='none' stroke='currentColor' stroke-width='2' />
        <line x1='3' y1='3' x2='21' y2='21'
              stroke='currentColor' stroke-width='2' />
    </svg>";
        public const string Chain = @$"<svg xmlns='http://www.w3.org/2000/svg' width='15' height='15' fill='none' viewBox='0 0 24 24' stroke-width='1.5' stroke='currentColor' class='size-6'>
  <path stroke-linecap='round' stroke-linejoin='round' d='M13.19 8.688a4.5 4.5 0 0 1 1.242 7.244l-4.5 4.5a4.5 4.5 0 0 1-6.364-6.364l1.757-1.757m13.35-.622 1.757-1.757a4.5 4.5 0 0 0-6.364-6.364l-4.5 4.5a4.5 4.5 0 0 0 1.242 7.244' />
</svg>";
        public const string ChinUnLink = @$"<svg xmlns='http://www.w3.org/2000/svg' width='15' height='15' fill='none' viewBox='0 0 24 24' stroke-width='1.5' stroke='currentColor' class='size-6'>
  <path stroke-linecap='round' stroke-linejoin='round' d='M13.181 8.68a4.503 4.503 0 0 1 1.903 6.405m-9.768-2.782L3.56 14.06a4.5 4.5 0 0 0 6.364 6.365l3.129-3.129m5.614-5.615 1.757-1.757a4.5 4.5 0 0 0-6.364-6.365l-4.5 4.5c-.258.26-.479.541-.661.84m1.903 6.405a4.495 4.495 0 0 1-1.242-.88 4.483 4.483 0 0 1-1.062-1.683m6.587 2.345 5.907 5.907m-5.907-5.907L8.898 8.898M2.991 2.99 8.898 8.9' />
</svg>";
        public const string Minus = "➖";              // U+1F522 (ResetQuantity) 🔢
        public const string Plus2 = "➕";
        // Crisp single plus glyph for "Lägg till" buttons (the ➕ emoji renders inconsistently).
        public const string PlusSVG = @"<svg xmlns='http://www.w3.org/2000/svg' width='14' height='14' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2.5' stroke-linecap='round'><line x1='12' y1='5' x2='12' y2='19'/><line x1='5' y1='12' x2='19' y2='12'/></svg>";
        public static string Search = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <circle cx='11' cy='11' r='8' />
  <line x1='21' y1='21' x2='16.65' y2='16.65' /></svg>";
        public const string Department = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <rect x='2' y='3' width='6' height='6' />
  <rect x='16' y='3' width='6' height='6' />
  <rect x='9' y='15' width='6' height='6' />
  <line x1='5' y1='9' x2='5' y2='15' />
  <line x1='19' y1='9' x2='19' y2='15' />
  <line x1='5' y1='15' x2='19' y2='15' />
</svg>";
        public const string Archive = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <polyline points='21 8 21 21 3 21 3 8' />
  <rect x='1' y='3' width='22' height='5' />
  <line x1='10' y1='12' x2='14' y2='12' />
</svg>";
        public const string Restore = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <polyline points='1 4 1 10 7 10' />
  <path d='M3.51 15a9 9 0 1 0 .49-4.74L1 10' />
</svg>";
        public const string Folder = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <path d='M3 7a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z' /></svg>";
        public const string ChevronLeft = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <polyline points='15 18 9 12 15 6' />
</svg>";
        public const string ChevronUp = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <polyline points='18 15 12 9 6 15' />
</svg>";
        public const string ChevronDown = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <polyline points='6 9 12 15 18 9' />
</svg>";
        public const string Locked = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <rect x='3' y='11' width='18' height='11' rx='2' ry='2' />
  <path d='M7 11V7a5 5 0 0 1 10 0v4' />
</svg>";
        public const string UnLocked = @$"<svg {SVGHeader} fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
  <rect x='3' y='11' width='18' height='11' rx='2' ry='2' /><path d='M17 11V7a5 5 0 0 0-9.9-1' /></svg>";
        public const string SE = @$"<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 16 10'>
  <rect width='16' height='10' fill='#005cbf'/>
  <rect x='5' width='2' height='10' fill='#ffd700'/>
  <rect y='4' width='16' height='2' fill='#ffd700'/>
</svg>";

        public const string EN = @$"<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 60 30'>
  <rect width='60' height='30' fill='#012169'/>
  <path d='M0,0 L60,30 M60,0 L0,30' stroke='#fff' stroke-width='6'/>
  <path d='M0,0 L60,30 M60,0 L0,30' stroke='#C8102E' stroke-width='4' />
  <path d='M30,0 v30 M0,15 h60' stroke='#fff' stroke-width='10'/>
  <path d='M30,0 v30 M0,15 h60' stroke='#C8102E' stroke-width='6'/>
</svg>";

        public const string Company = @$"<svg {SVGHeader} fill='currentColor'>
  <path d='M3 21V3h18v18h-7v-5H10v5H3zm2-2h3v-5h6v5h3V5H5v14zm4-8h2V9H9v2zm4 0h2V9h-2v2zm-4-4h2V5H9v2zm4 0h2V5h-2v2z'/>
</svg>";
        public const string Storage = @$"<svg {SVGHeader} fill='currentColor'>
  <path d='M12 2C6.48 2 2 4.69 2 8v8c0 3.31 4.48 6 10 6s10-2.69 10-6V8c0-3.31-4.48-6-10-6zm0 2c4.42 0 8 1.79 8 4s-3.58 4-8 4-8-1.79-8-4 3.58-4 8-4zm0 16c-4.42 0-8-1.79-8-4v-2c1.67 1.33 4.51 2 8 2s6.33-.67 8-2v2c0 2.21-3.58 4-8 4z'/>
</svg>";
        public static string IsLocked(bool isLock)
        {
            return isLock ? Locked : UnLocked;
        }
        public static string GetVisibleIcon(bool isVisible)
        {
            return isVisible ? EyeIcon : EyeOffIcon;
        }
        public static string RightBottom(bool isVisible)
        {
            return isVisible ? ChevronBottom : ChevronTop;
        }
        public static string TopBottom(bool isTop)
        {
            return isTop ? ChevronTop : ChevronBottom;
        }
        public static string AddDelete(bool isTop)
        {
            return isTop ? Minus : Plus2;
        }
    public const string Mouse = """
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"
     fill="none" stroke="currentColor" stroke-width="1.5"
     stroke-linecap="round" stroke-linejoin="round"
     class="h-6 w-6">
  <rect x="7" y="2" width="10" height="20" rx="5" ry="5" />
  <line x1="12" y1="6" x2="12" y2="10" />
</svg>
""";
        public const string DotsVertical = """
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"
     fill="currentColor" class="h-4 w-4">
  <circle cx="12" cy="5" r="2" />
  <circle cx="12" cy="12" r="2" />
  <circle cx="12" cy="19" r="2" />
</svg>
""";
    }
    }
