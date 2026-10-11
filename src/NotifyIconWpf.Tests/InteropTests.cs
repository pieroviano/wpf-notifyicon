using System.Runtime.InteropServices;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using NotifyIconWpf.Tests.Infrastructure;

namespace NotifyIconWpf.Tests
{
    public class InteropTests
    {
        [Fact]
        public void AppBarInfo_ReadsSystemTaskbarPosition() => Sta.Run(() =>
        {
            var info = new AppBarInfo();

            info.GetSystemTaskBarPosition();

            Assert.InRange(info.Edge, AppBarInfo.ScreenEdge.Left, AppBarInfo.ScreenEdge.Bottom);
            Assert.True(info.WorkArea.Width > 0);
            Assert.True(info.WorkArea.Height > 0);
        });

        [Fact]
        public void TrayInfo_GetTrayLocation_HonoursSpacing() => Sta.Run(() =>
        {
            var info = new AppBarInfo();
            info.GetSystemTaskBarPosition();

            var near = TrayInfo.GetTrayLocation(0);
            var far = TrayInfo.GetTrayLocation(100);

            int dx = far.X - near.X, dy = far.Y - near.Y;
            switch (info.Edge)
            {
                case AppBarInfo.ScreenEdge.Bottom:
                    Assert.Equal(0, dx);
                    Assert.True(dy < 0);
                    break;
                case AppBarInfo.ScreenEdge.Top:
                    Assert.Equal(0, dx);
                    Assert.True(dy > 0);
                    break;
                case AppBarInfo.ScreenEdge.Left:
                    Assert.True(dx > 0);
                    Assert.Equal(0, dy);
                    break;
                case AppBarInfo.ScreenEdge.Right:
                    Assert.True(dx < 0);
                    Assert.Equal(0, dy);
                    break;
            }
        });

        [Fact]
        public void SystemInfo_ScaleWithDpi_DividesByDpiFactors() => Sta.Run(() =>
        {
            var point = new Point { X = 960, Y = 540 };
            var size = new Size { Width = 64, Height = 32 };

            var scaledPoint = point.ScaleWithDpi();
            var scaledSize = size.ScaleWithDpi();
            var device = TrayInfo.GetDeviceCoordinates(point);

            Assert.Equal((int)(960 / SystemInfo.DpiFactorX), scaledPoint.X);
            Assert.Equal((int)(540 / SystemInfo.DpiFactorY), scaledPoint.Y);
            Assert.Equal((int)(64 / SystemInfo.DpiFactorX), scaledSize.Width);
            Assert.Equal((int)(32 / SystemInfo.DpiFactorY), scaledSize.Height);
            Assert.Equal(scaledPoint.X, device.X);
            Assert.Equal(scaledPoint.Y, device.Y);
        });

        [Fact]
        public void SystemInfo_SmallIconSize_IsPositiveAndCached() => Sta.Run(() =>
        {
            var first = SystemInfo.SmallIconSize;
            var second = SystemInfo.SmallIconSize;

            Assert.True(first.Width > 0);
            Assert.True(first.Height > 0);
            Assert.Equal(first, second);
        });

        [Fact]
        public void NotifyIconData_CreateDefault_InitializesStructure()
        {
            var handle = new IntPtr(0x1234);

            var data = NotifyIconData.CreateDefault(handle);

            Assert.Equal((uint)Marshal.SizeOf(typeof(NotifyIconData)), data.cbSize);
            Assert.Equal(handle, data.WindowHandle);
            Assert.Equal(0u, data.TaskbarIconId);
            Assert.Equal((uint)WindowMessageSink.CallbackMessageId, data.CallbackMessageId);
            Assert.Equal((uint)NotifyIconVersion.Win95, data.VersionOrTimeout);
            Assert.Equal(IntPtr.Zero, data.IconHandle);
            Assert.Equal(IconState.Hidden, data.IconState);
            Assert.Equal(IconState.Hidden, data.StateMask);
            Assert.Equal(IconDataMembers.Message | IconDataMembers.Icon | IconDataMembers.Tip, data.ValidMembers);
            Assert.Equal(string.Empty, data.ToolTipText);
            Assert.Equal(string.Empty, data.BalloonText);
            Assert.Equal(string.Empty, data.BalloonTitle);
        }
    }
}
