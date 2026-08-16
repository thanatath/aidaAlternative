using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ThreadingTimer = System.Threading.Timer;

namespace aidaAlternative.Services
{
    public sealed class DisplayGuard : IDisposable
    {
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;

        private readonly int processId = Environment.ProcessId;
        private readonly ThreadingTimer timer;

        public DisplayGuard()
        {
            timer = new ThreadingTimer(_ => Enforce(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(250));
        }

        private void Enforce()
        {
            // ponytail: resolved per tick, the small panel gets a new DeviceName when it is replugged
            var restricted = Program.FindTargetScreen();
            var primary = Screen.PrimaryScreen;
            if (restricted == null || primary == null || restricted.DeviceName == primary.DeviceName) return;

            MoveCursorToPrimary(restricted, primary);
            EnumWindowsProc callback = (handle, _) => MoveWindowToPrimary(handle, restricted, primary);
            EnumWindows(callback, IntPtr.Zero);
            GC.KeepAlive(callback);
        }

        private bool MoveWindowToPrimary(IntPtr handle, Screen restricted, Screen primary)
        {
            if (!IsWindowVisible(handle) || IsIconic(handle) || IsShellSurface(handle)) return true;

            GetWindowThreadProcessId(handle, out var windowProcessId);
            if (windowProcessId == processId || Screen.FromHandle(handle).DeviceName != restricted.DeviceName)
                return true;

            if (!GetWindowRect(handle, out var bounds)) return true;

            var location = MapToPrimary(bounds.ToRectangle(), restricted.Bounds, primary.WorkingArea);
            SetWindowPos(handle, IntPtr.Zero, location.X, location.Y, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);
            return true;
        }

        private static void MoveCursorToPrimary(Screen restricted, Screen primary)
        {
            if (!restricted.Bounds.Contains(Cursor.Position)) return;

            Cursor.Position = MapToPrimary(new Rectangle(Cursor.Position, Size.Empty), restricted.Bounds, primary.WorkingArea);
        }

        internal static Point MapToPrimary(Rectangle source, Rectangle restrictedBounds, Rectangle primaryBounds)
        {
            var x = primaryBounds.Left + source.Left - restrictedBounds.Left;
            var y = primaryBounds.Top + source.Top - restrictedBounds.Top;
            var maxX = Math.Max(primaryBounds.Left, primaryBounds.Right - Math.Max(1, source.Width));
            var maxY = Math.Max(primaryBounds.Top, primaryBounds.Bottom - Math.Max(1, source.Height));
            return new Point(
                Math.Clamp(x, primaryBounds.Left, maxX),
                Math.Clamp(y, primaryBounds.Top, maxY));
        }

        [Conditional("DEBUG")]
        internal static void AssertPositionMapping()
        {
            Debug.Assert(MapToPrimary(new Rectangle(970, 20, 100, 50), new Rectangle(960, 0, 960, 640), new Rectangle(0, 0, 1920, 1080)) == new Point(10, 20));
            Debug.Assert(IsShellSurface("Shell_TrayWnd") && !IsShellSurface("CabinetWClass"));
        }

        private static bool IsShellSurface(IntPtr handle)
        {
            var className = new StringBuilder(256);
            GetClassName(handle, className, className.Capacity);
            return IsShellSurface(className.ToString());
        }

        private static bool IsShellSurface(string className) => className is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd";

        public void Dispose() => timer.Dispose();

        private delegate bool EnumWindowsProc(IntPtr handle, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr handle, StringBuilder className, int maxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr handle, out NativeRectangle rectangle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
        }
    }
}
