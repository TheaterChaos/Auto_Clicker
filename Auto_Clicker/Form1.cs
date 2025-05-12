using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using WindowsInput;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;



namespace Auto_Clicker
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /*private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;*/
        private const int MOUSEEVENTF_XDOWN = 0x0080;
        private const int MOUSEEVENTF_XUP = 0x0100;
        private const int XBUTTON1 = 0x0001;
        private const int XBUTTON2 = 0x0002;

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;


        private bool clicking = false;
        private Keys hotkey = Keys.F6;
        private Keys clickKey = Keys.LButton; // Standard: Linksklick
        private bool keyWasDown = false;

        private List<ClickOrKeyAction> SavedActions = new List<ClickOrKeyAction>();

        private List<Form> activeMarkers = new();

        private List<string> BlacklistedWindowTitles = new List<string>();

        private List<string> AppsCheckedlist = new List<string>();

        private List<string> AllFoundAppsList = new List<string>();

        private int clickIndex = 0;

        private CursorOverlayForm? cursorOverlay;
        private OverlaySettingsForm? settingsForm;

        private CancellationTokenSource? clickCts;
        private readonly object clickLock = new();

        private CursorOverlayForm Clickoverlay;

        private System.Windows.Forms.Timer hotkeyTimer;


        private void SaveSettings()
        {
            Properties.Settings.Default.UseMouseMode = UseMouse.Checked; // true = Maus, false = Tastatur

            Properties.Settings.Default.Hotkey = hotkey.ToString(); // Hotkey für den Autoclicker
            Properties.Settings.Default.HoldOrSwitch = HoldToClick.Checked ? "hold" : "switch"; // "hold" oder "switch"
            Properties.Settings.Default.PositionButton = PositionIsChecked.Checked; // true = Position speichern, false = keine Position speichern
            Properties.Settings.Default.Whitlistchecked = WhitelistappsCheck.Checked; // true = Whitelist, false = Blacklist

            Properties.Settings.Default.Clickkey = clickKey.ToString(); // Taste für den Klick

            Properties.Settings.Default.ClickMode = ClicksPersSecButton.Checked ? "cps" : "time"; // "cps" oder "time"
            Properties.Settings.Default.ClicksPerSec = ClickPerSecNum.Value; // Klicks pro Sekunde
            Properties.Settings.Default.ClickTimeValue = PerTimeNum.Value; // Zeit zwischen Klicks
            Properties.Settings.Default.ClickTimeUnit = PerTimeValue.SelectedItem?.ToString() ?? "ms"; // Einheit für die Zeit (ms, sec, min, hours)

            Properties.Settings.Default.RepeatInfinite = RepeatUnlimited.Checked; // true = unendlich, false = wiederholen
            Properties.Settings.Default.RepeatCount = RepeatTimes.Value; // Anzahl der Wiederholungen

            var parts = SavedActions.Select(action =>
            {
                if (action.Type == ActionType.MouseClick)
                    return $"M:{action.MousePosition.X}:{action.MousePosition.Y}";
                else if (action.Type == ActionType.KeyPress && action.Key.HasValue)
                    return $"K:{action.Key}";
                return null;
            }).Where(s => s != null);
            Properties.Settings.Default.SavedPoints = string.Join(";", parts); // Speichern der gespeicherten Punkte

            Properties.Settings.Default.ShowPointCLick = ShowPointOnClick.Checked; // true = Punkt anzeigen, false = keinen Punkt anzeigen

            Properties.Settings.Default.SaveOnExit = SettingsSaveonexit.Checked; // true = Einstellungen speichern beim Schließen
            Properties.Settings.Default.SetOnTop = setTopMostMenu.Checked;  // true = Fenster immer im Vordergrund
            Properties.Settings.Default.DisableWindowOnPosition = disableWindowOnPositionMenu.Checked; // true = Fenster deaktivieren, wenn Position gespeichert ist
            Properties.Settings.Default.DisableredBox = disableRedBoxMenu.Checked; // true = rote Box deaktivieren
            Properties.Settings.Default.SaveAppsListToo = SaveAppsToOnExitMenu.Checked; // true = Liste der Fenster speichern
            Properties.Settings.Default.TooltipShowHide = AddTooltipsMenu.Checked; // true = Tooltip anzeigen, false = keinen Tooltip anzeigen

            if (SaveAppsToOnExitMenu.Checked)
            {
                var Blackcollection = new System.Collections.Specialized.StringCollection();
                Blackcollection.AddRange(BlacklistedWindowTitles.ToArray());
                Properties.Settings.Default.BlacklistedApps = Blackcollection;

                var Whitecollection = new System.Collections.Specialized.StringCollection();
                Whitecollection.AddRange(AppsCheckedlist.ToArray());
                Properties.Settings.Default.AppsChecked = Whitecollection;
            }
            Properties.Settings.Default.Save(); // Speichern der Einstellungen
        }

        private void LoadSettings()
        {
            if (Properties.Settings.Default.UseMouseMode) // true = Maus, false =  Tastatur
            {
                UseMouse.Checked = true;
            }
            else
            {
                UseKeyboard.Checked = true;
            }

            if (Properties.Settings.Default.ShowPointCLick) // true = Punkt anzeigen, false = keinen Punkt anzeigen
            {
                ShowPointOnClick.Checked = true;
            }
            else
            {
                ShowPointOnClick.Checked = false;
            }
            if (Properties.Settings.Default.SaveAppsListToo) // true = Liste der Fenster speichern
            {
                SaveAppsToOnExitMenu.Checked = true;
                SaveAppsToOnExitMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
            else
            {
                SaveAppsToOnExitMenu.Checked = false;
                SaveAppsToOnExitMenu.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }
            if (Properties.Settings.Default.SetOnTop) // true = Fenster immer im Vordergrund
            {
                setTopMostMenu.Checked = true;
                setTopMostMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
                this.TopMost = true;
            }
            else
            {
                setTopMostMenu.Checked = false;
                setTopMostMenu.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                this.TopMost = false;
            }
            if (Properties.Settings.Default.DisableWindowOnPosition) // true = Fenster deaktivieren, wenn Position gespeichert ist
            {
                disableWindowOnPositionMenu.Checked = true;
                disableWindowOnPositionMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
            else
            {
                disableWindowOnPositionMenu.Checked = false;
                disableWindowOnPositionMenu.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }
            if (Properties.Settings.Default.DisableredBox) // true = rote Box deaktivieren
            {
                disableRedBoxMenu.Checked = true;
                disableRedBoxMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
            else
            {
                disableRedBoxMenu.Checked = false;
                disableRedBoxMenu.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }
            if (Properties.Settings.Default.SaveOnExit) // true = Einstellungen speichern beim Schließen
            {
                SettingsSaveonexit.Checked = true;
                SettingsSaveonexit.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
            else
            {
                SettingsSaveonexit.Checked = false;
                SettingsSaveonexit.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }
            if (Properties.Settings.Default.TooltipShowHide) // true = Tooltip anzeigen, false = keinen Tooltip anzeigen
            {
                AddTooltipsMenu.Checked = true;
                AddTooltipsMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
                AToolTips.Active = true;
            }
            else
            {
                AddTooltipsMenu.Checked = false;
                AddTooltipsMenu.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                AToolTips.Active = false;
            }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.Hotkey)) // Hotkey für den Autoclicker
            {
                hotkey = (Keys)Enum.Parse(typeof(Keys), Properties.Settings.Default.Hotkey);
                Hotkeypressvalue.SelectedItem = hotkey.ToString();
            }

            if (Properties.Settings.Default.HoldOrSwitch == "hold") // "hold" oder "switch"
            {
                HoldToClick.Checked = true;
            }
            else
            {
                SwitchToClick.Checked = true;
            }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.Clickkey)) // Taste für den Klick
            {
                clickKey = (Keys)Enum.Parse(typeof(Keys), Properties.Settings.Default.Clickkey);
                KeyToPress.SelectedItem = clickKey.ToString();
            }

            if (Properties.Settings.Default.ClicksPerSec > 0) // Klicks pro Sekunde
            {
                ClickPerSecNum.Value = Properties.Settings.Default.ClicksPerSec;
            }

            if (Properties.Settings.Default.ClickMode == "cps") // "cps" oder "time"
            {
                ClicksPersSecButton.Checked = true;
                ClickRepeatgroup.Enabled = false;
                PerTimeNum.Enabled = false;
                PerTimeValue.Enabled = false;
            }
            else
            {
                PerTimeButton.Checked = true;
                ClickRepeatgroup.Enabled = true;
                PerTimeNum.Enabled = true;
                PerTimeValue.Enabled = true;
            }

            if (Properties.Settings.Default.RepeatInfinite) // true = unedlich, false = wiederholen
            {
                RepeatUnlimited.Checked = true;
            }
            else
            {
                RepeatRepeat.Checked = true;
            }

            if (PerTimeValue.Items.Contains(Properties.Settings.Default.ClickTimeUnit)) // Einheit für die Zeit (ms, s, m, h)
                PerTimeValue.SelectedItem = Properties.Settings.Default.ClickTimeUnit;
            if (Properties.Settings.Default.ClickTimeUnit == "ms")
            {
                PerTimeNum.Minimum = 5;
            }
            else
            {
                PerTimeNum.Minimum = 1;
            }
            if (Properties.Settings.Default.ClickTimeValue >= PerTimeNum.Minimum && Properties.Settings.Default.ClickTimeValue <= PerTimeNum.Maximum)
            {
                PerTimeNum.Value = Properties.Settings.Default.ClickTimeValue;
            }
            else
            {
                PerTimeNum.Value = 10;
            }

            RepeatTimes.Value = Properties.Settings.Default.RepeatCount;

            string saved = Properties.Settings.Default.SavedPoints;
            if (!string.IsNullOrWhiteSpace(saved))
            {
                var entries = saved.Split(';');
                foreach (string entry in entries)
                {
                    try
                    {
                        if (entry.StartsWith("M:")) // Mausaktion
                        {
                            var parts = entry.Substring(2).Split(':');
                            if (parts.Length == 2 &&
                                int.TryParse(parts[0], out int x) &&
                                int.TryParse(parts[1], out int y))
                            {
                                SavedActions.Add(new ClickOrKeyAction
                                {
                                    Type = ActionType.MouseClick,
                                    MousePosition = new Point(x, y)
                                });
                            }
                        }
                        else if (entry.StartsWith("K:")) // Tastendruck
                        {
                            string keyStr = entry.Substring(2);
                            if (Enum.TryParse<Keys>(keyStr, out Keys key))
                            {
                                SavedActions.Add(new ClickOrKeyAction
                                {
                                    Type = ActionType.KeyPress,
                                    Key = key
                                });
                            }

                        }
                    }
                    catch
                    {
                        // Bei fehlerhaftem Eintrag einfach überspringen
                        continue;
                    }
                }
            }

            if (Properties.Settings.Default.PositionButton) // true = Position speichern, false = keine Position speichern
            {
                PositionIsChecked.Checked = true;
                if (CurserPositionList.Items.Count > 0)
                {
                    GroupKeyPress.Enabled = false;
                }
            }
            else
            {
                PositionIsChecked.Checked = false;
            }

            if (Properties.Settings.Default.Whitlistchecked) // true = Whitelist, false = Blacklist
            {
                WhitelistappsCheck.Checked = true;
                BlackWhiteListAppsGroup.Text = "Whitelist Apps";
            }
            else
            {
                WhitelistappsCheck.Checked = false;
                BlackWhiteListAppsGroup.Text = "Blacklist Apps";
            }

            var stored = Properties.Settings.Default.BlacklistedApps;
            if (stored != null)
            {
                BlacklistedWindowTitles.AddRange(stored.Cast<string>());
            }
            var stored1 = Properties.Settings.Default.AppsChecked;
            if (stored1 != null)
            {
                AppsCheckedlist.AddRange(stored1.Cast<string>());
            }
            UpdateActionList(); // UI aktualisieren
        }

        private void ResetSettings()
        {
            var backupBlacklist = Properties.Settings.Default.BlacklistedApps;
            var backupChecklist = Properties.Settings.Default.AppsChecked;
            BlacklistedWindowTitles.Clear();

            Properties.Settings.Default.Reset();  // Setzt auf Standardwerte zurück
            Properties.Settings.Default.Save(); // Speichern der Einstellungen

            Properties.Settings.Default.BlacklistedApps = backupBlacklist;
            Properties.Settings.Default.AppsChecked = backupChecklist;
            Properties.Settings.Default.Save();

            SavedActions.Clear();
            AllAppsList.Items.Clear();

            LoadSettings();         // Lade die nun zurückgesetzten Werte
            UpdateActionList();   // UI aktualisieren
            btnRefreshWindows();
            reloadCheckedApps();

        }

        public Form1()
        {
            InitializeComponent();

            hotkeyTimer = new System.Windows.Forms.Timer();
            hotkeyTimer.Interval = 10;
            hotkeyTimer.Tick += HotkeyTimer_Tick;
            hotkeyTimer.Start();

            this.KeyPreview = false;
            //this.KeyDown += Form1_KeyDown;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Hotkeypressvalue.Items.Add(Keys.None.ToString());
            foreach (var name in Enum.GetNames(typeof(Keys)))
            {
                if (DataStings.AllowedMouseList.Contains(name) && (name != "LButton" && name != "RButton") || DataStings.AllowedKeyboardList.Contains(name))
                    Hotkeypressvalue.Items.Add(name);
            }
            AddKeysToPress(true);

            PerTimeValue.Items.Add("ms");
            PerTimeValue.Items.Add("sec");
            PerTimeValue.Items.Add("min");
            PerTimeValue.Items.Add("hour");

            // Lade die Einstellungen
            LoadSettings();
            btnRefreshWindows();
            reloadCheckedApps();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SettingsSaveonexit.Checked)
            {
                if (SaveAppsToOnExitMenu.Checked)
                {
                    var collection = new System.Collections.Specialized.StringCollection();
                    collection.AddRange(BlacklistedWindowTitles.ToArray());
                    Properties.Settings.Default.BlacklistedApps = collection;
                }
                SaveSettings();
            }
        }

        private void DoClick()
        {
            if (UseMouse.Checked)
            {
                if (clickKey == Keys.LButton)
                {
                    new InputSimulator().Mouse
                        .LeftButtonClick();

                    //mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    //mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                }
                else if (clickKey == Keys.RButton)
                {
                    new InputSimulator().Mouse
                        .RightButtonClick();

                    //mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    //mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                }
                else if (clickKey == Keys.MButton)
                {
                    new InputSimulator().Mouse
                        .MiddleButtonClick();

                    //mouse_event(0x20, 0, 0, 0, 0); // MiddleDown
                    //mouse_event(0x40, 0, 0, 0, 0); // MiddleUp
                }
                else if (clickKey == Keys.XButton1)
                {
                    mouse_event(MOUSEEVENTF_XDOWN, 0, 0, XBUTTON1, 0);
                    mouse_event(MOUSEEVENTF_XUP, 0, 0, XBUTTON1, 0);
                }
                else if (clickKey == Keys.XButton2)
                {
                    mouse_event(MOUSEEVENTF_XDOWN, 0, 0, XBUTTON2, 0);
                    mouse_event(MOUSEEVENTF_XUP, 0, 0, XBUTTON2, 0);
                }
            }
            else if (UseKeyboard.Checked)
            {
                string keyName = clickKey.ToString();
                if (DataStings.keyMap.TryGetValue(keyName, out VirtualKeyCode vk))
                {
                    new InputSimulator().Keyboard
                        .KeyPress(vk);
                }
                //SendKeys.SendWait(clickKey.ToString());
            }
        }

        private void StartClicking()
        {
            lock (clickLock)
            {
                if (clicking) return;
                clicking = true;
                clickCts = new CancellationTokenSource();
            }

            if (ShowHideMenu.Checked)
            {
                if (Clickoverlay == null || Clickoverlay.IsDisposed)
                {
                    Clickoverlay = new CursorOverlayForm();
                    Clickoverlay.Show();
                }
            }

            CancellationToken token = clickCts.Token;

            bool switchinfotext = false;

            if (ClicksPersSecButton.Checked)
            {
                decimal cps = ClickPerSecNum.Value;
                double secondsPerClick = 1.0 / (double)cps;
                double intervalMs = 1000.0 / (double)cps;

                Task.Run(() =>
                {
                    Stopwatch sw = new Stopwatch();

                    while (!token.IsCancellationRequested)
                    {
                        sw.Restart();
                        var (proc, _) = GetActiveProcessName();
                        if (!BlacklistedWindowTitles.Contains(proc))
                        {
                            if (switchinfotext)
                                Setinfotextfast("Auto clicker running.....");
                            switchinfotext = false;

                            if (SavedActions.Count != 0 && PositionIsChecked.Checked)
                            {

                                var action = SavedActions[clickIndex];
                                clickIndex = (clickIndex + 1) % SavedActions.Count;

                                if (action.Type == ActionType.MouseClick)
                                {
                                    if (Screen.PrimaryScreen != null)
                                    {
                                        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                                        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                                        double absoluteX = action.MousePosition.X * 65535.0 / (screenWidth - 1);
                                        double absoluteY = action.MousePosition.Y * 65535.0 / (screenHeight - 1);

                                        new InputSimulator().Mouse.MoveMouseTo(absoluteX, absoluteY);
                                    }
                                    new InputSimulator().Mouse
                                        .LeftButtonClick();
                                }
                                else if (action.Type == ActionType.KeyPress && action.Key.HasValue)
                                {
                                    // Tastendruck
                                    string keyName = action.Key.Value.ToString();
                                    if (DataStings.keyMap.TryGetValue(keyName, out VirtualKeyCode vk))
                                    {
                                        new InputSimulator().Keyboard
                                            .KeyPress(vk);
                                    }
                                }
                            }
                            else
                            {
                                //Debug.WriteLine("is Clicking.... " + clickIndex + pos.X + " " + pos.Y);
                                DoClick();
                            }
                        }
                        else
                        {
                            if (!switchinfotext)
                                Setinfotextfast("Auto clicker ON: Waiting for None Blacklisted window.......");
                            switchinfotext = true;
                        }

                        double remaining = intervalMs - sw.Elapsed.TotalMilliseconds;

                        if (remaining > 2)
                        {
                            Thread.Sleep((int)(remaining - 1)); // Grobschlaf
                        }

                        // Feintuning mit SpinWait (nur sehr kurz)
                        while (sw.Elapsed.TotalMilliseconds < intervalMs)
                        {
                            if (!clicking || token.IsCancellationRequested)
                                break;

                            Thread.SpinWait(5); // Weniger Spins reicht für 100 CPS
                        }
                    }
                }, token);
            }
            else if (PerTimeButton.Checked)
            {
                double timeValue = (double)PerTimeNum.Value; // Zeit zwischen Klicks
                double repeatCount = (double)RepeatTimes.Value; // wie oft klicken

                // Einheit umrechnen
                string unit = PerTimeValue.SelectedItem?.ToString() ?? "s";
                double intervalMs;

                switch (unit)
                {
                    case "ms":
                        intervalMs = timeValue;
                        break;
                    case "s":
                        intervalMs = timeValue * 1000.0;
                        break;
                    case "m":
                        intervalMs = timeValue * 60_000.0;
                        break;
                    case "h":
                        intervalMs = timeValue * 3_600_000.0;
                        break;
                    default:
                        intervalMs = timeValue * 1000.0; // Fallback
                        break;
                }

                // Klick-Intervall berechnen
                bool infinite = RepeatUnlimited.Checked;
                int clickCount = 0;
                Task.Run(() =>
                {
                    Stopwatch sw = new Stopwatch();

                    while ((infinite || clickCount < repeatCount) && !token.IsCancellationRequested)
                    {
                        sw.Restart();
                        var (proc, _) = GetActiveProcessName();
                        if (!BlacklistedWindowTitles.Contains(proc))
                        {
                            if (switchinfotext)
                                Setinfotextfast("Auto clicker running.....");
                            switchinfotext = false;
                            if (SavedActions.Count != 0 && PositionIsChecked.Checked)
                            {
                                foreach (var action in SavedActions)
                                {
                                    if (Screen.PrimaryScreen != null)
                                    {
                                        if (action.Type == ActionType.MouseClick)
                                        {
                                            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                                            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                                            double absoluteX = action.MousePosition.X * 65535.0 / (screenWidth - 1);
                                            double absoluteY = action.MousePosition.Y * 65535.0 / (screenHeight - 1);

                                            new InputSimulator().Mouse.MoveMouseTo(absoluteX, absoluteY);

                                            new InputSimulator().Mouse
                                                .LeftButtonClick();
                                        }
                                        else if (action.Type == ActionType.KeyPress && action.Key.HasValue)
                                        {
                                            string keyName = action.Key.Value.ToString();
                                            if (DataStings.keyMap.TryGetValue(keyName, out VirtualKeyCode vk))
                                            {
                                                new InputSimulator().Keyboard
                                                    .KeyPress(vk);
                                            }
                                        }
                                    }
                                    Thread.Sleep(10);
                                }
                            }
                            else
                            {
                                //Debug.WriteLine("is Clicking.... ");
                                DoClick();
                            }
                            //Debug.WriteLine("Pause");

                            clickCount++;
                        }
                        else
                        {
                            if (!switchinfotext)
                                Setinfotextfast("Auto clicker ON: Waiting for None Blacklisted window.......");
                            switchinfotext = true;
                        }

                        double remaining = intervalMs - sw.Elapsed.TotalMilliseconds;

                        if (remaining > 2)
                        {
                            Thread.Sleep((int)(remaining - 1)); // Grobschlaf
                        }

                        // Feintuning mit SpinWait (nur sehr kurz)
                        while (sw.Elapsed.TotalMilliseconds < intervalMs)
                        {
                            if (!clicking || token.IsCancellationRequested)
                                break;

                            Thread.SpinWait(5); // Weniger Spins reicht für 100 CPS
                        }
                    }
                    if (RepeatRepeat.Checked)
                    {
                        Setinfotextfast("Auto clicker Stopped.....");
                        StopClicking();
                    }
                }, token);
            }
        }

        private void StopClicking()
        {
            lock (clickLock)
            {
                if (!clicking) return;
                clicking = false;
                clickCts?.Cancel();
            }
            if (Clickoverlay != null && !Clickoverlay.IsDisposed)
            {
                Clickoverlay.Close();
                Clickoverlay = null;
            }


            Invoke(() => InfoLabel.Text = "Auto clicker Stopped.....");
        }

        public static (DialogResult result, bool anotherClicked) ShowCustomMessage(string message)
        {
            bool anotherClicked = false;

            Form form = new Form
            {
                Text = "Info",
                Size = new Size(300, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false,
                TopMost = true
            };

            Label lbl = new Label
            {
                Text = message,
                AutoSize = false,
                Size = new Size(260, 40),
                Location = new Point(20, 10),
                TextAlign = ContentAlignment.MiddleCenter
            };
            form.Controls.Add(lbl);

            Button okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(50, 70), Size = new Size(75, 30) };
            Button anotherButton = new Button { Text = "Set Another", Location = new Point(150, 70), Size = new Size(85, 30) };

            anotherButton.Click += (s, e) =>
            {
                anotherClicked = true;
                form.DialogResult = DialogResult.Retry;
                form.Close();
            };

            form.Controls.Add(okButton);
            form.Controls.Add(anotherButton);

            form.AcceptButton = okButton;

            var result = form.ShowDialog();
            return (result, anotherClicked);
        }

        private void ImageAndTextSwitch(ToolStripMenuItem Item)
        {
            if (Item.Checked)
            {
                Item.Checked = false;
                Item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }
            else
            {
                Item.Checked = true;
                Item.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
        }

        private void UpdateActionList()
        {
            CurserPositionList.Items.Clear();
            for (int i = 0; i < SavedActions.Count; i++)
            {
                CurserPositionList.Items.Add($"{i + 1}. {SavedActions[i]}");
            }
        }

        private void RemoveSelectedPosition()
        {
            int selectedIndex = CurserPositionList.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < SavedActions.Count)
            {
                SavedActions.RemoveAt(selectedIndex);
                UpdateActionList();
            }
        }

        private Form ShowPositionMarker(Point pos, int setTime = 2)
        {
            int size = 10;
            var marker = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.Magenta,
                TransparencyKey = Color.Magenta,
                TopMost = true,
                ShowInTaskbar = false,
                Size = new Size(size, size),
                Location = new Point(pos.X - size / 2, pos.Y - size / 2)
            };

            // Größe des Punkts (z. B. 6×6 Pixel)
            int Posindex = SavedActions.FindIndex(a => a.MousePosition.X == pos.X && a.MousePosition.Y == pos.Y) + 1;

            marker.Paint += (s, e) =>
            {
                // Punkt zeichnen
                using (Brush pointBrush = new SolidBrush(Color.Red))
                    e.Graphics.FillEllipse(pointBrush, 0, 0, size - 1, size - 1);

                // Text vorbereiten
                string text = $"{Posindex}. X={pos.X} Y={pos.Y}";
                using (Font font = new Font("Segoe UI", 10))
                {
                    SizeF textSize = e.Graphics.MeasureString(text, font);
                    RectangleF textRect = new RectangleF(size + 2, 0, textSize.Width + 4, textSize.Height);

                    // Hintergrund (halbtransparent schwarz)
                    using (Brush bgBrush = new SolidBrush(Color.FromArgb(60, 60, 60))) // dunkelgrau
                        e.Graphics.FillRectangle(bgBrush, textRect);

                    // Text in Weiß
                    using (Brush textBrush = new SolidBrush(Color.White))
                        e.Graphics.DrawString(text, font, textBrush, size + 4, 0);
                }
            };

            marker.Show();

            // Kreis nach kurzer Zeit wieder entfernen
            if (setTime > 0)
            {
                var t = new System.Windows.Forms.Timer();
                t.Interval = setTime * 1000; // Zeit in Millisekunden
                t.Tick += (s, e) =>
                {
                    t.Stop();
                    marker.Close();

                };
                t.Start();
            }
            return marker;
        }

        private (string processName, string windowTitle) GetActiveProcessName()
        {
            IntPtr hWnd = GetForegroundWindow();
            GetWindowThreadProcessId(hWnd, out uint processId);

            try
            {
                Process proc = Process.GetProcessById((int)processId);
                string processName = proc.ProcessName + ".exe";

                int length = GetWindowTextLength(hWnd);
                StringBuilder builder = new StringBuilder(length + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                string windowTitle = builder.ToString();

                return (processName, windowTitle);
            }
            catch
            {
                return (string.Empty, string.Empty);
            }
        }

        public void btnRefreshWindows()
        {
            AllFoundAppsList.Clear();
            AllAppsList.Items.Clear();
            IntPtr shellWindow = GetShellWindow();

            EnumWindows((hWnd, lParam) =>
            {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;
                if (GetParent(hWnd) != IntPtr.Zero) return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                int exStyle = (int)GetWindowLong(hWnd, GWL_EXSTYLE);
                if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true;

                StringBuilder builder = new StringBuilder(length + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                string windowTitle = builder.ToString();

                // Prozess-ID herausfinden
                GetWindowThreadProcessId(hWnd, out uint processId);
                string processName = "unknown";

                try
                {
                    Process proc = Process.GetProcessById((int)processId);
                    processName = proc.ProcessName + ".exe";
                }
                catch { }

                if (!DataStings.blacklistapps.Contains(processName))
                {
                    if (!AllFoundAppsList.Contains(processName))
                    {
                        int index = AllAppsList.Items.Add($"{processName} - {windowTitle}");
                        if (AppsCheckedlist.Contains(processName))
                        {
                            AllAppsList.SetItemChecked(index, true);
                        }
                        AllFoundAppsList.Add(processName);
                    }
                }

                return true;
            }, IntPtr.Zero);

            foreach (var blackitem in AppsCheckedlist)
            {
                bool isfound = false;

                if (AllFoundAppsList.Contains(blackitem))
                {
                    isfound = true;
                }

                if (!isfound)
                {
                    int index = AllAppsList.Items.Add($"{blackitem} - Not Found but saved!");
                    AllAppsList.SetItemChecked(index, true);
                }
            }
        }

        private void Setinfotextfast(string Text = "")
        {
            Invoke(new Action(() =>
            {
                InfoLabel.Text = Text;
            }));
        }

        private void OpenOverlaySettings()
        {
            if (settingsForm != null && !settingsForm.IsDisposed)
            {
                settingsForm.BringToFront();
                return;
            }
            hotkeyTimer.Stop();
            this.Enabled = false;

            // Overlay starten, falls nicht aktiv
            if (cursorOverlay == null || cursorOverlay.IsDisposed)
            {
                cursorOverlay = new CursorOverlayForm();
                cursorOverlay.Show();
            }

            // Settings-Fenster erstellen
            settingsForm = new OverlaySettingsForm(cursorOverlay.CurrentColor, cursorOverlay.CurrentSize);

            // Änderungen übernehmen
            settingsForm.ColorChanged += color =>
            {
                cursorOverlay?.SetOverlayColor(color);
                Properties.Settings.Default.ClickCircleColor = color;
            };

            settingsForm.SizeChanged += size =>
            {
                cursorOverlay?.SetOverlaySize(size);
                Properties.Settings.Default.ClickCircleSize = size;
            };

            settingsForm.TransparencyChanged += transparency =>
            {
                cursorOverlay?.SetOverlayTransparency(transparency);
                Properties.Settings.Default.ClickCircleTransparent = transparency;
            };

            settingsForm.FormClosed += (s, e) =>
            {
                Properties.Settings.Default.Save();

                if (cursorOverlay != null && !cursorOverlay.IsDisposed)
                {
                    cursorOverlay.Close();
                    cursorOverlay = null;
                }
                hotkeyTimer.Start();
                this.Enabled = true;

                settingsForm = null;
            };

            settingsForm.Show();
        }

        private void AddKeysToPress(bool switchtomouse)
        {
            if (switchtomouse)
            {
                KeyToPress.Items.Clear();
                KeyToPress.Items.Add("LButton");
                KeyToPress.Items.Add("RButton");
                KeyToPress.Items.Add("MButton");
                KeyToPress.Items.Add("XButton1");
                KeyToPress.Items.Add("XButton2");
                KeyToPress.SelectedIndex = 0;
                clickKey = Keys.LButton;
            }
            else
            {
                KeyToPress.Items.Clear();
                foreach (var name in Enum.GetNames(typeof(Keys)))
                {
                    // Nur normale Tasten, keine Maus oder Modifier wie Shift
                    if (DataStings.AllowedKeyboardList.Contains(name))
                        KeyToPress.Items.Add(name);
                }
                KeyToPress.SelectedItem = Keys.A.ToString();
                clickKey = Keys.A;
            }
        }

        private Keys RecordKeysSend(bool Mousefind, bool keyboardfind, bool isHotkey = false, bool isKeypress = false)
        {
            bool breakloop = false;
            Keys detectedKey = Keys.None;
            bool ShowError = false;
            string ErrortoShow = "";

            hotkeyTimer.Stop();

            using (DarkBackgroundOverlay bg = new DarkBackgroundOverlay())
            using (KeyCaptureOverlay overlay = new KeyCaptureOverlay())
            {
                bg.Show();
                overlay.Show();

                while (detectedKey == Keys.None && !breakloop)
                {
                    int TimerTogoback = 3;
                    if (disableRedBoxMenu.Checked)
                    {
                        TimerTogoback = 1;
                    }
                    Application.DoEvents(); // Damit das Overlay reagiert

                    foreach (Keys key in Enum.GetValues(typeof(Keys)))
                    {
                        short state = GetAsyncKeyState(key);
                        bool Keypressed = (state & 0x8000) != 0;
                        if (Keypressed && key.ToString() == "Escape")
                        {
                            breakloop = true; // Escape-Taste gedrückt, Schleife beenden
                            break;
                        }
                        else if (Keypressed && key == hotkey && !isHotkey)
                        {
                            ShowError = true;
                            ErrortoShow = $"(Same as Hotkey): {key}";
                        }
                        else if (Mousefind && Keypressed && DataStings.AllowedMouseList.Contains(key.ToString())) // Taste ist gedrückt
                        {
                            if (isHotkey && (key.ToString() == "LButton" || key.ToString() == "RButton"))
                            {
                                ShowError = true;
                                ErrortoShow = $"(Hotkey disabled keys: LButton, RButton): {key}";
                            }
                            else if (isKeypress && !UseMouse.Checked)
                            {
                                UseMouse.Checked = true;
                                AddKeysToPress(true);
                                detectedKey = key;
                                //ShowError = true;
                                //ErrortoShow = $"(You have to Check Mouse): {key}";
                            }
                            else
                            {
                                detectedKey = key;
                                break;
                            }
                        }
                        else if (keyboardfind && Keypressed && DataStings.AllowedKeyboardList.Contains(key.ToString())) // Taste ist gedrückt
                        {
                            if (keyboardfind && isKeypress && !UseKeyboard.Checked)
                            {
                                UseKeyboard.Checked = true;
                                AddKeysToPress(false);
                                detectedKey = key;
                                //ShowError = true;
                                //ErrortoShow = $"(You have to Check Keyboard): {key}";
                            }
                            else
                            {
                                detectedKey = key;
                                break;
                            }
                        }
                        else if (Keypressed)
                        {
                            ShowError = true;
                            ErrortoShow = $"(Not exist in list): {key}";
                        }
                    }
                    overlay.Focus();
                    if (ShowError)
                    {
                        overlay.BackColor = Color.Red;
                        //overlay.SetMessage($"Not a Valid Key: {detectedKey} Try again after it get back Orange\nBack in: 3");
                        overlay.SetMessage($"Not a Valid Key\n{ErrortoShow}\nTry again after it get back Orange\nBack in: {TimerTogoback}");
                        overlay.Refresh();
                        while (TimerTogoback > 0)
                        {
                            //overlay.SetMessage($"Not a Valid Key: {detectedKey} Try again after it get back Orange\nBack in: {TimerTogoback}");
                            overlay.SetMessage($"Not a Valid Key\n{ErrortoShow}\nTry again after it get back Orange\nBack in: {TimerTogoback}");
                            overlay.Refresh();
                            TimerTogoback--;
                            Thread.Sleep(1000);
                        }
                        overlay.BackColor = Color.Orange;
                        overlay.SetMessage("Press a Valid key.\nPRESS:  ESC  to cancle this process");
                        overlay.Refresh();
                        ShowError = false;
                    }
                    Thread.Sleep(40); // Kurze Pause, um CPU-Last zu reduzieren
                }
                overlay.Focus();
                if (detectedKey != Keys.None)
                {
                    overlay.BackColor = Color.Green;
                    overlay.SetMessage($"Key detected: {detectedKey}");
                    overlay.Refresh();
                    Thread.Sleep(500); // Kurze Pause, um die Anzeige zu sehen
                }
                else if (breakloop)
                {
                    overlay.BackColor = Color.Red;
                    overlay.SetMessage($"Press Canceled");
                    overlay.Refresh();
                    Thread.Sleep(1000); // Kurze Pause, um die Anzeige zu sehen
                }
                overlay.Close();
                bg.Close();
            }
            hotkeyTimer.Start();
            return detectedKey;
        }

        private void HotkeyTimer_Tick(object? sender, EventArgs e)
        {
            bool keyDown = (GetAsyncKeyState(hotkey) & 0x8000) != 0;

            if (HoldToClick.Checked)
            {
                if (keyDown && !clicking)
                {
                    InfoLabel.Text = "Auto clicker running.....";
                    StartClicking();
                }
                else if (!keyDown && clicking)
                {
                    InfoLabel.Text = "Auto clicker Stopped.....";
                    StopClicking();
                }
            }
            else if (SwitchToClick.Checked)
            {
                if (keyDown && !keyWasDown)
                {
                    if (!clicking)
                    {
                        InfoLabel.Text = "Auto clicker running.....";
                        StartClicking();
                    }
                    else
                    {
                        InfoLabel.Text = "Auto clicker Stopped.....";
                        StopClicking();
                    }
                }
            }
            keyWasDown = keyDown;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (UseMouse.Checked)
            {
                AddKeysToPress(true);
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (UseKeyboard.Checked)
            {
                AddKeysToPress(false);
            }
        }

        private void HotkeyFindKey_Click(object sender, EventArgs e)
        {
            Keys PressedKey = RecordKeysSend(true, true, true);
            if (PressedKey == Keys.None)
            {
                InfoLabel.Text = "No Key found or it got canceled";
            }
            else
            {
                hotkey = PressedKey;
                Hotkeypressvalue.SelectedItem = hotkey.ToString();
                InfoLabel.Text = $"Hotkey set: {hotkey}";
            }
        }

        private void ClickKeyFind_Click(object sender, EventArgs e)
        {
            Keys PressedKey = RecordKeysSend(true, true, false, true);
            if (PressedKey == Keys.None)
            {
                InfoLabel.Text = "No Key found or it got canceled";
            }
            else
            {
                clickKey = PressedKey;
                KeyToPress.SelectedItem = clickKey.ToString();
                InfoLabel.Text = $"Click key set: {clickKey}";
            }
        }

        private void Hotkeypressvalue_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Hotkeypressvalue.SelectedItem != null && Enum.TryParse(Hotkeypressvalue.SelectedItem.ToString(), out Keys selected))
            {
                hotkey = selected;
            }
        }

        private void KeyToPress_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (KeyToPress.SelectedItem != null && Enum.TryParse(KeyToPress.SelectedItem.ToString(), out Keys selected))
            {
                clickKey = selected;
            }
        }

        private void radioButton2_CheckedChanged_1(object sender, EventArgs e)
        {
            RepeatTimes.Enabled = true;
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            ClickPerSecNum.Enabled = true;
            HoldToClick.Enabled = true;
            ClickRepeatgroup.Enabled = false;
            PerTimeNum.Enabled = false;
            PerTimeValue.Enabled = false;
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            PerTimeNum.Enabled = true;
            PerTimeValue.Enabled = true;
            ClickRepeatgroup.Enabled = true;
            HoldToClick.Enabled = false;
            SwitchToClick.Checked = true;
            ClickPerSecNum.Enabled = false;
        }

        private void RepeatUnlimited_CheckedChanged(object sender, EventArgs e)
        {
            RepeatTimes.Enabled = false;
        }

        private void ResetAllSettings_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Do you really want to reset all settings?", "Reset Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                ResetSettings();
                InfoLabel.Text = "Settings Reset.";
            }
        }


        private void SaveMouseClick(Point pos)
        {
            if (pos == Point.Empty)
                pos = Cursor.Position;

            SavedActions.Add(new ClickOrKeyAction
            {
                Type = ActionType.MouseClick,
                MousePosition = pos
            });

            UpdateActionList();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;

            Keys detectedKey = Keys.None;

            Form marker = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.Magenta,
                TransparencyKey = Color.Magenta,
                TopMost = true,
                ShowInTaskbar = false,
                Size = new Size(400, 200) // Platz für Text
            };

            Label coordLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60) // halbtransparent schwarzer Hintergrund
            };

            marker.Controls.Add(coordLabel);
            marker.Show();

            while (detectedKey == Keys.None)
            {
                if ((GetAsyncKeyState(Keys.LButton) & 0x8000) != 0)
                {
                    detectedKey = Keys.LButton;
                    break;
                }
                Point cursorPos = Cursor.Position;

                // Marker neu positionieren (neben dem Cursor z. B. +20px)
                marker.Location = new Point(cursorPos.X + 10, cursorPos.Y + 10);
                coordLabel.Text = $"X={cursorPos.X}, Y={cursorPos.Y}";

                Application.DoEvents();
                Thread.Sleep(30); // Kurze Pause, um CPU-Last zu reduzieren
            }
            marker.Close();

            if (detectedKey != Keys.None)
            {
                Point selectedPos = Cursor.Position;
                SaveMouseClick(selectedPos);
                this.WindowState = FormWindowState.Normal;
                this.Activate(); // In den Vordergrund holen
                if (disableWindowOnPositionMenu.Checked)
                {
                    InfoLabel.Text = $"Position Saved: X={selectedPos.X}, Y={selectedPos.Y} (Window Disabled)";
                }
                else
                {
                    //MessageBox.Show($"Position Saved: X={selectedPos.X}, Y={selectedPos.Y}");
                    var (dialogResult, another) = ShowCustomMessage($"Position Saved: X={selectedPos.X}, Y={selectedPos.Y}");
                    InfoLabel.Text = $"Position Saved: X={selectedPos.X}, Y={selectedPos.Y}";

                    if (another)
                    {
                        PositionSave.PerformClick();
                    }
                }
            }

            if (CurserPositionList.Items.Count > 0)
            {
                if (PositionIsChecked.Checked && GroupKeyPress.Enabled)
                {
                    GroupKeyPress.Enabled = false;
                }
            }
            else
            {
                GroupKeyPress.Enabled = true;
            }

        }


        private void ClearSavedPositions()
        {
            SavedActions.Clear();
            CurserPositionList.Items.Clear();
            InfoLabel.Text = ("All saved Actions deleted.");
        }
        private void PositionClear_Click(object sender, EventArgs e)
        {
            ClearSavedPositions();

            if (CurserPositionList.Items.Count > 0)
            {
                if (PositionIsChecked.Checked && GroupKeyPress.Enabled)
                {
                    GroupKeyPress.Enabled = false;
                }
            }
            else
            {
                GroupKeyPress.Enabled = true;
            }
        }


        private void PositionRemove_Click(object sender, EventArgs e)
        {
            RemoveSelectedPosition();

            if (CurserPositionList.Items.Count > 0)
            {
                if (PositionIsChecked.Checked && GroupKeyPress.Enabled)
                {
                    GroupKeyPress.Enabled = false;
                }
            }
            else
            {
                GroupKeyPress.Enabled = true;
            }
        }

        private void CurserPositionList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ShowPointOnClick.Checked && CurserPositionList.SelectedItem != null && !CurserPositionList.SelectedItem.ToString().Contains("Key") && !ShowAllPositionsCheck.Checked)
            {
                int indexpos = CurserPositionList.SelectedIndex;
                Point pos = SavedActions[indexpos].MousePosition;
                //Debug.WriteLine("Selected: " + pos.X + " " + pos.Y);
                ShowPositionMarker(pos);
            }
            if (CurserPositionList.Items.Count > 0)
            {
                if (PositionIsChecked.Checked && GroupKeyPress.Enabled)
                {
                    GroupKeyPress.Enabled = false;
                }
            }
            else
            {
                GroupKeyPress.Enabled = true;
            }
        }

        private void setTopMostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (setTopMostMenu.Checked)
            {
                this.TopMost = false;
                setTopMostMenu.Checked = false;
                setTopMostMenu.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }
            else
            {
                this.TopMost = true;
                setTopMostMenu.Checked = true;
                setTopMostMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
        }

        private void SettingsSaveonexit_Click_1(object sender, EventArgs e)
        {
            ImageAndTextSwitch(SettingsSaveonexit);
        }

        private void saveToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            SaveSettings();
            InfoLabel.Text = "Settings saved.";
        }

        private void disableWindowOnPositionMenu_Click(object sender, EventArgs e)
        {
            ImageAndTextSwitch(disableWindowOnPositionMenu);
        }


        private void SaveKeyPress(Keys key)
        {
            SavedActions.Add(new ClickOrKeyAction
            {
                Type = ActionType.KeyPress,
                Key = key
            });

            UpdateActionList();
        }
        private void KeySaveInList_Click(object sender, EventArgs e)
        {
            Keys PressedKey = RecordKeysSend(false, true);
            if (PressedKey == Keys.None)
            {
                InfoLabel.Text = "No Key found or it got canceled";
            }
            else
            {
                SaveKeyPress(PressedKey);
            }

            if (CurserPositionList.Items.Count > 0)
            {
                if (PositionIsChecked.Checked && GroupKeyPress.Enabled)
                {
                    GroupKeyPress.Enabled = false;
                }
            }
            else
            {
                GroupKeyPress.Enabled = true;
            }
        }


        private void disableRedBoxMenu_Click(object sender, EventArgs e)
        {
            ImageAndTextSwitch(disableRedBoxMenu);
        }

        private void PositionIsChecked_CheckedChanged(object sender, EventArgs e)
        {
            if (PositionIsChecked.Checked)
            {
                LabelUsingActions.Visible = true;
                if (SavedActions.Count > 0)
                    GroupKeyPress.Enabled = false;
            }
            else
            {
                LabelUsingActions.Visible = false;
                GroupKeyPress.Enabled = true;
            }
        }

        private void ShowAllPositionsCheck_CheckedChanged(object sender, EventArgs e)
        {
            // Erst alte Marker entfernen
            foreach (var marker in activeMarkers)
                marker.Close();
            activeMarkers.Clear();

            if (ShowAllPositionsCheck.Checked)
            {
                foreach (var action in SavedActions)
                {
                    if (action.Type == ActionType.MouseClick)
                    {
                        var marker = ShowPositionMarker(action.MousePosition, 0);
                        activeMarkers.Add(marker);
                    }
                }
            }
        }


        private void reloadCheckedApps()
        {
            BlacklistedWindowTitles.Clear();
            AppsCheckedlist.Clear();

            for (int i = 0; i < AllAppsList.Items.Count; i++)
            {
                object item = AllAppsList.Items[i];
                if (item == null)
                    continue;
                string selectedItem = item.ToString();
                string[] parts = selectedItem.Split(new[] { " - " }, StringSplitOptions.None);
                if (parts.Length >= 2)
                {
                    string processName = parts[0];
                    string windowTitle = parts[1];

                    if (WhitelistappsCheck.Checked)
                    {
                        if (!BlacklistedWindowTitles.Contains(processName) && !AllAppsList.GetItemChecked(i))
                        {
                            BlacklistedWindowTitles.Add(processName);
                        }
                        else if (!AppsCheckedlist.Contains(processName) && AllAppsList.GetItemChecked(i))
                        {
                            AppsCheckedlist.Add(processName);
                        }
                    }
                    else
                    {
                        if (!BlacklistedWindowTitles.Contains(processName) && AllAppsList.GetItemChecked(i))
                        {
                            BlacklistedWindowTitles.Add(processName);
                        }
                        if (!AppsCheckedlist.Contains(processName) && AllAppsList.GetItemChecked(i))
                        {
                            AppsCheckedlist.Add(processName);
                        }
                    }
                    //Debug.WriteLine(processName);
                }
            }
        }

        private void AllAppsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            reloadCheckedApps();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            btnRefreshWindows();
        }

        private void SaveBlacklistList_Click(object sender, EventArgs e)
        {
            var Blackcollection = new System.Collections.Specialized.StringCollection();
            Blackcollection.AddRange(BlacklistedWindowTitles.ToArray());
            Properties.Settings.Default.BlacklistedApps = Blackcollection;

            var Whitecollection = new System.Collections.Specialized.StringCollection();
            Whitecollection.AddRange(AppsCheckedlist.ToArray());
            Properties.Settings.Default.AppsChecked = Whitecollection;

            Properties.Settings.Default.Save();
            InfoLabel.Text = "App List saved.";
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            BlacklistedWindowTitles.Clear();
            AppsCheckedlist.Clear();

            var collection = new System.Collections.Specialized.StringCollection();
            collection.AddRange(BlacklistedWindowTitles.ToArray());
            Properties.Settings.Default.BlacklistedApps = collection;

            Properties.Settings.Default.Save();
            btnRefreshWindows();
            reloadCheckedApps();

            InfoLabel.Text = "App List Has Been Reset";
        }

        private void WhitelistappsCheck_CheckedChanged(object sender, EventArgs e)
        {
            btnRefreshWindows();
            reloadCheckedApps();
            if (WhitelistappsCheck.Checked)
            {
                InfoLabel.Text = "Whitelist mode enabled";
                BlackWhiteListAppsGroup.Text = "Whitelist Apps";
            }
            else
            {
                InfoLabel.Text = "Blacklist mode enabled";
                BlackWhiteListAppsGroup.Text = "Blacklist Apps";
            }
        }

        private void PerTimeValue_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PerTimeValue.SelectedItem.ToString() == "ms")
            {
                PerTimeNum.Minimum = 5;
            }
            else
            {
                PerTimeNum.Minimum = 1;
            }
        }

        private void custemizeCircleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenOverlaySettings();
        }

        private void showHideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImageAndTextSwitch(ShowHideMenu);
        }

        private void SaveAppsToOnExitMenu_Click(object sender, EventArgs e)
        {
            ImageAndTextSwitch(SaveAppsToOnExitMenu);
        }

        private void addTooltipsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImageAndTextSwitch(AddTooltipsMenu);
            if (AddTooltipsMenu.Checked)
            {
                AToolTips.Active = true;
            }
            else
            {
                AToolTips.Active = false;
            }
        }


    }

    public class KeyCaptureOverlay : Form
    {
        private Label messageLabel;

        public KeyCaptureOverlay()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Orange;
            this.Opacity = 0.95;
            this.StartPosition = FormStartPosition.Manual;
            this.KeyPreview = true;
            this.ShowInTaskbar = false;
            this.TopMost = true;

            // Bildschirmgröße holen
            Rectangle screen = Screen.PrimaryScreen?.WorkingArea
                               ?? new Rectangle(0, 0, 800, 600);


            // Fenster Größe auf 50% des Bildschirms setzen
            int width = screen.Width / 2;
            int height = screen.Height / 2;

            // Fenster zentrieren
            this.Size = new Size(width, height);
            this.Location = new Point(
                screen.Left + (screen.Width - width) / 2,
                screen.Top + (screen.Height - height) / 2
            );

            // Label hinzufügen
            messageLabel = new Label();
            messageLabel.Text = "Press a Valid key.\nPRESS: ESC   to cancle this process";
            messageLabel.Font = new Font("Segoe UI", 40, FontStyle.Regular);
            messageLabel.TextAlign = ContentAlignment.MiddleCenter;
            messageLabel.Dock = DockStyle.Fill;
            messageLabel.ForeColor = Color.White;
            this.Controls.Add(messageLabel);
        }

        public void SetMessage(string text, Color? backColor = null)
        {
            messageLabel.Text = text;
            if (backColor.HasValue)
                this.BackColor = backColor.Value;

            this.Refresh(); // Sofort neu zeichnen
        }
    }

    public class DarkBackgroundOverlay : Form
    {
        public DarkBackgroundOverlay()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.Opacity = 0.5;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        }
    }

    public enum ActionType
    {
        MouseClick,
        KeyPress
    }

    public class ClickOrKeyAction
    {
        public ActionType Type { get; set; }
        public Point MousePosition { get; set; }
        public Keys? Key { get; set; }

        public override string ToString()
        {
            return Type == ActionType.MouseClick
                ? $"Mouse at X:{MousePosition.X}, Y:{MousePosition.Y}"
                : $"Key: {Key}";
        }
    }

    public class CursorOverlayForm : Form
    {
        private System.Windows.Forms.Timer followTimer;
        private int circleSize = Properties.Settings.Default.ClickCircleSize;

        public Color CurrentColor { get; private set; }
        public int CurrentSize { get; private set; }
        public int CurrentTransparentcy { get; private set; }

        public CursorOverlayForm()
        {
            CurrentColor = Properties.Settings.Default.ClickCircleColor;
            CurrentTransparentcy = Properties.Settings.Default.ClickCircleTransparent;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;

            var _ = Handle;

            Size = new Size(circleSize, circleSize); // garantiert quadratisch

            // Erlaube durchklicken & layered drawing
            int initialStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            SetWindowLong(Handle, GWL_EXSTYLE, initialStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);

            var cursor = Cursor.Position;
            var pos = new Point(cursor.X - Width / 2, cursor.Y - Height / 2);


            // Timer zum Cursor folgen
            followTimer = new System.Windows.Forms.Timer();
            followTimer.Interval = 30; // flüssige Bewegung
            followTimer.Tick += (s, e) => FollowCursor();
            followTimer.Start();

            // Initial anzeigen
            FollowCursor();
        }

        private void FollowCursor()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            var cursor = Cursor.Position;
            var pos = new POINT(cursor.X - Width / 2, cursor.Y - Height / 2);
            ShowCircle(pos);
        }

        public void SetOverlayColor(Color color)
        {
            CurrentColor = color;
            FollowCursor();
        }

        public void SetOverlayTransparency(int Transparentkey)
        {
            CurrentTransparentcy = Transparentkey;
            FollowCursor();
        }

        public void SetOverlaySize(int size)
        {
            CurrentSize = size;
            Size = new Size(size, size);
            FollowCursor();
        }

        private void ShowCircle(POINT screenPos)
        {
            int size = Math.Min(Width, Height); // sichere Größe für echten Kreis

            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Brush brush = new SolidBrush(Color.FromArgb(CurrentTransparentcy, CurrentColor)))
                {
                    g.FillEllipse(brush, 0, 0, size, size);
                }
            }

            IntPtr screenDC = GetDC(IntPtr.Zero);
            IntPtr memDC = CreateCompatibleDC(screenDC);
            IntPtr hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
            IntPtr oldBitmap = SelectObject(memDC, hBitmap);

            SIZE winSize = new SIZE(size, size);
            POINT pointSource = new POINT(0, 0);

            BLENDFUNCTION blend = new BLENDFUNCTION
            {
                BlendOp = 0,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = 1
            };

            if (IsDisposed || !IsHandleCreated)
                return;

            UpdateLayeredWindow(Handle, screenDC, ref screenPos, ref winSize, memDC, ref pointSource, 0, ref blend, 2);

            // Cleanup
            SelectObject(memDC, oldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(memDC);
            ReleaseDC(IntPtr.Zero, screenDC);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            followTimer?.Stop();
            followTimer?.Dispose();
            base.OnFormClosed(e);
        }

        // WinAPI
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool UpdateLayeredWindow(
            IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
            IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr ho);

        private struct POINT { public int X; public int Y; public POINT(int x, int y) { X = x; Y = y; } }
        private struct SIZE { public int cx; public int cy; public SIZE(int w, int h) { cx = w; cy = h; } }

        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }
    }

    public class OverlaySettingsForm : Form
    {
        public event Action<Color>? ColorChanged;
        public event Action<int>? SizeChanged;
        public event Action<int>? TransparencyChanged;

        private TrackBar sizeBar;
        private Label sizeLabel;
        private TrackBar TransparentcyBar;
        private Label TransparentcyLabel;
        private Button colorButton;
        private Panel previewPanel;

        private Color currentColor = Color.LimeGreen;

        public OverlaySettingsForm(Color initialColor, int initialSize)
        {
            Text = "Click Circle Settings";
            Width = 300;
            Height = 250;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            initialSize = Math.Clamp(initialSize, 10, 100);

            sizeBar = new TrackBar
            {
                Minimum = 10,
                Maximum = 100,
                Value = Properties.Settings.Default.ClickCircleSize,
                TickFrequency = 10,
                Dock = DockStyle.Top
            };

            sizeLabel = new Label
            {
                Text = $"Size: {sizeBar.Value}",
                Dock = DockStyle.Top
            };

            sizeBar.ValueChanged += (s, e) =>
            {
                SizeChanged?.Invoke(sizeBar.Value);
                sizeLabel.Text = $"Size: {sizeBar.Value}";
            };

            TransparentcyBar = new TrackBar
            {
                Minimum = 20,
                Maximum = 255,
                Value = Properties.Settings.Default.ClickCircleTransparent,
                TickFrequency = 10,
                Dock = DockStyle.Top
            };

            TransparentcyLabel = new Label
            {
                Text = $"Transparency: {TransparentcyBar.Value}",
                Dock = DockStyle.Top
            };

            TransparentcyBar.ValueChanged += (s, e) =>
            {
                TransparencyChanged?.Invoke(TransparentcyBar.Value);
                TransparentcyLabel.Text = $"Transparency: {TransparentcyBar.Value}";
            };

            colorButton = new Button
            {
                Text = "Pick Color",
                Dock = DockStyle.Top
            };

            colorButton.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.Color = currentColor;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        currentColor = cd.Color;
                        previewPanel.BackColor = currentColor;
                        ColorChanged?.Invoke(currentColor);
                    }
                }
            };

            previewPanel = new Panel
            {
                Height = 25,
                Dock = DockStyle.Fill,
                BackColor = Properties.Settings.Default.ClickCircleColor
            };

            Controls.Add(previewPanel);
            Controls.Add(colorButton);
            Controls.Add(TransparentcyBar);
            Controls.Add(TransparentcyLabel);
            Controls.Add(sizeBar);
            Controls.Add(sizeLabel);
        }
    }

    public static class DataStings
    {
        public static readonly string[] blacklistapps =
        [
            "TextInputHost.exe",
            "SystemSettings.exe",
            "ApplicationFrameHost.exe"
        ];

        public static readonly string[] AllowedKeyboardList =
        [
            // Buchstaben A-Z
            "A","B","C","D","E","F","G","H","I","J","K","L","M",
            "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",

            // Zahlen 0-9
            "D0","D1","D2","D3","D4","D5","D6","D7","D8","D9",

            // Numpad 0-9
            "NumPad0","NumPad1","NumPad2","NumPad3","NumPad4",
            "NumPad5","NumPad6","NumPad7","NumPad8","NumPad9",

            // Numpad Operatoren
            "Add",      // +
            "Subtract", // -
            "Multiply", // *
            "Divide",   // /


            // Steuerungstasten
            "Left", // Pfeiltaste links
            "Right", // Pfeiltaste rechts
            "Up", // Pfeiltaste oben
            "Down", // Pfeiltaste unten
            "Enter", // Enter-Taste
            "Return", // Enter-Taste
            "Capital", // CapsLock
            "Space", // Leertaste
            "Back", // Backspace-Taste
            "Tab", // Tabulator-Taste
            "ShiftKey", // Shift-Taste
            "ControlKey", // Strg-Taste
            "Menu", // Alt-Taste

            // F-Tasten
            "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10",
            "F11","F12",

            // Sonderzeichen (wenn über Tastatur erreichbar)
            "Oem6",  // ^ (je nach Tastatur)
            "Oemcomma", // ,
            "OemPeriod", // .
            "OemMinus", // -
            "Oem7", // # oder ' je nach Layout
            "Oemplus", // +
            "Oem3",  // ´ (auch ~ bei US-Tastatur)
            "Oem5", // \ (Backslash)
            "OemQuestion", // ? (je nach Tastatur)
            "OemPipe", // | (je nach Tastatur)
            "OemComma", // ,
            "Oem2", // ; (je nach Tastatur)
            "Oem4", // [ (je nach Tastatur)
            "OemSemicolon", // ; (je nach Tastatur)
            "OemBackslash" // \ (je nach Tastatur)
        ];

        public static readonly Dictionary<string, VirtualKeyCode> keyMap = new Dictionary<string, VirtualKeyCode>
        {
            // Buchstaben A-Z
            ["A"] = VirtualKeyCode.VK_A,
            ["B"] = VirtualKeyCode.VK_B,
            ["C"] = VirtualKeyCode.VK_C,
            ["D"] = VirtualKeyCode.VK_D,
            ["E"] = VirtualKeyCode.VK_E,
            ["F"] = VirtualKeyCode.VK_F,
            ["G"] = VirtualKeyCode.VK_G,
            ["H"] = VirtualKeyCode.VK_H,
            ["I"] = VirtualKeyCode.VK_I,
            ["J"] = VirtualKeyCode.VK_J,
            ["K"] = VirtualKeyCode.VK_K,
            ["L"] = VirtualKeyCode.VK_L,
            ["M"] = VirtualKeyCode.VK_M,
            ["N"] = VirtualKeyCode.VK_N,
            ["O"] = VirtualKeyCode.VK_O,
            ["P"] = VirtualKeyCode.VK_P,
            ["Q"] = VirtualKeyCode.VK_Q,
            ["R"] = VirtualKeyCode.VK_R,
            ["S"] = VirtualKeyCode.VK_S,
            ["T"] = VirtualKeyCode.VK_T,
            ["U"] = VirtualKeyCode.VK_U,
            ["V"] = VirtualKeyCode.VK_V,
            ["W"] = VirtualKeyCode.VK_W,
            ["X"] = VirtualKeyCode.VK_X,
            ["Y"] = VirtualKeyCode.VK_Y,
            ["Z"] = VirtualKeyCode.VK_Z,

            // Zahlen 0-9 (oben auf der Tastatur)
            ["D0"] = VirtualKeyCode.VK_0,
            ["D1"] = VirtualKeyCode.VK_1,
            ["D2"] = VirtualKeyCode.VK_2,
            ["D3"] = VirtualKeyCode.VK_3,
            ["D4"] = VirtualKeyCode.VK_4,
            ["D5"] = VirtualKeyCode.VK_5,
            ["D6"] = VirtualKeyCode.VK_6,
            ["D7"] = VirtualKeyCode.VK_7,
            ["D8"] = VirtualKeyCode.VK_8,
            ["D9"] = VirtualKeyCode.VK_9,

            // Numpad 0-9
            ["NumPad0"] = VirtualKeyCode.NUMPAD0,
            ["NumPad1"] = VirtualKeyCode.NUMPAD1,
            ["NumPad2"] = VirtualKeyCode.NUMPAD2,
            ["NumPad3"] = VirtualKeyCode.NUMPAD3,
            ["NumPad4"] = VirtualKeyCode.NUMPAD4,
            ["NumPad5"] = VirtualKeyCode.NUMPAD5,
            ["NumPad6"] = VirtualKeyCode.NUMPAD6,
            ["NumPad7"] = VirtualKeyCode.NUMPAD7,
            ["NumPad8"] = VirtualKeyCode.NUMPAD8,
            ["NumPad9"] = VirtualKeyCode.NUMPAD9,

            // Numpad Operatoren
            ["Add"] = VirtualKeyCode.ADD,
            ["Subtract"] = VirtualKeyCode.SUBTRACT,
            ["Multiply"] = VirtualKeyCode.MULTIPLY,
            ["Divide"] = VirtualKeyCode.DIVIDE,

            // Steuerungstasten
            ["Left"] = VirtualKeyCode.LEFT,
            ["Right"] = VirtualKeyCode.RIGHT,
            ["Up"] = VirtualKeyCode.UP,
            ["Down"] = VirtualKeyCode.DOWN,
            ["Enter"] = VirtualKeyCode.RETURN,
            ["Return"] = VirtualKeyCode.RETURN,
            ["CapsLock"] = VirtualKeyCode.CAPITAL,
            ["Space"] = VirtualKeyCode.SPACE,
            ["Back"] = VirtualKeyCode.BACK,
            ["Tab"] = VirtualKeyCode.TAB,
            ["ShiftKey"] = VirtualKeyCode.SHIFT,
            ["ControlKey"] = VirtualKeyCode.CONTROL,
            ["Menu"] = VirtualKeyCode.MENU,

            // F-Tasten
            ["F1"] = VirtualKeyCode.F1,
            ["F2"] = VirtualKeyCode.F2,
            ["F3"] = VirtualKeyCode.F3,
            ["F4"] = VirtualKeyCode.F4,
            ["F5"] = VirtualKeyCode.F5,
            ["F6"] = VirtualKeyCode.F6,
            ["F7"] = VirtualKeyCode.F7,
            ["F8"] = VirtualKeyCode.F8,
            ["F9"] = VirtualKeyCode.F9,
            ["F10"] = VirtualKeyCode.F10,
            ["F11"] = VirtualKeyCode.F11,
            ["F12"] = VirtualKeyCode.F12,

            // Sonderzeichen (Tastatur abhängig!)
            ["Oem6"] = VirtualKeyCode.OEM_6,
            ["Oemcomma"] = VirtualKeyCode.OEM_COMMA,
            ["OemPeriod"] = VirtualKeyCode.OEM_PERIOD,
            ["OemMinus"] = VirtualKeyCode.OEM_MINUS,
            ["Oem7"] = VirtualKeyCode.OEM_7,
            ["Oemplus"] = VirtualKeyCode.OEM_PLUS,
            ["Oem3"] = VirtualKeyCode.OEM_3,
            ["Oem5"] = VirtualKeyCode.OEM_5,
            ["OemQuestion"] = VirtualKeyCode.OEM_2,
            ["OemPipe"] = VirtualKeyCode.OEM_5,
            ["OemComma"] = VirtualKeyCode.OEM_COMMA,
            ["Oem2"] = VirtualKeyCode.OEM_2,
            ["Oem4"] = VirtualKeyCode.OEM_4,
            ["OemSemicolon"] = VirtualKeyCode.OEM_1,
            ["OemBackslash"] = VirtualKeyCode.OEM_5
        };

        public static readonly string[] AllowedMouseList =
        {
            "LButton",    // Linksklick
            "RButton",   // Rechtsklick
            "MButton",     // Mausrad-Klick
            "XButton1",   // Zusätzliche Taste 1 (z. B. Daumentaste)
            "XButton2"    // Zusätzliche Taste 2
        };
    }
}
