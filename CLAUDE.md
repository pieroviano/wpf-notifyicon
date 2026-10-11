# CLAUDE.md

WPF tray icon library (fork of hardcodet/wpf-notifyicon), published as NuGet `Net4x.Hardcodet.NotifyIcon.Wpf`.

## Layout (`src/`)

| Path | Content |
|---|---|
| `NotifyIconWpf.sln` | Solution (library, 3 samples, tests) |
| `NotifyIconWpf/` | Library. Assembly `Hardcodet.NotifyIcon.Wpf`, namespace `Hardcodet.Wpf.TaskbarNotification` |
| `NotifyIconWpf/TaskbarIcon.cs` | Behaviour: shell icon create/remove, mouse dispatch, popup/menu/tooltip, balloons, dispose |
| `NotifyIconWpf/TaskbarIcon.Declarations.cs` | Dependency properties, routed/attached events, property-changed handlers |
| `NotifyIconWpf/Interop/` | Win32: `WindowMessageSink` (hidden window, decodes shell callback messages), `WinApi`, `NotifyIconData`, `TrayInfo`/`AppBarInfo` (taskbar position), `SystemInfo` (DPI) |
| `NotifyIconWpf/Util.cs` | `WriteIconData`, `IsMatch` (activation mode ↔ mouse event), `ExecuteIfEnabled`, `GetDispatcher` |
| `NotifyIconWpf/README.md` | NuGet package readme (NuGet markdown subset, absolute links only) |
| `NotifyIconWpf.Sample.*` | ShowCases (WPF), Windowless, WindowsForms host |
| `NotifyIconWpf.Tests/` | xUnit functional tests (`net10.0-windows`) |

## Build config

- `src/Directory.Build.props`: shared props for **all** projects — WPF + WinForms, strong-name signing (`NotifyIconWpf.snk`), package-on-build, Nerdbank.GitVersioning, SourceLink; `IsSampleProject` = name contains `Sample` (disables signing/packing).
- `src/Directory.NuGet.props`: versions (`NotifyIconWpfVersion`, `NuGetUtilityVersion`).
- `NotifyIconWpf/Directory.Build.Props|Targets`: import parent + `Net4x.NuGetUtility` props/targets from the NuGet cache.
- Library TFMs: `net45;net462;net472;netcoreapp3.1;net6.0-windows;net8.0-windows;net10.0-windows` — keep code compatible with C# `latest` on net45 (no newer BCL APIs without `#if`).
- Debug sets `CheckForOverflowUnderflow=true`: casts of Win32 values (`short`, `uint`) need `unchecked`.
- Test project must opt out of package/doc generation and remove implicit `System.Drawing`/`System.Windows.Forms` usings (type-name clashes with `Interop.Point/Size` and WPF controls).

## Commands (from `src/`)

```
dotnet build NotifyIconWpf.sln -c Release
dotnet test NotifyIconWpf.Tests -c Debug
dotnet test NotifyIconWpf.Tests -c Debug --collect:"XPlat Code Coverage"
```

## Tests

- Functional, not mocked: real `TaskbarIcon`/`WindowMessageSink`, shell messages sent with `SendMessage` to the hidden message window (`Infrastructure/Sta.cs`).
- Every test body runs via `Sta.Run(...)` (dedicated STA thread + dispatcher); timer-driven behaviour (left-click delay, balloon timeout, cross-thread calls) needs `Sta.PumpUntil`/`PumpFor`.
- Private members (`messageSink`, `MessageWindowHandle`) reached via reflection — don't add `InternalsVisibleTo` (assembly is signed).
- Parallelization disabled (tray icons, popups, foreground window are global).
- Require an interactive Windows desktop with Explorer running; balloon-tip tests hide the icon first to avoid real toasts.

## Conventions

- Match existing style: `#region` blocks, XML doc comments on public members, `Raise*Event` helper pairs per routed event.
- Library fixes must be general (API/Win32 contract), never shaped around a sample.
