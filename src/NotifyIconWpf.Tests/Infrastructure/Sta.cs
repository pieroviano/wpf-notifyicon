using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Hardcodet.Wpf.TaskbarNotification.Interop;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace NotifyIconWpf.Tests.Infrastructure
{
    /// <summary>
    /// Runs WPF test bodies on a dedicated STA thread with its own dispatcher and
    /// drives the library through real window messages.
    /// </summary>
    internal static class Sta
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        public static void Run(Action body)
        {
            ExceptionDispatchInfo failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    body();
                }
                catch (Exception e)
                {
                    failure = ExceptionDispatchInfo.Capture(e);
                }
                finally
                {
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            if (!thread.Join(TimeSpan.FromSeconds(60)))
            {
                throw new TimeoutException("STA test body did not complete.");
            }

            failure?.Throw();
        }

        /// <summary>Pumps the current dispatcher until <paramref name="condition"/> holds or the timeout elapses.</summary>
        public static bool PumpUntil(Func<bool> condition, TimeSpan? timeout = null)
        {
            var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline) return false;
                PumpFor(TimeSpan.FromMilliseconds(10));
            }

            return true;
        }

        /// <summary>Pumps the current dispatcher for a fixed amount of time.</summary>
        public static void PumpFor(TimeSpan duration)
        {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = duration };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                frame.Continue = false;
            };
            timer.Start();
            Dispatcher.PushFrame(frame);
        }

        public static IntPtr GetMessageWindowHandle(this WindowMessageSink sink)
        {
            var property = typeof(WindowMessageSink).GetProperty("MessageWindowHandle",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (IntPtr)property.GetValue(sink);
        }

        public static WindowMessageSink GetMessageSink(this TaskbarIcon icon)
        {
            var field = typeof(TaskbarIcon).GetField("messageSink", BindingFlags.Instance | BindingFlags.NonPublic);
            return (WindowMessageSink)field.GetValue(icon);
        }

        /// <summary>Sends a taskbar callback message, as the shell does, to the icon's message window.</summary>
        public static void SendTrayMessage(this WindowMessageSink sink, WindowsMessages message, IntPtr wParam = default)
        {
            SendMessage(sink.GetMessageWindowHandle(), WindowMessageSink.CallbackMessageId, wParam, (IntPtr)(uint)message);
        }

        public static void SendTrayMessage(this TaskbarIcon icon, WindowsMessages message, IntPtr wParam = default)
        {
            icon.GetMessageSink().SendTrayMessage(message, wParam);
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern uint RegisterWindowMessage(string lpString);
    }
}
