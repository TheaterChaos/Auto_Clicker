using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsInput;

namespace Auto_Clicker
{
    public class RecorderFunc
    {

        private Form1 mainForm;

        public RecorderFunc(Form1 form)
        {
            mainForm = form;
        }

        private KeyboardHook kHook;
        private MouseHook mHook;
        private Stopwatch sw = new Stopwatch();
        public List<Common.InputAction> RecordedActions = new List<Common.InputAction>();
        private bool recording = false;

        public void playbackstart()
        {
            if (RecordedActions.Count == 0)
            {
                MessageBox.Show("Keine Aktionen aufgenommen!");
                return;
            }

            // Playback starten
            InputPlayer.PlayActions(RecordedActions);
        }

        public void StartRecording()
        {
            if (recording) return;

            RecordedActions.Clear();
            sw.Restart();

            kHook = new KeyboardHook();
            mHook = new MouseHook();

            kHook.KeyDownEvent += key => RecordKey(key, true);
            kHook.KeyUpEvent += key => RecordKey(key, false);

            mHook.MouseDownEvent += button => RecordMouse(button, true);
            mHook.MouseUpEvent += button => RecordMouse(button, false);

            mHook.MouseMoveEvent += (x, y) =>
            {
                long delay = sw.ElapsedMilliseconds;
                RecordedActions.Add(new Common.InputAction
                {
                    Type = Common.InputType.Mouse,
                    IsDown = false,
                    MouseX = x,
                    MouseY = y,
                    Delay = delay
                });
                sw.Restart();
            };

            recording = true;
            mainForm.InfoLabel.Text = "Recorder läuft. ESC beendet die Aufnahme.";
        }

        private void btnStopRecording_Click(object sender, EventArgs e)
        {
            StopRecording();
            mainForm.InfoLabel.Text = "Aufnahme beendet. " + RecordedActions.Count + " Aktionen aufgenommen.";
        }

        private void StopRecording()
        {
            if (!recording) return;
            kHook.Dispose();
            mHook.Dispose();
            sw.Stop();
            recording = false;
        }

        private void RecordKey(Keys key, bool isDown)
        {
            if (key == Keys.Escape)
            {
                StopRecording();
                return;
            }
            long delay = sw.ElapsedMilliseconds;
            RecordedActions.Add(new Common.InputAction { Type = Common.InputType.Key, Key = key, IsDown = isDown, Delay = delay });
            sw.Restart();

            Debug.WriteLine($"Key: {key}, IsDown: {isDown}, Delay: {delay}");
        }

        private void RecordMouse(MouseHook.MouseButton button, bool isDown)
        {
            long delay = sw.ElapsedMilliseconds;
            RecordedActions.Add(new Common.InputAction { Type = Common.InputType.Mouse, Button = (Common.MouseButton)button, IsDown = isDown, Delay = delay });
            sw.Restart();

            Debug.WriteLine($"Mouse Button: {button}, IsDown: {isDown}, Delay: {delay}");
        }

    }

    namespace Common
    {
        public enum InputType { Key, Mouse }
        public enum MouseButton { None, Left, Right, Middle }

        public class InputAction
        {
            public InputType Type;
            public bool IsDown;
            public Keys Key;
            public MouseButton Button;
            public long Delay;
            public int MouseX;
            public int MouseY;
        }
    }

    public class InputPlayer
    {

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        private static bool IsKeyDown(Keys key)
        {
            return (GetAsyncKeyState(key) & 0x8000) != 0;
        }

        private static InputSimulator sim = new InputSimulator();

        public static void PlayActions(List<Common.InputAction> actions)
        {
            foreach (var act in actions)
            {
                if (IsKeyDown(Keys.Escape))
                {
                    break; // Playback abbrechen
                }

                // Wartezeit einhalten
                Thread.Sleep((int)act.Delay);

                if (act.Type == Common.InputType.Key)
                {
                    if (act.IsDown)
                        sim.Keyboard.KeyDown((VirtualKeyCode)act.Key);
                    else
                        sim.Keyboard.KeyUp((VirtualKeyCode)act.Key);
                }
                if (act.Type == Common.InputType.Mouse)
                {
                    if (act.Button is 0 )
                    {
                        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                        double absoluteX = act.MouseX * 65535.0 / (screenWidth - 1);
                        double absoluteY = act.MouseY * 65535.0 / (screenHeight - 1);

                        new InputSimulator().Mouse.MoveMouseTo(absoluteX, absoluteY);
                        Debug.WriteLine($"Mouse Move to X: {act.MouseX}, Y: {act.MouseY} (Absolute: {absoluteX}, {absoluteY})");
                    }
                    else
                    {
                        // Mausbutton
                        switch (act.Button)
                        {
                            case (Common.MouseButton)MouseHook.MouseButton.Left:
                                if (act.IsDown) sim.Mouse.LeftButtonDown(); else sim.Mouse.LeftButtonUp();
                                break;
                            case (Common.MouseButton)MouseHook.MouseButton.Right:
                                if (act.IsDown) sim.Mouse.RightButtonDown(); else sim.Mouse.RightButtonUp();
                                break;
                            case (Common.MouseButton)MouseHook.MouseButton.Middle:
                                if (act.IsDown) sim.Mouse.MiddleButtonDown(); else sim.Mouse.MiddleButtonUp();
                                break;
                        }
                    }
                }
                Debug.WriteLine($"Action: {act.Type}, Key/Button: {act.Key}{act.Button}, IsDown: {act.IsDown}, Delay: {act.Delay}, MouseX: {act.MouseX}, MouseY: {act.MouseY}");
            }
        }
    }

    // === Keyboard Hook ===
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event Action<Keys> KeyDownEvent;
        public event Action<Keys> KeyUpEvent;

        public KeyboardHook()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        public void Dispose() => UnhookWindowsHookEx(_hookID);

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int w = wParam.ToInt32();
                int vkCode = Marshal.ReadInt32(lParam);
                if (w == WM_KEYDOWN) KeyDownEvent?.Invoke((Keys)vkCode);
                else if (w == WM_KEYUP) KeyUpEvent?.Invoke((Keys)vkCode);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    // === Mouse Hook ===
    public class MouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MOUSEMOVE = 0x0200;

        private LowLevelMouseProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public enum MouseButton { Left, Right, Middle }
        public event Action<MouseButton> MouseDownEvent;
        public event Action<MouseButton> MouseUpEvent;

        public MouseHook()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        public void Dispose() => UnhookWindowsHookEx(_hookID);

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();

                if (msg == WM_MOUSEMOVE)
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    MouseMoveEvent?.Invoke(hookStruct.pt.x, hookStruct.pt.y);
                }

                switch (msg)
                {
                    case WM_LBUTTONDOWN: MouseDownEvent?.Invoke(MouseButton.Left); break;
                    case WM_LBUTTONUP: MouseUpEvent?.Invoke(MouseButton.Left); break;
                    case WM_RBUTTONDOWN: MouseDownEvent?.Invoke(MouseButton.Right); break;
                    case WM_RBUTTONUP: MouseUpEvent?.Invoke(MouseButton.Right); break;
                    case WM_MBUTTONDOWN: MouseDownEvent?.Invoke(MouseButton.Middle); break;
                    case WM_MBUTTONUP: MouseUpEvent?.Invoke(MouseButton.Middle); break;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
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

        public event Action<int, int> MouseMoveEvent;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
