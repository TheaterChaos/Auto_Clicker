using System.Diagnostics;
using System.Drawing.Text;
using System.Numerics;
using System.Runtime.InteropServices;
using WindowsInput;

namespace Auto_Clicker
{
    public enum RecActionType
    {
        KeyDown,
        KeyUp,
        MouseDown,
        MouseUp,
        MouseMove
    }

    public abstract class RecAction
    {
        public RecActionType Type { get; set; }
        public int DelayMs { get; set; }
    }

    public sealed class RecKeyAction : RecAction
    {
        public Keys Key { get; set; }
    }

    public sealed class RecMouseButtonAction : RecAction
    {
        public MouseButtons Button { get; set; }
    }

    public sealed class RecMouseMoveAction : RecAction
    {
        public int X { get; set; }
        public int Y { get; set; }
    }



    public sealed class RecorderFunc
    {
        private readonly Form _Main;

        public List<RecAction> RecActions { get; } = new();

        private KeyboardHook kHook;
        private MouseHook mHook;

        private readonly Stopwatch sw = new();
        private long lastActionTime;
        private long lastMouseMoveTime;
        private int lastX, lastY;

        public RecorderFunc(Form form)
        {
            _Main = form;
        }

        // ---------------- RECORD ----------------

        public void StartRecording()
        {
            RecActions.Clear();
            sw.Restart();
            lastActionTime = 0;

            kHook = new KeyboardHook();
            mHook = new MouseHook();

            kHook.KeyDownEvent += k =>
            {
                if (k == Keys.Escape) StopRecording();
                Add(new RecKeyAction
                {
                    Type = RecActionType.KeyDown,
                    Key = k
                });
            };

            kHook.KeyUpEvent += k =>
                Add(new RecKeyAction
                {
                    Type = RecActionType.KeyUp,
                    Key = k
                });

            mHook.MouseDownEvent += b =>
                Add(new RecMouseButtonAction
                {
                    Type = RecActionType.MouseDown,
                    Button = ToWinBtn(b)
                });

            mHook.MouseUpEvent += b =>
                Add(new RecMouseButtonAction
                {
                    Type = RecActionType.MouseUp,
                    Button = ToWinBtn(b)
                });

            mHook.MouseMoveEvent += OnMouseMove;
        }

        public void StopRecording()
        {
            kHook?.Dispose();
            mHook?.Dispose();
            sw.Stop();
        }

        private void Add(RecAction act)
        {
            long now = sw.ElapsedMilliseconds;
            act.DelayMs = (int)(now - lastActionTime);
            lastActionTime = now;
            RecActions.Add(act);
        }

        private void OnMouseMove(int x, int y)
        {
            long now = sw.ElapsedMilliseconds;

            if (now - lastMouseMoveTime < 15) return;
            if (Math.Abs(x - lastX) < 3 && Math.Abs(y - lastY) < 3) return;

            lastMouseMoveTime = now;
            lastX = x;
            lastY = y;

            Add(new RecMouseMoveAction
            {
                Type = RecActionType.MouseMove,
                X = x,
                Y = y
            });
        }

        private static MouseButtons ToWinBtn(MouseHook.MouseButton b) =>
            b switch
            {
                MouseHook.MouseButton.Left => MouseButtons.Left,
                MouseHook.MouseButton.Right => MouseButtons.Right,
                MouseHook.MouseButton.Middle => MouseButtons.Middle,
                _ => MouseButtons.None
            };

        // ---------------- PLAYBACK ----------------

        private CancellationTokenSource cts;
        private readonly InputSimulator sim = new();

        public async Task StartPlaybackAsync(double speed = 1.0)
        {
            if (RecActions.Count == 0) return;

            cts = new CancellationTokenSource();

            try
            {
                foreach (var act in RecActions)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    if (act.DelayMs > 0)
                        await Task.Delay(
                            (int)(act.DelayMs / speed),
                            cts.Token);

                    Execute(act);
                }
            }
            catch (OperationCanceledException) { }
        }

        public void StopPlayback()
        {
            cts?.Cancel();
        }

        private void Execute(RecAction act)
        {
            switch (act)
            {
                case RecKeyAction k:
                    if (act.Type == RecActionType.KeyDown)
                        sim.Keyboard.KeyDown((VirtualKeyCode)k.Key);
                    else
                        sim.Keyboard.KeyUp((VirtualKeyCode)k.Key);
                    break;

                case RecMouseButtonAction m:
                    if (m.Button == MouseButtons.Left)
                    {
                        if (act.Type == RecActionType.MouseDown)
                            sim.Mouse.LeftButtonDown();
                        else
                            sim.Mouse.LeftButtonUp();
                    }
                    else if (m.Button == MouseButtons.Right)
                    {
                        if (act.Type == RecActionType.MouseDown)
                            sim.Mouse.RightButtonDown();
                        else
                            sim.Mouse.RightButtonUp();
                    }
                    else if (m.Button == MouseButtons.Middle)
                    {
                        if (act.Type == RecActionType.MouseDown)
                            sim.Mouse.MiddleButtonDown();
                        else
                            sim.Mouse.MiddleButtonUp();
                    }
                    break;

                case RecMouseMoveAction mm:
                    var s = Screen.PrimaryScreen.Bounds;
                    sim.Mouse.MoveMouseTo(
                        mm.X * 65535d / s.Width,
                        mm.Y * 65535d / s.Height);
                    break;
            }
        }
    }

    // =====================================================
    // KEYBOARD HOOK
    // =====================================================

    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private IntPtr hookId;
        private LowLevelKeyboardProc proc;

        public event Action<Keys> KeyDownEvent;
        public event Action<Keys> KeyUpEvent;

        public KeyboardHook()
        {
            proc = HookCallback;
            hookId = SetHook(proc);
        }

        public void Dispose() => UnhookWindowsHookEx(hookId);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                int vk = Marshal.ReadInt32(lParam);

                if (msg == WM_KEYDOWN) KeyDownEvent?.Invoke((Keys)vk);
                else if (msg == WM_KEYUP) KeyUpEvent?.Invoke((Keys)vk);
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var p = Process.GetCurrentProcess();
            using var m = p.MainModule;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(m.ModuleName), 0);
        }

        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint threadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string name);
    }

    // =====================================================
    // MOUSE HOOK
    // =====================================================

    public class MouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;

        public enum MouseButton { Left, Right, Middle }

        public event Action<int, int> MouseMoveEvent;
        public event Action<MouseButton> MouseDownEvent;
        public event Action<MouseButton> MouseUpEvent;

        private IntPtr hookId;
        private LowLevelMouseProc proc;

        public MouseHook()
        {
            proc = HookCallback;
            hookId = SetHook(proc);
        }

        public void Dispose() => UnhookWindowsHookEx(hookId);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                if (msg == WM_MOUSEMOVE)
                    MouseMoveEvent?.Invoke(data.pt.x, data.pt.y);

                if (msg == WM_LBUTTONDOWN) MouseDownEvent?.Invoke(MouseButton.Left);
                else if (msg == WM_LBUTTONUP) MouseUpEvent?.Invoke(MouseButton.Left);
                else if (msg == WM_RBUTTONDOWN) MouseDownEvent?.Invoke(MouseButton.Right);
                else if (msg == WM_RBUTTONUP) MouseUpEvent?.Invoke(MouseButton.Right);
                else if (msg == WM_MBUTTONDOWN) MouseDownEvent?.Invoke(MouseButton.Middle);
                else if (msg == WM_MBUTTONUP) MouseUpEvent?.Invoke(MouseButton.Middle);
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using var p = Process.GetCurrentProcess();
            using var m = p.MainModule;
            return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(m.ModuleName), 0);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint threadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string name);
    }
}
