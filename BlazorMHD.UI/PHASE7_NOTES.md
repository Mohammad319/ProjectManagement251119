# BlazorMHD.UI - Phase 7 (Accessibility, RTL, UX hardening)

## What was improved

- Added robust DI extension method `AddBlazorMhdUI()` while keeping old alias `BlazorMHD()` for compatibility.
- Unified host shells (`MHD.razor`, `MhdUiHost.razor`) with `dir="auto"` and feedback stack consistency.
- Added localization fallback helpers + direction helper (`Shared/Localiztion.Extensions.cs`).
- Upgraded Tabs with keyboard navigation (`Home/End/Arrows/Enter`), ARIA roles, RTL-aware arrow logic, better focus styles.
- Upgraded TreeView with ARIA tree semantics, keyboard navigation, expand/collapse semantics, focused item management.
- Upgraded Toggle to native `<button role="switch">` with keyboard support.
- Upgraded AutoComplete with stronger combobox/listbox ARIA attributes and active descendant handling.
- Fixed text/search input two-way binding flow to invoke `ValueChanged` reliably.
- Numeric input accessibility and attribute filtering improvements.
- Table:
  - localization fallback via `.Get(...)`
  - accessible sorting (`aria-sort`, action labels)
  - header/cell class support from column definitions
  - row key support and direction support
  - safer paging rebuild behavior
- Dialog:
  - fixed icon render condition bug
  - improved ARIA (`aria-labelledby`, `aria-describedby`)
  - keyboard behavior (`Esc`, arrow nudging)
  - deterministic service-sync rendering
- Toast / MessageBox / Loading overlays:
  - implemented `IDisposable` cleanup
  - stronger ARIA live regions
  - improved dark-mode contrast classes
- Splitters (horizontal + vertical):
  - added keyboard resize (`Arrow keys`)
  - ARIA separator semantics
  - persisted size restore (horizontal now also restores)
  - disposal cleanup
- CSS animation improvements:
  - added toast progress animation
  - reduced-motion handling
  - global helpers appended to `mhd.css`

## Changed files

- Core/Services/DependencyInjection.cs
- MHD.razor
- Components/MhdUiHost.razor
- Shared/Localiztion.Extensions.cs
- Components/Navigation/Tabs/MhdTabs.razor
- Components/Data/TreeView/MhdTreeView.razor
- Components/Inputs/Toggle/MhdToggle.razor
- Components/Inputs/AutoComplete/MhdAutoComplete.razor
- Components/Inputs/TextBox/MhdTextInput.razor
- Components/Inputs/Search/MhdSearchBox.razor
- Components/Inputs/Numeric/MhdNumericInput.razor
- Components/Data/Table/MhdTable.razor
- Components/Data/Table/MhdTableColumn.razor
- Components/Feedback/Dialog/MhdDialog.razor
- Components/Feedback/Dialog/MhdDialog.razor.css
- Components/Feedback/Toast/MhdToastContainer.razor
- Components/Feedback/MessageBox/MhdMessageBox.razor
- Components/Feedback/Loading/MhdLoadingOverlay.razor
- Components/Feedback/Splitter/HorizontalSplitter.razor
- Components/Feedback/Splitter/VerticalSplitter.razor
- wwwroot/css/animations.css
- wwwroot/css/mhd.css
