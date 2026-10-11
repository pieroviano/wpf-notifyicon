using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using NotifyIconWpf.Tests.Infrastructure;

namespace NotifyIconWpf.Tests
{
    /// <summary>
    /// Verifies which shell mouse messages open the tray popup / context menu for every
    /// <see cref="PopupActivationMode"/>.
    /// </summary>
    public class TaskbarIconActivationTests
    {
        private static readonly WindowsMessages[] Messages =
        {
            WindowsMessages.WM_MOUSEMOVE,
            WindowsMessages.WM_LBUTTONDOWN,
            WindowsMessages.WM_LBUTTONUP,
            WindowsMessages.WM_LBUTTONDBLCLK,
            WindowsMessages.WM_RBUTTONDOWN,
            WindowsMessages.WM_RBUTTONUP,
            WindowsMessages.WM_MBUTTONDOWN,
            WindowsMessages.WM_MBUTTONUP,
            WindowsMessages.NIN_BALLOONUSERCLICK,
        };

        public static TheoryData<PopupActivationMode, WindowsMessages, bool> Matrix
        {
            get
            {
                var data = new TheoryData<PopupActivationMode, WindowsMessages, bool>();
                foreach (PopupActivationMode mode in Enum.GetValues(typeof(PopupActivationMode)))
                {
                    foreach (var message in Messages)
                    {
                        data.Add(mode, message, IsExpectedMatch(mode, message));
                    }
                }

                return data;
            }
        }

        private static bool IsExpectedMatch(PopupActivationMode mode, WindowsMessages message)
        {
            bool left = message == WindowsMessages.WM_LBUTTONUP;
            bool right = message == WindowsMessages.WM_RBUTTONUP;
            bool middle = message == WindowsMessages.WM_MBUTTONUP;
            bool dbl = message == WindowsMessages.WM_LBUTTONDBLCLK;
            switch (mode)
            {
                case PopupActivationMode.LeftClick: return left;
                case PopupActivationMode.RightClick: return right;
                case PopupActivationMode.DoubleClick: return dbl;
                case PopupActivationMode.LeftOrRightClick: return left || right;
                case PopupActivationMode.LeftOrDoubleClick: return left || dbl;
                case PopupActivationMode.MiddleClick: return middle;
                case PopupActivationMode.All: return message != WindowsMessages.WM_MOUSEMOVE;
                default: return false;
            }
        }

        [Theory]
        [MemberData(nameof(Matrix))]
        public void PopupActivation_OpensTrayPopupOnlyForMatchingMessages(PopupActivationMode mode, WindowsMessages message, bool expected) => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon
            {
                NoLeftClickDelay = true,
                PopupActivation = mode,
                MenuActivation = PopupActivationMode.None,
                TrayPopup = new Border { Width = 10, Height = 10 },
            };
            int opened = 0;
            icon.TrayPopupOpen += (s, e) => opened++;

            icon.SendTrayMessage(message);

            if (expected) Assert.True(Sta.PumpUntil(() => opened == 1));
            else Sta.PumpFor(TimeSpan.FromMilliseconds(100));

            Assert.Equal(expected ? 1 : 0, opened);
            icon.CloseTrayPopup();
        });

        [Theory]
        [MemberData(nameof(Matrix))]
        public void MenuActivation_OpensContextMenuOnlyForMatchingMessages(PopupActivationMode mode, WindowsMessages message, bool expected) => Sta.Run(() =>
        {
            var menu = new ContextMenu();
            using var icon = new TaskbarIcon
            {
                NoLeftClickDelay = true,
                PopupActivation = PopupActivationMode.None,
                MenuActivation = mode,
                ContextMenu = menu,
            };
            int opened = 0;
            icon.TrayContextMenuOpen += (s, e) => opened++;

            icon.SendTrayMessage(message);

            if (expected) Assert.True(Sta.PumpUntil(() => opened == 1));
            else Sta.PumpFor(TimeSpan.FromMilliseconds(100));

            Assert.Equal(expected ? 1 : 0, opened);
            menu.IsOpen = false;
        });
    }
}
