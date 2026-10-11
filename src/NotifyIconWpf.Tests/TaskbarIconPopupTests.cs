using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Hardcodet.Wpf.TaskbarNotification;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using NotifyIconWpf.Tests.Infrastructure;

namespace NotifyIconWpf.Tests
{
    public class TaskbarIconPopupTests
    {
        [Fact]
        public void TrayPopup_UIElement_IsWrappedInPopupWithParentAndDataContext() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon { PopupPlacement = PlacementMode.Mouse };
            var content = new Border();

            icon.TrayPopup = content;

            var popup = icon.TrayPopupResolved;
            Assert.NotNull(popup);
            Assert.Same(content, popup.Child);
            Assert.False(popup.StaysOpen);
            Assert.Equal(PlacementMode.Mouse, popup.Placement);
            Assert.Same(icon, popup.DataContext);
            Assert.Same(icon, TaskbarIcon.GetParentTaskbarIcon(content));
        });

        [Fact]
        public void TrayPopup_PopupInstance_IsUsedDirectly() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var popup = new Popup();

            icon.TrayPopup = popup;

            Assert.Same(popup, icon.TrayPopupResolved);
        });

        [Fact]
        public void TrayPopup_Replaced_ClearsParentOfPreviousElement() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var first = new Border();
            var second = new Border();

            icon.TrayPopup = first;
            icon.TrayPopup = second;

            Assert.Null(TaskbarIcon.GetParentTaskbarIcon(first));
            Assert.Same(icon, TaskbarIcon.GetParentTaskbarIcon(second));

            icon.TrayPopup = null;
            Assert.Null(icon.TrayPopupResolved);
            Assert.Null(TaskbarIcon.GetParentTaskbarIcon(second));
        });

        [Fact]
        public void ShowTrayPopup_OpensPopupAndRaisesEvents() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var content = new Border { Width = 20, Height = 20 };
            icon.TrayPopup = content;
            var events = new List<string>();
            icon.PreviewTrayPopupOpen += (s, e) => events.Add("preview");
            icon.TrayPopupOpen += (s, e) => events.Add("open");
            TaskbarIcon.AddPopupOpenedHandler(content, (s, e) => events.Add("attached"));

            icon.ShowTrayPopup();

            Assert.True(icon.TrayPopupResolved.IsOpen);
            Assert.Equal(new[] { "preview", "attached", "open" }, events);

            icon.CloseTrayPopup();
            Assert.False(icon.TrayPopupResolved.IsOpen);
        });

        [Fact]
        public void ShowTrayPopup_HandledPreview_DoesNotOpen() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon { TrayPopup = new Border() };
            icon.PreviewTrayPopupOpen += (s, e) => e.Handled = true;
            int opened = 0;
            icon.TrayPopupOpen += (s, e) => opened++;

            icon.ShowTrayPopup();
            icon.CloseTrayPopup();

            Assert.False(icon.TrayPopupResolved.IsOpen);
            Assert.Equal(0, opened);
        });

        [Fact]
        public void ShowTrayPopup_WithoutPopup_RaisesOnlyPreview() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            int preview = 0, opened = 0;
            icon.PreviewTrayPopupOpen += (s, e) => preview++;
            icon.TrayPopupOpen += (s, e) => opened++;

            icon.ShowTrayPopup();

            Assert.Equal(1, preview);
            Assert.Equal(0, opened);
        });

        [Fact]
        public void ShowTrayPopup_RemovingAttachedHandler_StopsNotification() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var content = new Border();
            icon.TrayPopup = content;
            int attached = 0;
            RoutedEventHandler handler = (s, e) => attached++;
            TaskbarIcon.AddPopupOpenedHandler(content, handler);
            TaskbarIcon.RemovePopupOpenedHandler(content, handler);

            icon.ShowTrayPopup();
            icon.CloseTrayPopup();

            Assert.Equal(0, attached);
        });

        [Fact]
        public void ShowTrayPopup_AbsolutePoint_AppliesOffsetsToCursorPosition() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon
            {
                TrayPopup = new Border(),
                PopupPlacement = PlacementMode.AbsolutePoint,
                PopupHorizontalOffset = 7,
                PopupVerticalOffset = -3,
            };

            icon.ShowTrayPopup();
            var cursor = GetCursor(icon);

            var popup = icon.TrayPopupResolved;
            Assert.Equal(PlacementMode.AbsolutePoint, popup.Placement);
            Assert.InRange(popup.HorizontalOffset, cursor.X + 7 - 50, cursor.X + 7 + 50);
            Assert.InRange(popup.VerticalOffset, cursor.Y - 3 - 50, cursor.Y - 3 + 50);
            icon.CloseTrayPopup();
        });

        [Fact]
        public void ShowTrayPopup_Bottom_PlacesPopupAtTrayLocation() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon
            {
                TrayPopup = new Border(),
                PopupPlacement = PlacementMode.Bottom,
                PopupHorizontalOffset = 5,
                PopupVerticalOffset = 6,
            };

            icon.ShowTrayPopup();

            var tray = TrayInfo.GetTrayLocation(0);
            var popup = icon.TrayPopupResolved;
            Assert.Equal(PlacementMode.AbsolutePoint, popup.Placement);
            Assert.Equal(tray.X + 5, popup.HorizontalOffset);
            Assert.Equal(tray.Y + 6, popup.VerticalOffset);
            icon.CloseTrayPopup();
        });

        [Fact]
        public void ContextMenu_Assigned_GetsParentAndDataContext() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var first = new ContextMenu();
            var second = new ContextMenu();

            icon.ContextMenu = first;
            Assert.Same(icon, TaskbarIcon.GetParentTaskbarIcon(first));
            Assert.Same(icon, first.DataContext);

            icon.ContextMenu = second;
            Assert.Null(TaskbarIcon.GetParentTaskbarIcon(first));
            Assert.Same(icon, TaskbarIcon.GetParentTaskbarIcon(second));
        });

        [Fact]
        public void ContextMenuMessage_OpensMenuAtAnchor() => Sta.Run(() =>
        {
            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem { Header = "Item" });
            using var icon = new TaskbarIcon { ContextMenu = menu };
            var events = new List<string>();
            icon.PreviewTrayContextMenuOpen += (s, e) => events.Add("preview");
            icon.TrayContextMenuOpen += (s, e) => events.Add("open");

            icon.SendTrayMessage(WindowsMessages.WM_CONTEXTMENU, (IntPtr)((200 << 16) | 100));

            Assert.True(menu.IsOpen);
            Assert.Equal(PlacementMode.AbsolutePoint, menu.Placement);
            Assert.Equal(100, menu.HorizontalOffset);
            Assert.Equal(200, menu.VerticalOffset);
            Assert.Equal(new[] { "preview", "open" }, events);
            menu.IsOpen = false;
        });

        [Fact]
        public void ContextMenuMessage_HandledPreview_DoesNotOpenMenu() => Sta.Run(() =>
        {
            var menu = new ContextMenu();
            using var icon = new TaskbarIcon { ContextMenu = menu };
            icon.PreviewTrayContextMenuOpen += (s, e) => e.Handled = true;
            int opened = 0;
            icon.TrayContextMenuOpen += (s, e) => opened++;

            icon.SendTrayMessage(WindowsMessages.WM_CONTEXTMENU);

            Assert.False(menu.IsOpen);
            Assert.Equal(0, opened);
        });

        [Fact]
        public void ContextMenuMessage_WithoutMenu_RaisesOnlyPreview() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            int preview = 0, opened = 0;
            icon.PreviewTrayContextMenuOpen += (s, e) => preview++;
            icon.TrayContextMenuOpen += (s, e) => opened++;

            icon.SendTrayMessage(WindowsMessages.NIN_KEYSELECT);

            Assert.Equal(1, preview);
            Assert.Equal(0, opened);
        });

        [Fact]
        public void LeftClickMenuActivation_OpensMenuAtCursorAfterDelayAndRunsCommand() => Sta.Run(() =>
        {
            var menu = new ContextMenu();
            var command = new RecordingCommand();
            using var icon = new TaskbarIcon
            {
                ContextMenu = menu,
                MenuActivation = PopupActivationMode.LeftClick,
                PopupActivation = PopupActivationMode.None,
                NoLeftClickDelay = true,
                LeftClickCommand = command,
            };

            icon.SendTrayMessage(WindowsMessages.WM_LBUTTONUP);

            Assert.True(Sta.PumpUntil(() => menu.IsOpen));
            Assert.Single(command.Executions);
            menu.IsOpen = false;
        });

        [Fact]
        public void LeftClickPopupActivation_OpensPopupAfterDelayAndRunsCommand() => Sta.Run(() =>
        {
            var command = new RecordingCommand();
            using var icon = new TaskbarIcon
            {
                TrayPopup = new Border(),
                NoLeftClickDelay = true,
                LeftClickCommand = command,
                LeftClickCommandParameter = "x",
            };
            int opened = 0;
            icon.TrayPopupOpen += (s, e) => opened++;

            icon.SendTrayMessage(WindowsMessages.WM_LBUTTONUP);

            Assert.True(Sta.PumpUntil(() => opened == 1));
            Assert.Equal(new object[] { "x" }, command.Executions);
            icon.CloseTrayPopup();
        });

        [Fact]
        public void ToolTipText_CreatesAndUpdatesSimpleToolTip() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();

            icon.ToolTipText = "hello";
            var tooltip = icon.TrayToolTipResolved;
            Assert.NotNull(tooltip);
            Assert.Equal("hello", tooltip.Content);
            Assert.Same(icon, tooltip.DataContext);

            icon.ToolTipText = "world";
            Assert.Same(tooltip, icon.TrayToolTipResolved);
            Assert.Equal("world", tooltip.Content);
        });

        [Fact]
        public void TrayToolTip_UIElement_IsWrappedAndOverridesToolTipText() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var content = new Border();

            icon.TrayToolTip = content;
            icon.ToolTipText = "ignored for content";

            var tooltip = icon.TrayToolTipResolved;
            Assert.Same(content, tooltip.Content);
            Assert.False(tooltip.HasDropShadow);
            Assert.True(tooltip.StaysOpen);
            Assert.Same(icon, TaskbarIcon.GetParentTaskbarIcon(content));

            icon.TrayToolTip = null;
            Assert.Null(TaskbarIcon.GetParentTaskbarIcon(content));
            Assert.Equal("ignored for content", icon.TrayToolTipResolved.Content);
        });

        [Fact]
        public void TrayToolTip_ToolTipInstance_IsUsedDirectly() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var tooltip = new ToolTip();

            icon.TrayToolTip = tooltip;

            Assert.Same(tooltip, icon.TrayToolTipResolved);
        });

        [Fact]
        public void PopupOpenAndCloseMessages_ToggleCustomToolTipAndRaiseEvents() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var content = new Border { Width = 10, Height = 10 };
            icon.TrayToolTip = content;
            var events = new List<string>();
            icon.PreviewTrayToolTipOpen += (s, e) => events.Add("previewOpen");
            icon.TrayToolTipOpen += (s, e) => events.Add("open");
            icon.PreviewTrayToolTipClose += (s, e) => events.Add("previewClose");
            icon.TrayToolTipClose += (s, e) => events.Add("close");
            TaskbarIcon.AddToolTipOpenedHandler(content, (s, e) => events.Add("attachedOpen"));
            TaskbarIcon.AddToolTipCloseHandler(content, (s, e) => events.Add("attachedClose"));

            icon.SendTrayMessage(WindowsMessages.NIN_POPUPOPEN);
            Assert.True(icon.TrayToolTipResolved.IsOpen);

            icon.SendTrayMessage(WindowsMessages.NIN_POPUPCLOSE);
            Assert.False(icon.TrayToolTipResolved.IsOpen);

            Assert.Equal(new[] { "previewOpen", "attachedOpen", "open", "previewClose", "attachedClose", "close" }, events);
        });

        [Fact]
        public void ToolTipHandlers_Removed_AreNotInvoked() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var content = new Border();
            icon.TrayToolTip = content;
            int count = 0;
            RoutedEventHandler handler = (s, e) => count++;
            TaskbarIcon.AddToolTipOpenedHandler(content, handler);
            TaskbarIcon.AddToolTipCloseHandler(content, handler);
            TaskbarIcon.RemoveToolTipOpenedHandler(content, handler);
            TaskbarIcon.RemoveToolTipCloseHandler(content, handler);

            icon.SendTrayMessage(WindowsMessages.NIN_POPUPOPEN);
            icon.SendTrayMessage(WindowsMessages.NIN_POPUPCLOSE);

            Assert.Equal(0, count);
        });

        [Fact]
        public void PopupOpenMessage_HandledPreviews_KeepToolTipState() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon { ToolTipText = "tip" };
            icon.PreviewTrayToolTipOpen += (s, e) => e.Handled = true;
            icon.PreviewTrayToolTipClose += (s, e) => e.Handled = true;
            int events = 0;
            icon.TrayToolTipOpen += (s, e) => events++;
            icon.TrayToolTipClose += (s, e) => events++;

            icon.SendTrayMessage(WindowsMessages.NIN_POPUPOPEN);
            Assert.False(icon.TrayToolTipResolved.IsOpen);

            icon.SendTrayMessage(WindowsMessages.NIN_POPUPCLOSE);
            Assert.Equal(0, events);
        });

        [Fact]
        public void PopupOpenMessage_WithoutToolTip_DoesNothing() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            int preview = 0;
            icon.PreviewTrayToolTipOpen += (s, e) => preview++;
            icon.PreviewTrayToolTipClose += (s, e) => preview++;

            icon.SendTrayMessage(WindowsMessages.NIN_POPUPOPEN);
            icon.SendTrayMessage(WindowsMessages.NIN_POPUPCLOSE);

            Assert.Equal(0, preview);
        });

        [Fact]
        public void PopupOpenMessage_IsIgnoredWhileCustomBalloonIsOpen() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon { ToolTipText = "tip" };
            icon.ShowCustomBalloon(new Border(), PopupAnimation.None, null);
            int preview = 0;
            icon.PreviewTrayToolTipOpen += (s, e) => preview++;

            icon.SendTrayMessage(WindowsMessages.NIN_POPUPOPEN);

            Assert.False(icon.TrayToolTipResolved.IsOpen);
            Assert.Equal(0, preview);
            icon.CloseBalloon();
        });

        [Fact]
        public void DataContext_PropagatesToPopupToolTipAndMenu() => Sta.Run(() =>
        {
            var menu = new ContextMenu();
            using var icon = new TaskbarIcon
            {
                TrayPopup = new Border(),
                ToolTipText = "tip",
                ContextMenu = menu,
            };
            var first = new object();
            var second = new object();

            icon.DataContext = first;
            Assert.Same(first, icon.TrayPopupResolved.DataContext);
            Assert.Same(first, icon.TrayToolTipResolved.DataContext);
            Assert.Same(first, menu.DataContext);

            icon.DataContext = second;
            Assert.Same(second, icon.TrayPopupResolved.DataContext);
            Assert.Same(second, icon.TrayToolTipResolved.DataContext);
            Assert.Same(second, menu.DataContext);

            icon.DataContext = null;
            Assert.Same(icon, icon.TrayPopupResolved.DataContext);
        });

        [Fact]
        public void DataContext_DoesNotOverrideOwnOrBoundDataContext() => Sta.Run(() =>
        {
            var own = new object();
            var menu = new ContextMenu { DataContext = own };
            var popup = new Popup();
            popup.SetBinding(FrameworkElement.DataContextProperty, new Binding { Source = "bound" });
            using var icon = new TaskbarIcon { ContextMenu = menu, TrayPopup = popup };

            icon.DataContext = new object();

            Assert.Same(own, menu.DataContext);
            Assert.Equal("bound", popup.DataContext);
        });

        private static Hardcodet.Wpf.TaskbarNotification.Interop.Point GetCursor(TaskbarIcon icon)
        {
            var p = System.Windows.Forms.Cursor.Position;
            return TrayInfo.GetDeviceCoordinates(new Hardcodet.Wpf.TaskbarNotification.Interop.Point { X = p.X, Y = p.Y });
        }
    }
}
