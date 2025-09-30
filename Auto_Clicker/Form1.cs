using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WindowsInput;
using static Auto_Clicker.ColorClickerFunc;
using static System.Windows.Forms.DataFormats;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;



namespace Auto_Clicker
{
    public partial class Form1 : Form
    {
        private ActionsFunc ActionsFunc;
        public SideForm sideForm;
        public RecorderFunc recorderFunc;
        public ColorClickerFunc colorClickerFunc;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);


        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = -20;
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        /*private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;*/
        private const int MOUSEEVENTF_XDOWN = 0x0080;
        private const int MOUSEEVENTF_XUP = 0x0100;
        private const int XBUTTON1 = 0x0001;
        private const int XBUTTON2 = 0x0002;


        public bool clicking = false;
        public Keys hotkey = Keys.F6;
        private Keys clickKey = Keys.LButton; // Standard: Linksklick
        private bool keyWasDown = false;

        public int clickIndex = 0;

        private CursorOverlayForm? cursorOverlay;
        private OverlaySettingsForm? settingsForm;

        public CancellationTokenSource? clickCts;
        public readonly object clickLock = new();

        public CursorOverlayForm? Clickoverlay;

        public System.Windows.Forms.Timer hotkeyTimer;


        private void SaveSettings()
        {
            Properties.Settings.Default.UseMouseMode = UseMouse.Checked; // true = Maus, false = Tastatur

            Properties.Settings.Default.Hotkey = hotkey.ToString(); // Hotkey für den Autoclicker
            Properties.Settings.Default.HoldOrSwitch = HoldToClick.Checked ? "hold" : "switch"; // "hold" oder "switch"
            Properties.Settings.Default.UsePageSlecter = SelectedFuncUse.SelectedIndex; // 0 = Main, 1 = Actions, 3 = Color Clicker
            Properties.Settings.Default.Whitlistchecked = WhitelistappsCheck.Checked; // true = Whitelist, false = Blacklist

            Properties.Settings.Default.Clickkey = clickKey.ToString(); // Taste für den Klick

            Properties.Settings.Default.ClickMode = ClicksPersSecButton.Checked ? "cps" : "time"; // "cps" oder "time"
            Properties.Settings.Default.ClicksPerSec = ClickPerSecNum.Value; // Klicks pro Sekunde

            Properties.Settings.Default.RepeatInfinite = RepeatUnlimited.Checked; // true = unendlich, false = wiederholen
            Properties.Settings.Default.RepeatCount = RepeatTimes.Value; // Anzahl der Wiederholungen
            Properties.Settings.Default.SPerTimems = (int)PerTimems.Value; // Millisekunden
            Properties.Settings.Default.SPerTimesec = (int)PerTimesec.Value; // Sekunden
            Properties.Settings.Default.SPerTimemin = (int)PerTimemin.Value; // Minuten
            Properties.Settings.Default.SPerTimehour = (int)PerTimehour.Value; // Stunden
            Properties.Settings.Default.STimeBetweenAction = (int)TimeBetweenAction.Value; // Zeit zwischen den Aktionen (in ms)
            Properties.Settings.Default.SIgnoreWait = IgnoreWaitCheck.Checked;

            var parts = ActionsFunc.SavedActions.Select(action =>
            {
                if (action.Type == ActionType.MouseClick)
                    return $"M:{action.MousePosition.X}:{action.MousePosition.Y}:<{action.Mousepress}>";
                else if (action.Type == ActionType.KeyPress && action.Key.HasValue)
                    return $"K:{action.Key}";
                else if (action.Type == ActionType.Waittime && action.ToWait > 0)
                    return $"W:{action.ToWait}";
                return null;
            }).Where(s => s != null);
            Properties.Settings.Default.SavedPoints = string.Join(";", parts); // Speichern der gespeicherten Punkte
            Properties.Settings.Default.ShowPointCLick = ShowPointOnClick.Checked; // true = Punkt anzeigen, false = keinen Punkt anzeigen
            Properties.Settings.Default.SActionRepeatTimes = (int)ActionRepeatTimes.Value; // Anzahl der Wiederholungen der Aktionen

            Properties.Settings.Default.SaveOnExit = SettingsSaveonexit.Checked; // true = Einstellungen speichern beim Schließen
            Properties.Settings.Default.SetOnTop = setTopMostMenu.Checked;  // true = Fenster immer im Vordergrund
            Properties.Settings.Default.DisableWindowOnPosition = disableWindowOnPositionMenu.Checked; // true = Fenster deaktivieren, wenn Position gespeichert ist
            Properties.Settings.Default.DisableredBox = disableRedBoxMenu.Checked; // true = rote Box deaktivieren
            Properties.Settings.Default.SaveAppsListToo = SaveAppsToOnExitMenu.Checked; // true = Liste der Fenster speichern
            Properties.Settings.Default.TooltipShowHide = AddTooltipsMenu.Checked; // true = Tooltip anzeigen, false = keinen Tooltip anzeigen
            Properties.Settings.Default.TabPagesSelected = TabPages.SelectedIndex; // Speichern des ausgewählten Tabs
            Properties.Settings.Default.HotkeySettingsVisible = HotkeyBoxOC.Visible; // true = Hotkey Einstellungen sichtbar
            Properties.Settings.Default.SAutoUsePage = AutoUsePageCheck.Checked; // true = Auto Use Page, false = manuell

            if (SaveAppsToOnExitMenu.Checked)
            {
                var Blackcollection = new System.Collections.Specialized.StringCollection();
                Blackcollection.AddRange(sideForm.BlacklistedWindowTitles.ToArray());
                Properties.Settings.Default.BlacklistedApps = Blackcollection;

                var Whitecollection = new System.Collections.Specialized.StringCollection();
                Whitecollection.AddRange(sideForm.AppsCheckedlist.ToArray());
                Properties.Settings.Default.AppsChecked = Whitecollection;
            }

            Properties.Settings.Default.SRectUseArea = $"{colorClickerFunc.scanArea.X},{colorClickerFunc.scanArea.Y},{colorClickerFunc.scanArea.Width},{colorClickerFunc.scanArea.Height}";
            Properties.Settings.Default.SUseScanColor = ColorSetColor.BackColor;
            Properties.Settings.Default.SIntervalofScans = (int)ColorIntervalScan.Value;
            Properties.Settings.Default.SToleranceofColors = (int)ColorToleranzenScan.Value;

            Properties.Settings.Default.Save(); // Speichern der Einstellungen
        }

        private void LoadSettings()
        {
            UseMouse.Checked = Properties.Settings.Default.UseMouseMode; // true = Maus, false =  Tastatur
            UseKeyboard.Checked = !Properties.Settings.Default.UseMouseMode;

            ShowPointOnClick.Checked = Properties.Settings.Default.ShowPointCLick;

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
            if (Properties.Settings.Default.SAutoUsePage) // true = Auto Use Page, false = manuell
            {
                AutoUsePageCheck.Checked = true;
                AutoUsePageCheck.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
            else
            {
                AutoUsePageCheck.Checked = false;
                AutoUsePageCheck.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }

            if (!Properties.Settings.Default.HotkeySettingsVisible)
            {
                Settingsbartoggle();
            }
            else if (!Settingsbarexpanded)
            {
                Settingsbartoggle();
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
                PerTimems.Enabled = false;
                PerTimesec.Enabled = false;
                PerTimemin.Enabled = false;
                PerTimehour.Enabled = false;
            }
            else
            {
                PerTimeButton.Checked = true;
                ClickRepeatgroup.Enabled = true;
                PerTimems.Enabled = true;
                PerTimesec.Enabled = true;
                PerTimemin.Enabled = true;
                PerTimehour.Enabled = true;
            }

            RepeatUnlimited.Checked = Properties.Settings.Default.RepeatInfinite; // true = unedlich, false = wiederholen
            RepeatRepeat.Checked = !Properties.Settings.Default.RepeatInfinite; // true = unedlich, false = wiederholen

            PerTimems.Value = Properties.Settings.Default.SPerTimems; // Millisekunden
            PerTimesec.Value = Properties.Settings.Default.SPerTimesec; // Sekunden
            PerTimemin.Value = Properties.Settings.Default.SPerTimemin; // Minuten
            PerTimehour.Value = Properties.Settings.Default.SPerTimehour; // Stunden

            RepeatTimes.Value = Properties.Settings.Default.RepeatCount; // Anzahl der Wiederholungen

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
                            if (parts.Length >= 2 &&
                                int.TryParse(parts[0], out int x) &&
                                int.TryParse(parts[1], out int y))
                            {
                                string keyPart = entry.Contains("<") && entry.Contains(">")
                                    ? entry.Substring(entry.IndexOf('<') + 1, entry.IndexOf('>') - entry.IndexOf('<') - 1)
                                    : null;
                                ActionsFunc.SavedActions.Add(new ClickOrKeyAction
                                {
                                    Type = ActionType.MouseClick,
                                    MousePosition = new Point(x, y),
                                    Mousepress = Enum.TryParse(keyPart, out Keys parsedKey) ? parsedKey : (Keys?)null

                                });
                            }
                        }
                        else if (entry.StartsWith("K:")) // Tastendruck
                        {
                            string keyStr = entry.Substring(2);
                            if (Enum.TryParse<Keys>(keyStr, out Keys key))
                            {
                                ActionsFunc.SavedActions.Add(new ClickOrKeyAction
                                {
                                    Type = ActionType.KeyPress,
                                    Key = key
                                });
                            }

                        }
                        else if (entry.StartsWith("W:"))
                        {
                            string waitStr = entry.Substring(2);
                            if (long.TryParse(waitStr, out long parsedWaitTime) && parsedWaitTime > 0)
                            {
                                ActionsFunc.SavedActions.Add(new ClickOrKeyAction
                                {
                                    Type = ActionType.Waittime,
                                    ToWait = parsedWaitTime
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

            SelectedFuncUse.SelectedIndex = Properties.Settings.Default.UsePageSlecter; // true = Use Action, false = Use Main
            IgnoreWaitCheck.Checked = Properties.Settings.Default.SIgnoreWait; // true = Ignore Wait, false = Use Wait
            TimeBetweenAction.Value = Properties.Settings.Default.STimeBetweenAction; // Zeit zwischen den Aktionen (in ms)

            ActionRepeatTimes.Value = Properties.Settings.Default.SActionRepeatTimes; // Anzahl der Wiederholungen der Aktionen

            if (Properties.Settings.Default.Whitlistchecked) // true = Whitelist, false = Blacklist
            {
                WhitelistappsCheck.Checked = true;
                sideForm.BlackWhiteListAppsGroup.Text = "Whitelist Apps";
            }
            else
            {
                WhitelistappsCheck.Checked = false;
                sideForm.BlackWhiteListAppsGroup.Text = "Blacklist Apps";
            }

            var stored = Properties.Settings.Default.BlacklistedApps;
            if (stored != null)
            {
                sideForm.BlacklistedWindowTitles.AddRange(stored.Cast<string>());
            }
            var stored1 = Properties.Settings.Default.AppsChecked;
            if (stored1 != null)
            {
                sideForm.AppsCheckedlist.AddRange(stored1.Cast<string>());
            }

            switch (Properties.Settings.Default.TabPagesSelected)
            {   case 0:
                    TabPages.SelectedIndex = 0;
                    TabPages.Size = new Size(612, 218);
                    break;
                case 1:
                    TabPages.SelectedIndex = 1;
                    TabPages.Size = new Size(612, 377);
                    break;
                case 2:
                    TabPages.SelectedIndex = 2;
                    TabPages.Size = new Size(612, 152);
                    break;
            }
            if (Properties.Settings.Default.SRectUseArea.Length >= 3)
            {
                string[] Rectparts = Properties.Settings.Default.SRectUseArea.Split(',');

                colorClickerFunc.scanArea = new Rectangle(
                    int.Parse(Rectparts[0]),
                    int.Parse(Rectparts[1]),
                    int.Parse(Rectparts[2]),
                    int.Parse(Rectparts[3])
                );
            }
            else
                colorClickerFunc.scanArea = Rectangle.Empty;
            UpdateColorClickAreaText(colorClickerFunc.scanArea);
            ColorSetColor.BackColor = Properties.Settings.Default.SUseScanColor;
            ColorIntervalScan.Value = Properties.Settings.Default.SIntervalofScans;
            ColorToleranzenScan.Value = Properties.Settings.Default.SToleranceofColors;

            ActionsFunc.UpdateActionList(); // UI aktualisieren
        }

        private void ResetSettings()
        {
            var backupBlacklist = Properties.Settings.Default.BlacklistedApps;
            var backupChecklist = Properties.Settings.Default.AppsChecked;
            sideForm.BlacklistedWindowTitles.Clear();

            Properties.Settings.Default.Reset();  // Setzt auf Standardwerte zurück
            Properties.Settings.Default.Save(); // Speichern der Einstellungen

            Properties.Settings.Default.BlacklistedApps = backupBlacklist;
            Properties.Settings.Default.AppsChecked = backupChecklist;
            Properties.Settings.Default.Save();

            ActionsFunc.SavedActions.Clear();
            sideForm.AllAppsList.Items.Clear();

            LoadSettings();         // Lade die nun zurückgesetzten Werte
            ActionsFunc.UpdateActionList();   // UI aktualisieren
            sideForm.btnRefreshWindows();
            sideForm.reloadCheckedApps();

        }

        public Form1()
        {
            InitializeComponent();

            sideForm = new SideForm(this);
            sideForm.FormBorderStyle = FormBorderStyle.None;
            sideForm.ShowInTaskbar = false;
            sideForm.TopMost = false;
            sideForm.Show();
            sideForm.Visible = false;
            sideForm.Width = 0;

            this.MinimumSizeChanged += MainForm_MinimizeChanged;
            this.LocationChanged += MainForm_LocationChanged;
            this.SizeChanged += MainForm_LocationChanged;
            MainForm_LocationChanged(null, null); // direkt initial setzen


            ActionsFunc = new ActionsFunc(this);
            recorderFunc = new RecorderFunc(this);
            colorClickerFunc = new ColorClickerFunc(this);

            hotkeyTimer = new System.Windows.Forms.Timer();
            hotkeyTimer.Interval = 10;
            hotkeyTimer.Tick += HotkeyTimer_Tick;
            hotkeyTimer.Start();

            Sidebartimer = new System.Windows.Forms.Timer();
            Sidebartimer.Interval = 15;
            Sidebartimer.Tick += SidebarTimer_Tick;

            this.KeyPreview = false;
            //this.KeyDown += Form1_KeyDown;
        }

        private bool sidebarexpanded = false;
        private bool Settingsbarexpanded = true;
        private System.Windows.Forms.Timer Sidebartimer;

        public void SidebarToggle()
        {
            sidebarexpanded = !sidebarexpanded;
            SideBarOC.Text = sidebarexpanded ? "▼" : "▶";
            Sidebartimer.Start();
        }

        private void Settingsbartoggle()
        {
            CloseOpenHotkey.Enabled = false;
            Settingsbarexpanded = !Settingsbarexpanded;
            if (Settingsbarexpanded)
                HotkeyBoxOC.Visible = true;
            for (int i = 0; i < 92; i += 1)
            {
                if (Settingsbarexpanded)
                {
                    TabPages.Location = new Point(TabPages.Location.X, TabPages.Location.Y + 1);
                    Application.DoEvents();
                }
                else
                {
                    TabPages.Location = new Point(TabPages.Location.X, TabPages.Location.Y - 1);
                    Application.DoEvents();
                }
            }
            CloseOpenHotkey.Text = Settingsbarexpanded ? "▼" : "▶";
            if (!Settingsbarexpanded)
                HotkeyBoxOC.Visible = !HotkeyBoxOC.Visible;
            CloseOpenHotkey.Enabled = true;
        }

        private void SidebarTimer_Tick(object sender, EventArgs e)
        {
            if (sidebarexpanded && sideForm.Width < 268)
            {
                sideForm.Visible = true;
                sideForm.Width += 10;
            }
            else if (!sidebarexpanded && sideForm.Width > 0)
            {
                sideForm.Width -= 10;
                if (sideForm.Width <= 5)
                {
                    sideForm.Visible = false;
                }
            }
            else
                Sidebartimer.Stop();
        }

        private void MainForm_LocationChanged(object? sender, EventArgs e)
        {
            if (sideForm != null && !sideForm.IsDisposed)
            {
                sideForm.Location = new Point(this.Right - 7, this.Top + 31);
                sideForm.Height = 279;
            }
        }

        private void MainForm_MinimizeChanged(object? sender, EventArgs e)
        {
            if (sideForm != null && !sideForm.IsDisposed)
            {
                sideForm.Width = 0;
                sideForm.Visible = false;
            }
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

            // Lade die Einstellungen
            LoadSettings();
            sideForm.btnRefreshWindows();
            sideForm.reloadCheckedApps();
            Setinfotextfast("Infos LOL");

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SettingsSaveonexit.Checked)
            {
                if (SaveAppsToOnExitMenu.Checked)
                {
                    var collection = new System.Collections.Specialized.StringCollection();
                    collection.AddRange(sideForm.BlacklistedWindowTitles.ToArray());
                    Properties.Settings.Default.BlacklistedApps = collection;
                }
                SaveSettings();
            }
            if (sideForm != null && !sideForm.IsDisposed)
                sideForm.Close();
        }

        public void DoClick(bool mouseclick = false, bool keyclick = false, Keys toclickpress = Keys.None)
        {
            bool usingaction = SelectedFuncUse.Invoke(() => SelectedFuncUse.SelectedIndex == 1);
            if (UseMouse.Checked && !usingaction || mouseclick)
            {
                if (clickKey == Keys.LButton && !mouseclick || toclickpress == Keys.LButton)
                {
                    new InputSimulator().Mouse
                        .LeftButtonClick();

                    //mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    //mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                }
                else if (clickKey == Keys.RButton && !mouseclick || toclickpress == Keys.RButton)
                {
                    new InputSimulator().Mouse
                        .RightButtonClick();

                    //mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    //mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                }
                else if (clickKey == Keys.MButton && !mouseclick || toclickpress == Keys.MButton)
                {
                    new InputSimulator().Mouse
                        .MiddleButtonClick();

                    //mouse_event(0x20, 0, 0, 0, 0); // MiddleDown
                    //mouse_event(0x40, 0, 0, 0, 0); // MiddleUp
                }
                else if (clickKey == Keys.XButton1 && !mouseclick || toclickpress == Keys.XButton1)
                {
                    new InputSimulator().Mouse
                        .XButtonClick(XBUTTON1);

                    //mouse_event(MOUSEEVENTF_XDOWN, 0, 0, XBUTTON1, 0);
                    //mouse_event(MOUSEEVENTF_XUP, 0, 0, XBUTTON1, 0);
                }
                else if (clickKey == Keys.XButton2 && !mouseclick || toclickpress == Keys.XButton2)
                {
                    new InputSimulator().Mouse
                        .XButtonClick(XBUTTON2);

                    //mouse_event(MOUSEEVENTF_XDOWN, 0, 0, XBUTTON2, 0);
                    //mouse_event(MOUSEEVENTF_XUP, 0, 0, XBUTTON2, 0);
                }
            }
            else if (UseKeyboard.Checked && !usingaction || keyclick)
            {
                string keyName = "";
                if (keyclick)
                {
                    keyName = toclickpress.ToString();
                }
                else
                {
                    keyName = clickKey.ToString();
                }

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
            clickIndex = 0;
            String ActionText = "";
            var TextActionShow = "";

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
                        var (proc, _) = sideForm.GetActiveProcessName();
                        if (WhitelistappsCheck.Checked && sideForm.AppsCheckedlist.Contains(proc) || !WhitelistappsCheck.Checked && !sideForm.AppsCheckedlist.Contains(proc)) //(!BlacklistedWindowTitles.Contains(proc))
                        {
                            if (switchinfotext)
                                Setinfotextfast("Auto clicker running.....", true);
                            switchinfotext = false;

                            //Debug.WriteLine("is Clicking.... " + clickIndex + pos.X + " " + pos.Y);
                            DoClick();
                        }
                        else
                        {
                            if (!switchinfotext)
                                Setinfotextfast("Auto clicker ON: Waiting for None Blacklisted window.......", true);
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
                double repeatCount = (double)RepeatTimes.Value; // wie oft klicken

                double intervalMs = (long)PerTimems.Value;

                if (PerTimesec.Value > 0)
                    intervalMs += (long)PerTimesec.Value * 1000;
                if (PerTimemin.Value > 0)
                    intervalMs += (long)PerTimemin.Value * 60_000;
                if (PerTimehour.Value > 0)
                    intervalMs += (long)PerTimehour.Value * 3_600_000;

                if (intervalMs < 3)
                    intervalMs = 2; // Minimum 2ms

                // Klick-Intervall berechnen
                bool infinite = RepeatUnlimited.Checked;
                int clickCount = 0;
                Task.Run(() =>
                {
                    Stopwatch sw = new Stopwatch();

                    while ((infinite || clickCount < repeatCount) && !token.IsCancellationRequested)
                    {
                        sw.Restart();
                        var (proc, _) = sideForm.GetActiveProcessName();
                        if (WhitelistappsCheck.Checked && sideForm.AppsCheckedlist.Contains(proc) || !WhitelistappsCheck.Checked && !sideForm.AppsCheckedlist.Contains(proc))//(!BlacklistedWindowTitles.Contains(proc))
                        {
                            if (switchinfotext)
                                Setinfotextfast("Auto clicker running.....", true);
                            switchinfotext = false;

                            //Debug.WriteLine("is Clicking.... ");
                            DoClick();
                            //Debug.WriteLine("Pause");
                            if (!infinite)
                            {
                                clickCount++;
                                String Texttoshow = $"Auto clicker ON   Count: {clickCount}";
                                Setinfotextfast(Texttoshow, true);
                            }
                        }
                        else
                        {
                            if (!switchinfotext)
                                Setinfotextfast("Auto clicker ON: Waiting for None Blacklisted window.......", true);
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
                        Setinfotextfast("Auto clicker Stopped.....", true);
                        StopClicking();
                    }
                }, token);
            }
        }

        public void StopClicking()
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
                //Clickoverlay = null;
            }

            Setinfotextfast("Auto clicker Stopped.....", true);
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


        public void Setinfotextfast(string Text = "", bool isinvoke = false)
        {
            if (isinvoke)
            {
                if (!clicking)
                    return;
                Invoke(new Action(() =>
                {
                    InfoLabel.Text = Text;
                }));
            }
            else
            {
                InfoLabel.Text = Text;
            }

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

        public void MakeClickThrough(Form form)
        {
            int extendedStyle = GetWindowLong(form.Handle, GWL_EXSTYLE);
            SetWindowLong(form.Handle, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
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

        public Keys RecordKeysSend(bool Mousefind, bool keyboardfind, bool isHotkey = false, bool isKeypress = false, bool isAction = false)
        {
            bool breakloop = false;
            Keys detectedKey = Keys.None;
            bool ShowError = false;
            string ErrortoShow = "";

            hotkeyTimer.Stop();
            TabPages.Enabled = false;

            using (DarkBackgroundOverlay bg = new DarkBackgroundOverlay())
            using (KeyCaptureOverlay overlay = new KeyCaptureOverlay())
            {
                bg.Show();
                overlay.Show();

                while (detectedKey == Keys.None && !breakloop)
                {
                    int TimerTogoback = 3;
                    Application.DoEvents(); // Damit das Overlay reagiert

                    foreach (Keys key in Enum.GetValues(typeof(Keys)))
                    {
                        short state = GetAsyncKeyState(key);
                        bool Keypressed = (state & 0x8000) != 0;
                        if (Keypressed)
                        {
                            if (key.ToString() == "Escape")
                            {
                                breakloop = true; // Escape-Taste gedrückt, Schleife beenden
                                break;
                            }
                            else if (key == hotkey && isKeypress)
                            {
                                ShowError = true;
                                ErrortoShow = $"(Same as Hotkey): {key}";
                            }
                            else if (key == clickKey && isHotkey)
                            {
                                ShowError = true;
                                ErrortoShow = $"(Same as ClickKey): {key}";
                            }
                            else if (key == hotkey && isAction)
                            {
                                ShowError = true;
                                ErrortoShow = $"(Same as Hotkey): {key}";
                            }
                            else if (Mousefind && DataStings.AllowedMouseList.Contains(key.ToString())) // Taste ist gedrückt
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
                                    break;
                                    //ShowError = true;
                                    //ErrortoShow = $"(You have to Check Mouse): {key}";
                                }
                                else
                                {
                                    detectedKey = key;
                                    break;
                                }
                            }
                            else if (keyboardfind && DataStings.AllowedKeyboardList.Contains(key.ToString())) // Taste ist gedrückt
                            {
                                if (keyboardfind && isKeypress && !UseKeyboard.Checked)
                                {
                                    UseKeyboard.Checked = true;
                                    AddKeysToPress(false);
                                    detectedKey = key;
                                    break;
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
                    }
                    overlay.Focus();
                    if (ShowError)
                    {
                        if (disableRedBoxMenu.Checked)
                        {
                            TimerTogoback = 1;
                        }
                        overlay.BackColor = Color.Red;
                        //overlay.SetMessage($"Not a Valid Key: {detectedKey} Try again after it get back Orange\nBack in: 3");
                        overlay.SetMessage($"Not a Valid Key\n{ErrortoShow}\nTry again in:\n {TimerTogoback}");
                        overlay.Refresh();
                        while (TimerTogoback > 0)
                        {
                            //overlay.SetMessage($"Not a Valid Key: {detectedKey} Try again after it get back Orange\nBack in: {TimerTogoback}");
                            overlay.SetMessage($"Not a Valid Key\n{ErrortoShow}\nTry again in:\n {TimerTogoback}");
                            overlay.Refresh();
                            TimerTogoback--;
                            Thread.Sleep(1000);
                        }
                        overlay.BackColor = Color.Orange;
                        overlay.SetMessage("Press a Valid key.\nPRESS:  ESC  to cancel this process");
                        overlay.Refresh();
                        ShowError = false;
                        Debug.WriteLine("Error: " + ErrortoShow);
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
            TabPages.Enabled = true;
            return detectedKey;
        }

        private void HotkeyTimer_Tick(object? sender, EventArgs e)
        {
            bool keyDown = (GetAsyncKeyState(hotkey) & 0x8000) != 0;

            if (HoldToClick.Checked)
            {
                if (keyDown && !clicking)
                {
                    Setinfotextfast("Auto clicker running.....");
                    switch (SelectedFuncUse.SelectedIndex)
                    {
                        case 0:
                            StartClicking();
                            break;
                        case 1:
                            ActionsFunc.StartClickingAction();
                            break;
                        case 2:
                            colorClickerFunc.StartClickingColor();
                            break;
                    }

                }
                else if (!keyDown && clicking)
                {
                    Setinfotextfast("Auto clicker Stopped.....");
                    StopClicking();
                }
            }
            else if (SwitchToClick.Checked)
            {
                if (keyDown && !keyWasDown)
                {
                    if (!clicking)
                    {
                        Setinfotextfast("Auto clicker running.....");
                        switch (SelectedFuncUse.SelectedIndex)
                        {
                            case 0:
                                StartClicking();
                                break;
                            case 1:
                                ActionsFunc.StartClickingAction();
                                break;
                            case 2:
                                colorClickerFunc.StartClickingColor();
                                break;
                        }
                    }
                    else
                    {
                        Setinfotextfast("Auto clicker Stopped.....");
                        StopClicking();
                    }
                }
            }
            keyWasDown = keyDown;
        }

        public void UpdateColorClickAreaText(Rectangle bounds)
        {
            if (bounds.IsEmpty)
            {
                ColorAreaRecText.Text = "Area: None";
                return;
            }
            String areaText = $"Area: {bounds}";
            Size textSize = TextRenderer.MeasureText(areaText, ColorAreaRecText.Font);
            ColorAreaRecText.Size = new Size(textSize.Width + 10, ColorAreaRecText.Height);
            ColorAreaRecText.Text = areaText;
        }

        private void UseMouse_CheckedChanged(object sender, EventArgs e)
        {
            if (UseMouse.Checked)
            {
                AddKeysToPress(true);
            }
        }

        private void UseKeyboard_CheckedChanged(object sender, EventArgs e)
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
                Setinfotextfast("No Key found or it got canceled");
            }
            else
            {
                hotkey = PressedKey;
                Hotkeypressvalue.SelectedItem = hotkey.ToString();
                Setinfotextfast($"Hotkey set: {hotkey}");
            }
        }

        private void ClickKeyFind_Click(object sender, EventArgs e)
        {
            Keys PressedKey = RecordKeysSend(true, true, false, true);
            if (PressedKey == Keys.None)
            {
                Setinfotextfast("No Key found or it got canceled");
            }
            else
            {
                clickKey = PressedKey;
                KeyToPress.SelectedItem = clickKey.ToString();
                Setinfotextfast($"Click key set: {clickKey}");
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

        private void RepeatRepeat_CheckedChanged(object sender, EventArgs e)
        {
            RepeatTimes.Enabled = true;
        }

        private void ClicksPersSecButton_CheckedChanged(object sender, EventArgs e)
        {
            ClickPerSecNum.Enabled = true;
            HoldToClick.Enabled = true;
            ClickRepeatgroup.Enabled = false;
            PerTimems.Enabled = false;
            PerTimesec.Enabled = false;
            PerTimemin.Enabled = false;
            PerTimehour.Enabled = false;
            ResetPerTime.Enabled = false;

        }

        private void PerTimeButton_CheckedChanged(object sender, EventArgs e)
        {
            PerTimems.Enabled = true;
            PerTimesec.Enabled = true;
            PerTimemin.Enabled = true;
            PerTimehour.Enabled = true;
            ClickRepeatgroup.Enabled = true;
            HoldToClick.Enabled = false;
            SwitchToClick.Checked = true;
            ClickPerSecNum.Enabled = false;
            ResetPerTime.Enabled = true;
        }

        private void RepeatUnlimited_CheckedChanged(object sender, EventArgs e)
        {
            RepeatTimes.Enabled = false;
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

        private void SettingsSaveonexit_Click(object sender, EventArgs e)
        {
            ImageAndTextSwitch(SettingsSaveonexit);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSettings();
            Setinfotextfast("Settings saved.");
        }

        private void disableWindowOnPositionMenu_Click(object sender, EventArgs e)
        {
            ImageAndTextSwitch(disableWindowOnPositionMenu);
        }

        private void disableRedBoxMenu_Click(object sender, EventArgs e)
        {
            ImageAndTextSwitch(disableRedBoxMenu);
        }

        private void SelectedFuncUse_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (SelectedFuncUse.SelectedIndex)
            {
                case 0:
                    PageMain.Text = "Main ⬤";
                    PageActions.Text = "Actions";
                    PageColorClick.Text = "ColorClick";
                    break;
                case 1:
                    PageActions.Text = "Actions ⬤";
                    PageMain.Text = "Main";
                    PageColorClick.Text = "ColorClick";
                    break;
                case 2:
                    PageColorClick.Text = "ColorClick ⬤";
                    PageMain.Text = "Main";
                    PageActions.Text = "Actions";
                    break;
            }
        }



        private void WhitelistappsCheck_CheckedChanged(object sender, EventArgs e)
        {
            sideForm.btnRefreshWindows();
            sideForm.reloadCheckedApps();
            if (WhitelistappsCheck.Checked)
            {
                Setinfotextfast("Whitelist mode enabled");
                sideForm.BlackWhiteListAppsGroup.Text = "Whitelist Apps";
                WhitelistappsCheck.Text = "Using Whitelist";
            }
            else
            {
                Setinfotextfast("Blacklist mode enabled");
                sideForm.BlackWhiteListAppsGroup.Text = "Blacklist Apps";
                WhitelistappsCheck.Text = "Using Blacklist";
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

        private void TabPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TabPages.SelectedTab == PageMain)
            {
                TabPages.Size = new Size(612, 218);
                if (AutoUsePageCheck.Checked)
                    SelectedFuncUse.SelectedIndex = 0;
            }
            else if (TabPages.SelectedTab == PageActions)
            {
                TabPages.Size = new Size(612, 377);
                if (AutoUsePageCheck.Checked)
                    SelectedFuncUse.SelectedIndex = 1;
            }
            else if (TabPages.SelectedTab == PageColorClick)
            {
                TabPages.Size = new Size(612, 152);
                if (AutoUsePageCheck.Checked)
                    SelectedFuncUse.SelectedIndex = 2;
            }
        }


        private void ShowAllPositionsCheck_CheckedChanged(object sender, EventArgs e)
        {
            ActionsFunc.ShowAllPositionens();
        }

        private void PositionSave_Click(object sender, EventArgs e)
        {
            ActionsFunc.PositionSave();
        }

        private void KeySaveInList_Click(object sender, EventArgs e)
        {
            ActionsFunc.KeySaveInAction();
        }

        private void PositionClear_Click(object sender, EventArgs e)
        {
            ActionsFunc.ClearSavedActions();
        }


        private void PositionRemove_Click(object sender, EventArgs e)
        {
            ActionsFunc.RemoveSelectedActions();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionsFunc.RemoveSelectedActions();
        }

        private void moveUToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionsFunc.MoveActions(true);
        }

        private void pToolStripMenuItem_Click(object sender, EventArgs e)
        {

            ActionsFunc.MoveActions(false);
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActionsFunc.EditActionsSelected();
        }

        private void CurserPositionList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActionsFunc.SelectedAction();
        }

        private void WaitAddButton_Click(object sender, EventArgs e)
        {
            ActionsFunc.AddWaitTime();
        }

        private void CloseOpenHotkey_Click(object sender, EventArgs e)
        {
            Settingsbartoggle();
        }

        private void SideBarOC_Click(object sender, EventArgs e)
        {
            sideForm.btnRefreshWindows();
            sideForm.reloadCheckedApps();
            SidebarToggle();
        }

        private void resetSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Do you really want to reset all settings?", "Reset Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                ResetSettings();
                Setinfotextfast("Settings Reset.");
            }
        }

        private void ResetPerTime_Click(object sender, EventArgs e)
        {
            PerTimesec.Value = 0;
            PerTimemin.Value = 0;
            PerTimehour.Value = 0;
            PerTimems.Value = 0;
        }

        private void ActionResetTime_Click(object sender, EventArgs e)
        {
            WaitTimeMs.Value = 0;
            WaitTimeSec.Value = 0;
            WaitTimeMin.Value = 0;
            WaitTimeHour.Value = 0;
        }

        private void RecordButton_Click(object sender, EventArgs e)
        {
            recorderFunc.StartRecording();
        }

        private void PlayRecord_Click(object sender, EventArgs e)
        {
            recorderFunc.playbackstart();
        }

        private void ColorSetArea_Click(object sender, EventArgs e)
        {
            bool wasChecked = ColorShowAreaCheck.Checked;
            if (wasChecked)
                ColorShowAreaCheck.Checked = false; // Deaktivieren, um Konflikte zu vermeiden
            this.WindowState = FormWindowState.Minimized;
            var result = colorClickerFunc.SelectRectangle();

            if (!result.Area.IsEmpty)
            {
                // Screenshot in der PictureBox anzeigen
                //ColorScreenshotArea.Image = result.Screenshot;

                // Falls du die Koordinaten brauchst:
                Setinfotextfast($"Area Saved: {result.Area}");
                colorClickerFunc.scanArea = result.Area;
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
            }
            ColorShowAreaCheck.Checked = wasChecked; // Ursprünglichen Zustand wiederherstellen
        }

        private void AutoUsePageCheck_Click(object sender, EventArgs e)
        {
            ImageAndTextSwitch(AutoUsePageCheck);
        }

        private void ColorShowAreaCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (ColorShowAreaCheck.Checked)
            {
                Form marker = colorClickerFunc.ShowAreaMarker(colorClickerFunc.scanArea);
                colorClickerFunc.ActiveMarker = marker;
            }
            else
            {
                colorClickerFunc.ActiveMarker?.Close();
            }
        }

        private void ColorFullScreenButton_Click(object sender, EventArgs e)
        {
            bool wasChecked = ColorShowAreaCheck.Checked;
            if (wasChecked)
                ColorShowAreaCheck.Checked = false; // Deaktivieren, um Konflikte zu vermeiden
            Screen screen = Screen.FromPoint(Cursor.Position);
            if (screen != null)
            {
                Rectangle bounds = screen.Bounds;
                colorClickerFunc.scanArea = bounds;
                Setinfotextfast($"Area set to full screen: {bounds}");
                UpdateColorClickAreaText(bounds);
            }
            else
            {
                Setinfotextfast("Error: Could not determine the screen.");
            }
            ColorShowAreaCheck.Checked = wasChecked; // Ursprünglichen Zustand wiederherstellen
        }

        private void ColorSetColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                // Optional: vorherige Farbe setzen
                colorDialog.Color = ColorSetColor.BackColor;

                // Optional: erlauben, dass Benutzer benutzerdefinierte Farben speichert
                colorDialog.FullOpen = true;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    Color selectedColor = colorDialog.Color;
                    // Beispiel: Hintergrundfarbe eines Panels setzen
                    ColorSetColor.BackColor = selectedColor;
                }
            }
        }

        private void ColorPickFromScreen_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            var picker = new ColorClickerFunc.ColorPicker();
            Color picked = picker.Show();
            ColorSetColor.BackColor = picked;
            Setinfotextfast($"Gewählte Farbe: {picked}");
            this.WindowState = FormWindowState.Normal;
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
            "Insert", // Einfügen
            "Delete", // Entf-Taste
            "Home", // Pos1
            "End", // Ende-Taste
            "PageUp", // Bild hoch
            "PageDown", // Bild runter
            "Next", // Bild runter

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
            ["Insert"] = VirtualKeyCode.INSERT,
            ["Delete"] = VirtualKeyCode.DELETE,
            ["Home"] = VirtualKeyCode.HOME,
            ["End"] = VirtualKeyCode.END,
            ["PageUp"] = VirtualKeyCode.PRIOR,
            ["PageDown"] = VirtualKeyCode.NEXT,

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
