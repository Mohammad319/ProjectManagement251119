# Components Dark Mode Hotfix 3

This hotfix targets the components that still showed light surfaces or browser-default controls in dark mode.

## Fixed files
- AppUser/UpdateUserUI.razor
- Property/PropertyGroups/PropertyGroupsFormUI.razor
- ResourceUI/ResourceFormDLUI.razor
- LogUI.razor

## What was fixed
- Strengthened dark card backgrounds using `dark:!bg-*` and `dark:!border-*`
- Added native control theme hints using `[color-scheme:light] dark:[color-scheme:dark]`
- Fixed `LogUI` controls that accidentally had layout classes like `space-y-1.5` instead of real input styles
- Improved dark text contrast inside the logs table

## Notes
If some dialog shells are still light, the remaining issue is probably in the dialog host component or global CSS outside these component files.
