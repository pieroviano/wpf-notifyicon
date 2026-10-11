using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using NotifyIconWpf.Tests.Infrastructure;

namespace NotifyIconWpf.Tests
{
    public class TaskbarIconMouseTests
    {
        public static TheoryData<WindowsMessages, string> RoutedMouseEvents => new TheoryData<WindowsMessages, string>
        {
            { WindowsMessages.WM_MOUSEMOVE, nameof(TaskbarIcon.TrayMouseMove) },
            { WindowsMessages.WM_LBUTTONDOWN, nameof(TaskbarIcon.TrayLeftMouseDown) },
            { WindowsMessages.WM_LBUTTONUP, nameof(TaskbarIcon.TrayLeftMouseUp) },
            { WindowsMessages.WM_RBUTTONDOWN, nameof(TaskbarIcon.TrayRightMouseDown) },
            { WindowsMessages.WM_RBUTTONUP, nameof(TaskbarIcon.TrayRightMouseUp) },
            { WindowsMessages.WM_MBUTTONDOWN, nameof(TaskbarIcon.TrayMiddleMouseDown) },
            { WindowsMessages.WM_MBUTTONUP, nameof(TaskbarIcon.TrayMiddleMouseUp) },
            { WindowsMessages.WM_LBUTTONDBLCLK, nameof(TaskbarIcon.TrayMouseDoubleClick) },
            { WindowsMessages.NIN_BALLOONUSERCLICK, nameof(TaskbarIcon.TrayBalloonTipClicked) },
            { WindowsMessages.NIN_BALLOONSHOW, nameof(TaskbarIcon.TrayBalloonTipShown) },
            { WindowsMessages.NIN_BALLOONHIDE, nameof(TaskbarIcon.TrayBalloonTipClosed) },
        };

        [Theory]
        [MemberData(nameof(RoutedMouseEvents))]
        public void ShellMessage_RaisesRoutedEvent(WindowsMessages message, string eventName) => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var raised = new List<string>();
            foreach (var evt in typeof(TaskbarIcon).GetEvents())
            {
                if (evt.EventHandlerType != typeof(RoutedEventHandler)) continue;
                string name = evt.Name;
                evt.AddEventHandler(icon, new RoutedEventHandler((s, e) => raised.Add(name)));
            }

            icon.SendTrayMessage(message);

            Assert.Equal(1, raised.Count(n => n == eventName));
        });

        [Fact]
        public void DoubleClick_ExecutesDoubleClickCommandWithParameter() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var command = new RecordingCommand();
            icon.DoubleClickCommand = command;
            icon.DoubleClickCommandParameter = "dbl";

            icon.SendTrayMessage(WindowsMessages.WM_LBUTTONDBLCLK);

            Assert.Equal(new object[] { "dbl" }, command.Executions);
        });

        [Fact]
        public void DoubleClick_DoesNotExecuteDisabledCommand() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var command = new RecordingCommand { CanExecuteResult = false };
            icon.DoubleClickCommand = command;

            icon.SendTrayMessage(WindowsMessages.WM_LBUTTONDBLCLK);

            Assert.Empty(command.Executions);
        });

        [Fact]
        public void LeftClick_ExecutesLeftClickCommandAfterDelay() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon { NoLeftClickDelay = true };
            var command = new RecordingCommand();
            icon.LeftClickCommand = command;
            icon.LeftClickCommandParameter = 42;

            icon.SendTrayMessage(WindowsMessages.WM_LBUTTONUP);

            Assert.True(Sta.PumpUntil(() => command.Executions.Count == 1));
            Assert.Equal(42, command.Executions[0]);
        });

        [Fact]
        public void DoubleClick_CancelsPendingLeftClickCommand() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var command = new RecordingCommand();
            icon.LeftClickCommand = command;

            icon.SendTrayMessage(WindowsMessages.WM_LBUTTONUP);
            icon.SendTrayMessage(WindowsMessages.WM_LBUTTONDBLCLK);

            Sta.PumpFor(TimeSpan.FromMilliseconds(WinFormsDoubleClickTime() + 300));
            Assert.Empty(command.Executions);
        });

        [Fact]
        public void LeftClick_RoutedCommand_ExecutesOnTarget() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon { NoLeftClickDelay = true };
            var routed = new RoutedCommand("Click", typeof(TaskbarIconMouseTests));
            var target = new Border();
            object executedWith = null;
            target.CommandBindings.Add(new CommandBinding(routed, (s, e) => executedWith = e.Parameter, (s, e) => e.CanExecute = true));
            icon.LeftClickCommand = routed;
            icon.LeftClickCommandTarget = target;
            icon.LeftClickCommandParameter = "p";

            icon.SendTrayMessage(WindowsMessages.WM_LBUTTONUP);

            Assert.True(Sta.PumpUntil(() => executedWith != null));
            Assert.Equal("p", executedWith);
        });

        [Fact]
        public void DoubleClick_RoutedCommandThatCannotExecute_IsNotExecuted() => Sta.Run(() =>
        {
            using var icon = new TaskbarIcon();
            var routed = new RoutedCommand("Dbl", typeof(TaskbarIconMouseTests));
            bool executed = false;
            icon.CommandBindings.Add(new CommandBinding(routed, (s, e) => executed = true, (s, e) => e.CanExecute = false));
            icon.DoubleClickCommand = routed;

            icon.SendTrayMessage(WindowsMessages.WM_LBUTTONDBLCLK);

            Assert.False(executed);
        });

        private static int WinFormsDoubleClickTime() => System.Windows.Forms.SystemInformation.DoubleClickTime;
    }

    internal sealed class RecordingCommand : ICommand
    {
        public List<object> Executions { get; } = new List<object>();

        public bool CanExecuteResult { get; set; } = true;

        public event EventHandler CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object parameter) => CanExecuteResult;

        public void Execute(object parameter) => Executions.Add(parameter);
    }
}
