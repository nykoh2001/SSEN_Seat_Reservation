# Instructions for coding agents

This repository is a small WPF application developed by a team. Keep the architecture simple, but make ownership of UI, state, and application behavior clear. Apply these rules to new code; follow an established project convention when one already exists, and do not restructure unrelated code for a small change.

## Architecture and responsibilities

- Use lightweight MVVM for screens with state or actions. A View (`.xaml`) defines layout and presentation; its ViewModel exposes bindable state and commands. Put file, database, network, and other application operations in a service when they warrant a separate responsibility.
- Keep business logic and data access out of View code-behind. Code-behind may handle view-only work such as focus, animations, and direct window or control interaction.
- Use WPF `{Binding}` and a clearly assigned `DataContext` to connect a View to its ViewModel. Prefer `ICommand` for user actions. Do not use WinUI's `{x:Bind}` syntax in WPF.
- Notify the UI when bound properties change (`INotifyPropertyChanged`). Use `ObservableCollection<T>` when additions or removals must appear in a bound list; item property changes still need property-change notification.
- Keep long-running work off the UI thread. Use `async`/`await` for asynchronous operations and marshal direct UI access back to the UI dispatcher when necessary.
- Use the existing MVVM package if the project has one. If introducing `CommunityToolkit.Mvvm`, use only the needed features, such as `ObservableObject` and relay commands. Do not add a DI framework, custom base class, extra project, or interface solely to satisfy a pattern.

## Project layout

Use the existing layout if the repository already has one. For a new small project, start with this structure and create optional directories only when files need them:

```text
App.xaml
App.xaml.cs
Views/
  MainWindow.xaml
  MainWindow.xaml.cs
ViewModels/
  MainViewModel.cs
Resources/
  Styles.xaml
.editorconfig
README.md
```

- Add `Models/` for actual domain or data types and `Services/` for separate application operations; do not create empty placeholder directories.
- Keep a View and its ViewModel easy to identify by name. Split a screen into `UserControl`s when it has meaningful, independently maintained sections, not merely to increase the number of files.
- Set the View's `DataContext` at a clear construction or composition point. Keep that choice consistent across screens.

## XAML and styles

- Put shared brushes, dimensions, styles, and templates in `Resources/Styles.xaml` and merge it through `App.xaml`. Keep styles used by only one View in that View's resources.
- Use a named style for a specific role, such as `PrimaryButtonStyle`. Use an implicit style only when every control of that type in its scope should receive it.
- Prefer `{StaticResource ...}` for stable resources. Use `{DynamicResource ...}` when a resource must change at runtime, such as a switchable theme.
- Use `Style` for shared property values, `DataTemplate` for presenting data, and `ControlTemplate` when changing a control's visual structure. Avoid replacing a control template for a simple color or spacing change.
- Prefer adaptable layout containers such as `Grid` over absolute coordinates and fixed sizes for ordinary screens.

## Naming conventions

Follow `.editorconfig` where it defines a rule. Otherwise use these team defaults:

| Element | Convention | Example |
| --- | --- | --- |
| Class, record, enum, namespace | PascalCase; descriptive noun | `FileService`, `SaveResult` |
| Interface | `I` + PascalCase | `IFileService` |
| View and ViewModel | Matching feature names; `View` or `Window` and `ViewModel` suffixes | `SettingsView` / `SettingsViewModel` |
| Method | PascalCase; verb or verb phrase | `SaveSettings()` |
| Asynchronous method | Verb phrase ending in `Async` | `SaveSettingsAsync()` |
| Property | PascalCase; name the value | `SelectedFilePath` |
| Boolean property | Positive state or capability | `IsLoading`, `HasChanges`, `CanSave` |
| Collection property | Plural name for its items | `RecentFiles` |
| Command property | Action + `Command` | `SaveCommand` |
| Private instance field | `_camelCase` | `_fileService` |
| Private static field | `s_camelCase` | `s_cache` |
| Parameter and local variable | camelCase | `filePath` |
| Constant and enum member | PascalCase | `DefaultTimeout`, `SaveResult.Success` |
| XAML `x:Name` | Purpose + control type; only when a name is needed | `SearchTextBox`, `SaveButton` |
| Style resource key | Purpose + control type + `Style` | `PrimaryButtonStyle` |
| Brush resource key | Purpose + `Brush` | `AccentBrush` |
| Template resource key | Purpose + `Template` | `FileItemTemplate` |
| Converter class | Conversion purpose + `Converter` | `StatusToBrushConverter` |
| View-only event handler | Element name + `_` + event name | `SaveButton_Click` |

- Match a C# type's file name to the type name and keep namespaces aligned with the project and folder structure.
- Prefer clear words over abbreviations. Use one term for the same concept throughout the codebase.
- If using `[RelayCommand]` from `CommunityToolkit.Mvvm`, a method named `SaveAsync` generates a `SaveCommand` property; bind to the generated name.

## Working and verification

- Before editing, inspect the relevant View, ViewModel, resources, project configuration, and nearby conventions. Keep changes focused and avoid unrelated renames.
- Record durable team decisions and run instructions in `README.md`; keep formatting rules in `.editorconfig` rather than repeating them in code comments.
- Build the solution or project after code changes when a compatible .NET/WPF environment is available. Run relevant existing tests for changed behavior; add focused tests for nontrivial ViewModel or service logic.
- When changing bindings, run the affected screen if possible and check Visual Studio's XAML Binding Failures window or debug output. Report any verification that could not be performed.
