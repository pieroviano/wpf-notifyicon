using Hardcodet.Wpf.TaskbarNotification.Interop;
using NotifyIconWpf.Tests.Infrastructure;

namespace NotifyIconWpf.Tests
{
    public class WindowMessageSinkTests
    {
        public static TheoryData<WindowsMessages, MouseEvent> MouseMessages => new TheoryData<WindowsMessages, MouseEvent>
        {
            { WindowsMessages.WM_MOUSEMOVE, MouseEvent.MouseMove },
            { WindowsMessages.WM_LBUTTONDOWN, MouseEvent.IconLeftMouseDown },
            { WindowsMessages.WM_LBUTTONUP, MouseEvent.IconLeftMouseUp },
            { WindowsMessages.NIN_SELECT, MouseEvent.IconLeftMouseUp },
            { WindowsMessages.WM_LBUTTONDBLCLK, MouseEvent.IconDoubleClick },
            { WindowsMessages.WM_RBUTTONDOWN, MouseEvent.IconRightMouseDown },
            { WindowsMessages.WM_RBUTTONUP, MouseEvent.IconRightMouseUp },
            { WindowsMessages.WM_MBUTTONDOWN, MouseEvent.IconMiddleMouseDown },
            { WindowsMessages.WM_MBUTTONUP, MouseEvent.IconMiddleMouseUp },
            { WindowsMessages.NIN_BALLOONUSERCLICK, MouseEvent.BalloonToolTipClicked },
        };

        [Theory]
        [MemberData(nameof(MouseMessages))]
        public void CallbackMessage_RaisesMatchingMouseEvent(WindowsMessages message, MouseEvent expected) => Sta.Run(() =>
        {
            using var sink = new WindowMessageSink(NotifyIconVersion.Vista);
            var received = new List<MouseEvent>();
            sink.MouseEventReceived += received.Add;

            sink.SendTrayMessage(message);

            Assert.Equal(new[] { expected }, received);
        });

        [Theory]
        [InlineData(WindowsMessages.WM_RBUTTONDBLCLK)]
        [InlineData(WindowsMessages.WM_MBUTTONDBLCLK)]
        [InlineData((WindowsMessages)0x0333)]
        public void IgnoredCallbackMessages_RaiseNothing(WindowsMessages message) => Sta.Run(() =>
        {
            using var sink = new WindowMessageSink(NotifyIconVersion.Vista);
            int count = 0;
            sink.MouseEventReceived += _ => count++;
            sink.ContextMenuReceived += _ => count++;
            sink.BalloonToolTipChanged += _ => count++;
            sink.ChangeToolTipStateRequest += _ => count++;

            sink.SendTrayMessage(message);

            Assert.Equal(0, count);
        });

        [Fact]
        public void LeftButtonUp_AfterDoubleClick_IsSuppressedOnce() => Sta.Run(() =>
        {
            using var sink = new WindowMessageSink(NotifyIconVersion.Vista);
            var received = new List<MouseEvent>();
            sink.MouseEventReceived += received.Add;

            sink.SendTrayMessage(WindowsMessages.WM_LBUTTONDBLCLK);
            sink.SendTrayMessage(WindowsMessages.WM_LBUTTONUP);
            sink.SendTrayMessage(WindowsMessages.WM_LBUTTONUP);

            Assert.Equal(new[] { MouseEvent.IconDoubleClick, MouseEvent.IconLeftMouseUp }, received);
        });

        [Theory]
        [InlineData(WindowsMessages.WM_CONTEXTMENU)]
        [InlineData(WindowsMessages.NIN_KEYSELECT)]
        public void ContextMenuMessages_DecodeAnchorFromWParam(WindowsMessages message) => Sta.Run(() =>
        {
            using var sink = new WindowMessageSink(NotifyIconVersion.Vista);
            Point? anchor = null;
            sink.ContextMenuReceived += p => anchor = p;

            sink.SendTrayMessage(message, (IntPtr)((120 << 16) | 340));

            Assert.NotNull(anchor);
            Assert.Equal(340, anchor.Value.X);
            Assert.Equal(120, anchor.Value.Y);
        });

        [Fact]
        public void ContextMenuMessage_DecodesNegativeCoordinates() => Sta.Run(() =>
        {
            using var sink = new WindowMessageSink(NotifyIconVersion.Vista);
            Point? anchor = null;
            sink.ContextMenuReceived += p => anchor = p;

            // x = -10, y = -20 as signed 16-bit words (multi-monitor setups left/above the primary screen)
            sink.SendTrayMessage(WindowsMessages.WM_CONTEXTMENU, (IntPtr)unchecked((int)0xFFECFFF6));

            Assert.Equal(-10, anchor.Value.X);
            Assert.Equal(-20, anchor.Value.Y);
        });

        [Theory]
        [InlineData(WindowsMessages.NIN_BALLOONSHOW, true)]
        [InlineData(WindowsMessages.NIN_BALLOONHIDE, false)]
        [InlineData(WindowsMessages.NIN_BALLOONTIMEOUT, false)]
        public void BalloonMessages_RaiseBalloonToolTipChanged(WindowsMessages message, bool expected) => Sta.Run(() =>
        {
            using var sink = new WindowMessageSink(NotifyIconVersion.Vista);
            var received = new List<bool>();
            sink.BalloonToolTipChanged += received.Add;

            sink.SendTrayMessage(message);

            Assert.Equal(new[] { expected }, received);
        });

        [Theory]
        [InlineData(WindowsMessages.NIN_POPUPOPEN, true)]
        [InlineData(WindowsMessages.NIN_POPUPCLOSE, false)]
        public void PopupMessages_RaiseChangeToolTipStateRequest(WindowsMessages message, bool expected) => Sta.Run(() =>
        {
            using var sink = new WindowMessageSink(NotifyIconVersion.Vista);
            var received = new List<bool>();
            sink.ChangeToolTipStateRequest += received.Add;

            sink.SendTrayMessage(message);

            Assert.Equal(new[] { expected }, received);
        });

        [Fact]
        public void TaskbarCreatedMessage_RaisesTaskbarCreated() => Sta.Run(() =>
        {
            using var sink = new WindowMessageSink(NotifyIconVersion.Win95);
            int count = 0;
            sink.TaskbarCreated += () => count++;

            uint taskbarCreated = Sta.RegisterWindowMessage("TaskbarCreated");
            Sta.SendMessage(sink.GetMessageWindowHandle(), taskbarCreated, IntPtr.Zero, IntPtr.Zero);

            Assert.Equal(1, count);
        });

        [Fact]
        public void NonCallbackMessages_AreIgnoredButDpiChangeIsHandled() => Sta.Run(() =>
        {
            using var sink = new WindowMessageSink(NotifyIconVersion.Win95);
            int count = 0;
            sink.MouseEventReceived += _ => count++;
            var hwnd = sink.GetMessageWindowHandle();

            Sta.SendMessage(hwnd, (uint)WindowsMessages.WM_DPICHANGED, IntPtr.Zero, IntPtr.Zero);
            Sta.SendMessage(hwnd, (uint)WindowsMessages.WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);

            Assert.Equal(0, count);
            Assert.True(SystemInfo.DpiFactorX > 0);
            Assert.True(SystemInfo.DpiFactorY > 0);
        });

        [Fact]
        public void Constructor_CreatesMessageWindowAndKeepsVersion() => Sta.Run(() =>
        {
            using var sink = new WindowMessageSink(NotifyIconVersion.Win2000);

            Assert.True(Sta.IsWindow(sink.GetMessageWindowHandle()));
            Assert.Equal(NotifyIconVersion.Win2000, sink.Version);
            Assert.False(sink.IsDisposed);
        });

        [Fact]
        public void Dispose_DestroysMessageWindowAndIsIdempotent() => Sta.Run(() =>
        {
            var sink = new WindowMessageSink(NotifyIconVersion.Vista);
            var hwnd = sink.GetMessageWindowHandle();

            sink.Dispose();
            sink.Dispose();

            Assert.True(sink.IsDisposed);
            Assert.False(Sta.IsWindow(hwnd));
        });
    }
}
