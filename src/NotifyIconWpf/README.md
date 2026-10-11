# Net4x.Hardcodet.NotifyIcon.Wpf

WPF system tray (notify) icon control: rich tooltips, popups, WPF context menus, standard and custom balloons, click commands. Usable from XAML or code (also hostable from Windows Forms apps).

Fork of [hardcodet/wpf-notifyicon](https://github.com/hardcodet/wpf-notifyicon), multi-targeted for .NET Framework and modern .NET.

## Install

```
dotnet add package Net4x.Hardcodet.NotifyIcon.Wpf
```

## Supported frameworks

| Family | TFMs |
|---|---|
| .NET Framework | `net45`, `net462`, `net472` |
| .NET Core / .NET | `netcoreapp3.1`, `net6.0-windows`, `net8.0-windows`, `net10.0-windows` |

- Assembly: `Hardcodet.NotifyIcon.Wpf`
- Namespace: `Hardcodet.Wpf.TaskbarNotification`
- XAML xmlns: `http://www.hardcodet.net/taskbar`

## XAML

```xml
<Window xmlns:tb="http://www.hardcodet.net/taskbar" ...>
  <tb:TaskbarIcon x:Name="MyNotifyIcon"
                  IconSource="/Icons/App.ico"
                  ToolTipText="My application"
                  DoubleClickCommand="{Binding ShowWindowCommand}">
    <tb:TaskbarIcon.ContextMenu>
      <ContextMenu>
        <MenuItem Header="Show" Command="{Binding ShowWindowCommand}" />
        <MenuItem Header="Exit" Command="{Binding ExitCommand}" />
      </ContextMenu>
    </tb:TaskbarIcon.ContextMenu>
  </tb:TaskbarIcon>
</Window>
```

For a window-less app, declare the icon as a resource and fetch it in `App.OnStartup` with `FindResource("...")`.

## Code

```csharp
using Hardcodet.Wpf.TaskbarNotification;

var icon = new TaskbarIcon
{
    Icon = new System.Drawing.Icon("App.ico"),
    ToolTipText = "My application"
};

icon.ShowBalloonTip("Title", "Hello from the tray", BalloonIcon.Info);

// when done
icon.Dispose();
```

## Key properties

| Property | Type | Default | Purpose |
|---|---|---|---|
| `IconSource` | `ImageSource` | – | Icon from a WPF resource/URI (XAML-friendly); sets `Icon` |
| `Icon` | `System.Drawing.Icon` | – | Icon from code |
| `ToolTipText` | `string` | – | Plain tooltip; fallback when `TrayToolTip` is not set/supported |
| `TrayToolTip` | `UIElement` | – | Custom WPF tooltip |
| `TrayPopup` | `UIElement` | – | Interactive popup shown on click |
| `ContextMenu` | `ContextMenu` | – | WPF context menu |
| `MenuActivation` | `PopupActivationMode` | `RightClick` | Click that opens `ContextMenu` |
| `PopupActivation` | `PopupActivationMode` | `LeftClick` | Click that opens `TrayPopup` |
| `PopupPlacement` | `PlacementMode` | `AbsolutePoint` | `TrayPopup` placement (see also `PopupHorizontalOffset`, `PopupVerticalOffset`, `CustomPopupPosition`) |
| `LeftClickCommand` | `ICommand` | – | Run on left click (`...CommandParameter`, `...CommandTarget`) |
| `DoubleClickCommand` | `ICommand` | – | Run on double click (`...CommandParameter`, `...CommandTarget`) |
| `NoLeftClickDelay` | `bool` | `false` | Handle left click immediately instead of waiting for the double-click interval |

`PopupActivationMode`: `LeftClick`, `RightClick`, `DoubleClick`, `LeftOrRightClick`, `LeftOrDoubleClick`, `MiddleClick`, `All`, `None`.

Read-only: `IsTaskbarIconCreated`, `IsDisposed`, `TrayPopupResolved`, `TrayToolTipResolved`, `CustomBalloon`.

## Key methods

| Method | Purpose |
|---|---|
| `ShowBalloonTip(title, message, BalloonIcon)` | Standard balloon; `BalloonIcon`: `None`, `Info`, `Warning`, `Error` |
| `ShowBalloonTip(title, message, Icon customIcon, bool largeIcon = false)` | Standard balloon with custom icon |
| `HideBalloonTip()` | Hide the standard balloon |
| `ShowCustomBalloon(UIElement, PopupAnimation, int? timeout)` | Show a WPF element as balloon; `timeout` ms, `null` = stays open |
| `CloseBalloon()` | Close the custom balloon |
| `ResetBalloonCloseTimer()` | Restart the custom balloon timeout |
| `ShowTrayPopup()` / `CloseTrayPopup()` | Open / close `TrayPopup` programmatically |
| `GetPopupTrayPosition()` | Tray position used for popups |
| `Dispose()` | Remove the icon from the tray |

## Key events

All are routed events (`RoutedEventHandler`).

| Group | Events |
|---|---|
| Mouse | `TrayLeftMouseDown`, `TrayLeftMouseUp`, `TrayRightMouseDown`, `TrayRightMouseUp`, `TrayMiddleMouseDown`, `TrayMiddleMouseUp`, `TrayMouseDoubleClick`, `TrayMouseMove` |
| Balloon | `TrayBalloonTipShown`, `TrayBalloonTipClosed`, `TrayBalloonTipClicked` |
| Popup / menu | `PreviewTrayPopupOpen`, `TrayPopupOpen`, `PreviewTrayContextMenuOpen`, `TrayContextMenuOpen` |
| ToolTip | `PreviewTrayToolTipOpen`, `TrayToolTipOpen`, `PreviewTrayToolTipClose`, `TrayToolTipClose` |

Attached events raised on hosted elements (for animations/triggers): `PopupOpened`, `ToolTipOpened`, `ToolTipClose`, `BalloonShowing`, `BalloonClosing`. Attached property `ParentTaskbarIcon` gives hosted elements access to the icon.

## Lifetime

- `TaskbarIcon` is `IDisposable`: call `Dispose()` when no longer needed, otherwise the icon stays in the tray.
- If `Application.Current` exists, the icon disposes itself on `Application.Exit`; call `Dispose()` explicitly anyway in window-less / Windows Forms hosts.
- Icons declared in a `Window` should be disposed when the window closes if the app keeps running.

## Credits and license

- Original author: Philipp Sumi (Hardcodet) — https://github.com/hardcodet/wpf-notifyicon
- Tutorial: https://www.codeproject.com/KB/WPF/wpf_notifyicon.aspx
- License: [CPOL 1.02](https://www.codeproject.com/info/cpol10.aspx)
