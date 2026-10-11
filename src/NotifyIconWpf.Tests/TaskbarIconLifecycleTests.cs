using System.Drawing;
using System.IO.Packaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using NotifyIconWpf.Tests.Infrastructure;

namespace NotifyIconWpf.Tests
{
    public class TaskbarIconLifecycleTests
    {
        private static bool ShellIsRunning => FindWindow("Shell_TrayWnd", null) != IntPtr.Zero;

        [Fact]
        public void Constructor_CreatesIconWhenShellIsRunning() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();

            Assert.Equal(ShellIsRunning, icon.IsTaskbarIconCreated);
            Assert.Equal(ShellIsRunning, icon.SupportsCustomToolTips);
            Assert.False(icon.IsDisposed);
            Assert.Equal(PopupActivationMode.LeftClick, icon.PopupActivation);
            Assert.Equal(PopupActivationMode.RightClick, icon.MenuActivation);
            Assert.Equal(PlacementMode.AbsolutePoint, icon.PopupPlacement);
            Assert.Equal(string.Empty, icon.ToolTipText);
        });

        [Fact]
        public void Visibility_RemovesAndRecreatesIcon() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            bool initial = icon.IsTaskbarIconCreated;

            icon.Visibility = Visibility.Collapsed;
            Assert.False(icon.IsTaskbarIconCreated);

            icon.Visibility = Visibility.Hidden;
            Assert.False(icon.IsTaskbarIconCreated);

            icon.Visibility = Visibility.Visible;
            Assert.Equal(initial, icon.IsTaskbarIconCreated);
        });

        [Fact]
        public void TaskbarCreatedMessage_RecreatesIcon() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon { ToolTipText = "restart" };
            bool initial = icon.IsTaskbarIconCreated;

            uint taskbarCreated = Sta.RegisterWindowMessage("TaskbarCreated");
            Sta.SendMessage(icon.GetMessageSink().GetMessageWindowHandle(), taskbarCreated, IntPtr.Zero, IntPtr.Zero);

            Assert.Equal(initial, icon.IsTaskbarIconCreated);
        });

        [Fact]
        public void Icon_CanBeAssignedAndCleared() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var systemIcon = SystemIcons.Application;

            icon.Icon = systemIcon;
            Assert.Same(systemIcon, icon.Icon);

            icon.Icon = null;
            Assert.Null(icon.Icon);
        });

        [Fact]
        public void IconSource_PackUri_ResolvesToIconOfSmallIconSize() => Sta.Run(() =>
        {
            _ = PackUriHelper.UriSchemePack;
            using var icon = new TaskbarIcon();
            var uri = new Uri("pack://application:,,,/NotifyIconWpf.Tests;component/Icons/Test.ico");

            icon.IconSource = new BitmapImage(uri);

            Assert.NotNull(icon.Icon);
            var expected = SystemInfo.SmallIconSize;
            Assert.True(icon.Icon.Width > 0 && icon.Icon.Width <= Math.Max(expected.Width, 256));
            Assert.True(icon.Icon.Height > 0 && icon.Icon.Height <= Math.Max(expected.Height, 256));

            icon.IconSource = null;
            Assert.Null(icon.Icon);
        });

        [Fact]
        public void ToolTipText_AndTrayToolTip_DoNotFailWhenIconIsHidden() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon { Visibility = Visibility.Collapsed };

            icon.TrayToolTip = new Border();
            icon.ToolTipText = new string('x', 300);

            Assert.NotNull(icon.TrayToolTipResolved);
        });

        [Fact]
        public void GetPopupTrayPosition_MatchesTrayInfo() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();

            var position = icon.GetPopupTrayPosition();
            var expected = TrayInfo.GetTrayLocation();

            Assert.Equal(expected.X, position.X);
            Assert.Equal(expected.Y, position.Y);
        });

        [Fact]
        public void Dispose_RemovesIconAndDestroysMessageWindow() => Sta.Run(() =>
        {
            var icon = new TaskbarIcon();
            var sink = icon.GetMessageSink();
            var hwnd = sink.GetMessageWindowHandle();

            icon.Dispose();
            icon.Dispose();

            Assert.True(icon.IsDisposed);
            Assert.False(icon.IsTaskbarIconCreated);
            Assert.True(sink.IsDisposed);
            Assert.False(Sta.IsWindow(hwnd));
        });

        [Fact]
        public void Dispose_TolerantMembersAreNoOps() => Sta.Run(() =>
        {
            var icon = new TaskbarIcon { TrayPopup = new Border() };
            icon.Dispose();

            icon.CloseBalloon();
            icon.ResetBalloonCloseTimer();
            icon.ShowTrayPopup();
            icon.CloseTrayPopup();

            Assert.False(icon.TrayPopupResolved.IsOpen);
        });

        [Fact]
        public void Dispose_StrictMembersThrowObjectDisposedException() => Sta.Run(() =>
        {
            var icon = new TaskbarIcon { Name = "disposedIcon" };
            icon.Dispose();

            var ex = Assert.Throws<ObjectDisposedException>(() => icon.ShowCustomBalloon(new Border(), PopupAnimation.None, null));
            Assert.Contains("disposedIcon", ex.ObjectName);
            Assert.Throws<ObjectDisposedException>(() => icon.ShowBalloonTip("t", "m", BalloonIcon.Info));
            Assert.Throws<ObjectDisposedException>(() => icon.HideBalloonTip());
        });

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    }
}
