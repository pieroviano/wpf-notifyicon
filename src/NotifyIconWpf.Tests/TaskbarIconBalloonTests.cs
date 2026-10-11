using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Hardcodet.Wpf.TaskbarNotification;
using NotifyIconWpf.Tests.Infrastructure;

namespace NotifyIconWpf.Tests
{
    public class TaskbarIconBalloonTests
    {
        [Fact]
        public void ShowCustomBalloon_OpensPopupAndRaisesShowingEvent() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon { PopupPlacement = PlacementMode.AbsolutePoint };
            var balloon = new Border { Width = 50, Height = 20 };
            TaskbarIcon parentWhileShowing = null;
            TaskbarIcon.AddBalloonShowingHandler(balloon, (s, e) => parentWhileShowing = TaskbarIcon.GetParentTaskbarIcon(balloon));

            icon.ShowCustomBalloon(balloon, PopupAnimation.Fade, null);

            var popup = icon.CustomBalloon;
            Assert.NotNull(popup);
            Assert.True(popup.IsOpen);
            Assert.True(popup.StaysOpen);
            Assert.Equal(PopupAnimation.Fade, popup.PopupAnimation);
            Assert.Same(balloon, popup.Child);
            Assert.Same(icon, popup.DataContext);
            Assert.Same(icon, TaskbarIcon.GetParentTaskbarIcon(balloon));
            Assert.Same(icon, parentWhileShowing);

            var tray = icon.GetPopupTrayPosition();
            Assert.Equal(tray.X - 1, popup.HorizontalOffset);
            Assert.Equal(tray.Y - 1, popup.VerticalOffset);

            icon.CloseBalloon();
        });

        [Fact]
        public void ShowCustomBalloon_UsesCustomPopupPosition() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon
            {
                CustomPopupPosition = () => new Hardcodet.Wpf.TaskbarNotification.Interop.Point { X = 300, Y = 400 },
            };

            icon.ShowCustomBalloon(new Border(), PopupAnimation.None, null);

            Assert.Equal(299, icon.CustomBalloon.HorizontalOffset);
            Assert.Equal(399, icon.CustomBalloon.VerticalOffset);
            icon.CloseBalloon();
        });

        [Fact]
        public void CloseBalloon_ClosesPopupAndReleasesBalloon() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var balloon = new Border();
            TaskbarIcon parentWhileClosing = null;
            TaskbarIcon.AddBalloonClosingHandler(balloon, (s, e) => parentWhileClosing = TaskbarIcon.GetParentTaskbarIcon(balloon));
            icon.ShowCustomBalloon(balloon, PopupAnimation.None, null);
            var popup = icon.CustomBalloon;

            icon.CloseBalloon();

            Assert.Same(icon, parentWhileClosing);
            Assert.False(popup.IsOpen);
            Assert.Null(popup.Child);
            Assert.Null(icon.CustomBalloon);
            Assert.Null(TaskbarIcon.GetParentTaskbarIcon(balloon));

            // the released balloon can be displayed again
            icon.ShowCustomBalloon(balloon, PopupAnimation.None, null);
            Assert.True(icon.CustomBalloon.IsOpen);
            icon.CloseBalloon();
        });

        [Fact]
        public void CloseBalloon_HandledClosingEvent_LeavesPopupToHandler() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var balloon = new Border();
            TaskbarIcon.AddBalloonClosingHandler(balloon, (s, e) => e.Handled = true);
            icon.ShowCustomBalloon(balloon, PopupAnimation.None, null);
            var popup = icon.CustomBalloon;

            icon.CloseBalloon();

            Assert.Null(icon.CustomBalloon);
            Assert.True(popup.IsOpen);
            Assert.Same(balloon, popup.Child);
            popup.IsOpen = false;
        });

        [Fact]
        public void BalloonHandlers_Removed_AreNotInvoked() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var balloon = new Border();
            int count = 0;
            RoutedEventHandler handler = (s, e) => count++;
            TaskbarIcon.AddBalloonShowingHandler(balloon, handler);
            TaskbarIcon.AddBalloonClosingHandler(balloon, handler);
            TaskbarIcon.RemoveBalloonShowingHandler(balloon, handler);
            TaskbarIcon.RemoveBalloonClosingHandler(balloon, handler);

            icon.ShowCustomBalloon(balloon, PopupAnimation.None, null);
            icon.CloseBalloon();

            Assert.Equal(0, count);
        });

        [Fact]
        public void CloseBalloon_WithoutBalloon_IsNoOp() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();

            icon.CloseBalloon();
            icon.ResetBalloonCloseTimer();

            Assert.Null(icon.CustomBalloon);
        });

        [Fact]
        public void ShowCustomBalloon_Twice_ClosesPreviousBalloon() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var first = new Border();
            var second = new Border();

            icon.ShowCustomBalloon(first, PopupAnimation.None, null);
            var firstPopup = icon.CustomBalloon;
            icon.ShowCustomBalloon(second, PopupAnimation.None, null);

            Assert.False(firstPopup.IsOpen);
            Assert.Null(TaskbarIcon.GetParentTaskbarIcon(first));
            Assert.Same(second, icon.CustomBalloon.Child);
            icon.CloseBalloon();
        });

        [Fact]
        public void ShowCustomBalloon_Timeout_ClosesBalloonAutomatically() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();

            icon.ShowCustomBalloon(new Border(), PopupAnimation.None, 500);

            Assert.NotNull(icon.CustomBalloon);
            Assert.True(Sta.PumpUntil(() => icon.CustomBalloon == null));
        });

        [Fact]
        public void ResetBalloonCloseTimer_KeepsBalloonOpen() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();

            icon.ShowCustomBalloon(new Border(), PopupAnimation.None, 500);
            icon.ResetBalloonCloseTimer();
            Sta.PumpFor(TimeSpan.FromMilliseconds(900));

            Assert.NotNull(icon.CustomBalloon);
            Assert.True(icon.CustomBalloon.IsOpen);
            icon.CloseBalloon();
        });

        [Fact]
        public void ShowCustomBalloon_InvalidArguments_Throw() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();

            Assert.Throws<ArgumentNullException>(() => icon.ShowCustomBalloon(null, PopupAnimation.None, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => icon.ShowCustomBalloon(new Border(), PopupAnimation.None, 499));
            Assert.Null(icon.CustomBalloon);
        });

        [Fact]
        public void ShowCustomBalloon_ElementHostedByAnotherPopup_Throws() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var balloon = new Border();
            var foreignPopup = new Popup { Child = balloon };

            Assert.Throws<InvalidOperationException>(() => icon.ShowCustomBalloon(balloon, PopupAnimation.None, null));
            Assert.Null(foreignPopup.Child);
        });

        [Fact]
        public void BalloonMethods_FromWorkerThread_MarshalToDispatcher() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var balloon = new Border();

            var show = Task.Run(() => icon.ShowCustomBalloon(balloon, PopupAnimation.None, null));
            Assert.True(Sta.PumpUntil(() => show.IsCompleted));
            show.GetAwaiter().GetResult();
            Assert.True(icon.CustomBalloon.IsOpen);

            var close = Task.Run(icon.CloseBalloon);
            Assert.True(Sta.PumpUntil(() => close.IsCompleted));
            close.GetAwaiter().GetResult();
            Assert.Null(icon.CustomBalloon);
        });

        [Theory]
        [InlineData(BalloonIcon.None)]
        [InlineData(BalloonIcon.Info)]
        [InlineData(BalloonIcon.Warning)]
        [InlineData(BalloonIcon.Error)]
        public void ShowBalloonTip_StandardIcons_DoNotThrow(BalloonIcon symbol) => Sta.Run(() =>
        {
            // hidden icon: exercises the Shell_NotifyIcon path without popping real toasts on the desktop
            using var icon = new TaskbarIcon { Visibility = Visibility.Collapsed };

            icon.ShowBalloonTip("title", "message", symbol);
            icon.ShowBalloonTip(null, null, symbol);
            icon.HideBalloonTip();
        });

        [Fact]
        public void ShowBalloonTip_CustomIcon_DoesNotThrow() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon { Visibility = Visibility.Collapsed };

            icon.ShowBalloonTip("title", "message", SystemIcons.Information);
            icon.ShowBalloonTip("title", "message", SystemIcons.Warning, largeIcon: true);
            icon.HideBalloonTip();
        });

        [Fact]
        public void ShowBalloonTip_InvalidArguments_Throw() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon { Visibility = Visibility.Collapsed };

            Assert.Throws<ArgumentNullException>(() => icon.ShowBalloonTip("t", "m", (Icon)null));
            Assert.Throws<ArgumentOutOfRangeException>(() => icon.ShowBalloonTip("t", "m", (BalloonIcon)99));
        });
    }
}
