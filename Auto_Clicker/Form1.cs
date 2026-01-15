using System;
using System.Data;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using WindowsInput;
using static Auto_Clicker.ActionsFunc;



namespace Auto_Clicker
{
    public partial class Form1 : Form
    {
        private ActionsFunc _Actions;
        public SideForm _SideForm;
        public RecorderFunc recorderFunc;
        public ColorClickerFunc _ColorClick;

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

        private bool sidebarexpanded = false;
        private System.Windows.Forms.Timer Sidebartimer;

        List<string> _infoLogs = new();

        public int clickIndex = 0;

        private CursorOverlayForm? cursorOverlay;
        private OverlaySettingsForm? settingsForm;

        public CancellationTokenSource? clickCts;
        public readonly object clickLock = new();

        public CursorOverlayForm? Clickoverlay;

        public System.Windows.Forms.Timer hotkeyTimer;

        private static readonly ThreadLocal<Random> _rng =
                        new(() => new Random(Guid.NewGuid().GetHashCode()));




        //Settings
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
            Properties.Settings.Default.SNum_Randommizer = (int)Num_Randomizer.Value; // Randomizer für Klicks pro Sekunde

            Properties.Settings.Default.RepeatInfinite = RepeatUnlimited.Checked; // true = unendlich, false = wiederholen
            Properties.Settings.Default.RepeatCount = RepeatTimes.Value; // Anzahl der Wiederholungen
            Properties.Settings.Default.SPerTimems = (int)PerTimems.Value; // Millisekunden
            Properties.Settings.Default.SPerTimesec = (int)PerTimesec.Value; // Sekunden
            Properties.Settings.Default.SPerTimemin = (int)PerTimemin.Value; // Minuten
            Properties.Settings.Default.SPerTimehour = (int)PerTimehour.Value; // Stunden
            Properties.Settings.Default.STimeBetweenAction = (int)TimeBetweenAction.Value; // Zeit zwischen den Aktionen (in ms)
            Properties.Settings.Default.SIgnoreWait = IgnoreWaitCheck.Checked;

            var parts = _Actions.SavedActions.Select(action =>
            {
                if (action.Type == ActionType.MouseClick)
                    return $"M:{action.MousePosition.X}:{action.MousePosition.Y}:{action.HoldClickMS}:<{action.Mousepress}>";
                else if (action.Type == ActionType.KeyPress && action.Key.HasValue)
                    return $"K:{action.Key}:{action.HoldClickMS}";
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
                Blackcollection.AddRange(_SideForm.BlacklistedWindowTitles.ToArray());
                Properties.Settings.Default.BlacklistedApps = Blackcollection;

                var Whitecollection = new System.Collections.Specialized.StringCollection();
                Whitecollection.AddRange(_SideForm.AppsCheckedlist.ToArray());
                Properties.Settings.Default.AppsChecked = Whitecollection;
            }

            Properties.Settings.Default.SRectUseArea = $"{_ColorClick.scanArea.X},{_ColorClick.scanArea.Y},{_ColorClick.scanArea.Width},{_ColorClick.scanArea.Height}";
            Properties.Settings.Default.SCheckIsHoldingon = CheckIsHoldingon.Checked;
            Properties.Settings.Default.SNumColorHoldTime = (int)ColTimeHolding.Value;
            Properties.Settings.Default.SUseScanColor = ColorSetColor.BackColor;
            Properties.Settings.Default.SIntervalofScans = (int)ColorIntervalScan.Value;
            Properties.Settings.Default.SToleranceofColors = (int)ColorToleranzenScan.Value;

            Properties.Settings.Default.SBoxSelectedindex = Color_SelectActionsbox.SelectedIndex;
            Properties.Settings.Default.SColorSavedClickkey = (int)_ColorClick.ClickaKey_Key;
            Properties.Settings.Default.SColorSavedPosClick = (int)_ColorClick.ClickPos_Key;
            Properties.Settings.Default.SColorSavedPoint = new Point(_ColorClick.ClickPosition.X, _ColorClick.ClickPosition.Y);

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
                Num_Randomizer.Enabled = true;
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
                Num_Randomizer.Enabled = false;
            }

            Num_Randomizer.Value = Properties.Settings.Default.SNum_Randommizer; // Randomizer für Klicks pro Sekunde

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
                            if (parts.Length >= 3 &&
                                int.TryParse(parts[0], out int x) &&
                                int.TryParse(parts[1], out int y) &&
                                long.TryParse(parts[2], out long time))
                            {
                                string keyPart = entry.Contains("<") && entry.Contains(">")
                                    ? entry.Substring(entry.IndexOf('<') + 1, entry.IndexOf('>') - entry.IndexOf('<') - 1)
                                    : null;
                                _Actions.SavedActions.Add(new ClickOrKeyAction
                                {
                                    Type = ActionType.MouseClick,
                                    MousePosition = new Point(x, y),
                                    Mousepress = Enum.TryParse(keyPart, out Keys parsedKey) ? parsedKey : (Keys?)null,
                                    HoldClickMS = time

                                });
                            }
                        }
                        else if (entry.StartsWith("K:")) // Tastendruck
                        {
                            var parts = entry.Split(":");

                            if (parts.Length >= 2 &&
                                Enum.TryParse(parts[1], out Keys key))
                            {
                                long holdMs = 0;

                                if (parts.Length >= 3)
                                    long.TryParse(parts[2], out holdMs);

                                _Actions.SavedActions.Add(new ClickOrKeyAction
                                {
                                    Type = ActionType.KeyPress,
                                    Key = key,
                                    HoldClickMS = holdMs
                                });
                            }

                        }
                        else if (entry.StartsWith("W:"))
                        {
                            string waitStr = entry.Substring(2);
                            if (long.TryParse(waitStr, out long parsedWaitTime) && parsedWaitTime > 0)
                            {
                                _Actions.SavedActions.Add(new ClickOrKeyAction
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
                _SideForm.BlackWhiteListAppsGroup.Text = "Whitelist Apps";
                WhitelistappsCheck.Text = "Using Whitelist";
            }
            else
            {
                WhitelistappsCheck.Checked = false;
                _SideForm.BlackWhiteListAppsGroup.Text = "Blacklist Apps";
                WhitelistappsCheck.Text = "Using Blacklist";
            }

            var stored = Properties.Settings.Default.BlacklistedApps;
            if (stored != null)
            {
                _SideForm.BlacklistedWindowTitles.AddRange(stored.Cast<string>());
            }
            var stored1 = Properties.Settings.Default.AppsChecked;
            if (stored1 != null)
            {
                _SideForm.AppsCheckedlist.AddRange(stored1.Cast<string>());
            }

            TabPages.SelectedIndex = Properties.Settings.Default.TabPagesSelected;
            setwindowsizeFunc();

            if (Properties.Settings.Default.SRectUseArea.Length >= 3)
            {
                string[] Rectparts = Properties.Settings.Default.SRectUseArea.Split(',');

                _ColorClick.scanArea = new Rectangle(
                    int.Parse(Rectparts[0]),
                    int.Parse(Rectparts[1]),
                    int.Parse(Rectparts[2]),
                    int.Parse(Rectparts[3])
                );
            }
            else
                _ColorClick.scanArea = Rectangle.Empty;
            UpdateColorClickAreaText(_ColorClick.scanArea);
            ColorSetColor.BackColor = Properties.Settings.Default.SUseScanColor;
            string hex = $"Hex:#{ColorSetColor.BackColor.R:X2}{ColorSetColor.BackColor.G:X2}{ColorSetColor.BackColor.B:X2}";
            LabelColorPicked.Text = ($"{ColorSetColor.BackColor} {hex}");
            ColorIntervalScan.Value = Properties.Settings.Default.SIntervalofScans;
            ColorToleranzenScan.Value = Properties.Settings.Default.SToleranceofColors;
            CheckIsHoldingon.Checked = Properties.Settings.Default.SCheckIsHoldingon;
            ColTimeHolding.Value = Properties.Settings.Default.SNumColorHoldTime;

            Color_SelectActionsbox.SelectedIndex = Properties.Settings.Default.SBoxSelectedindex;
            _ColorClick.ClickaKey_Key = (Keys)Properties.Settings.Default.SColorSavedClickkey;
            _ColorClick.ClickPos_Key = (Keys)Properties.Settings.Default.SColorSavedPosClick;
            _ColorClick.ClickPosition = Properties.Settings.Default.SColorSavedPoint;
            switch (Color_SelectActionsbox.SelectedIndex)
            {
                case 0:
                    Color_Clickakey_setkey.Visible = false;
                    Color_Clickakey_setkey_Label.Visible = false;
                    break;
                case 1:
                    Color_Clickakey_setkey.Visible = true;
                    Color_Clickakey_setkey_Label.Visible = true;
                    Color_Clickakey_setkey.Text = "Set Key";
                    Color_Clickakey_setkey_Label.Text = "Key: " + _ColorClick.ClickaKey_Key.ToString();
                    break;
                case 2:
                    Color_Clickakey_setkey.Visible = true;
                    Color_Clickakey_setkey_Label.Visible = true;
                    Color_Clickakey_setkey.Text = "Set Position";
                    Color_Clickakey_setkey_Label.Text = $"X:{_ColorClick.ClickPosition.X}, Y:{_ColorClick.ClickPosition.Y} <{_ColorClick.ClickPos_Key.ToString()}>";
                    break;
            }
            _Actions.UpdateActionList(); // UI aktualisieren
        }

        private void ResetSettings()
        {
            var backupBlacklist = Properties.Settings.Default.BlacklistedApps;
            var backupChecklist = Properties.Settings.Default.AppsChecked;
            _SideForm.BlacklistedWindowTitles.Clear();

            Properties.Settings.Default.Reset();  // Setzt auf Standardwerte zurück
            Properties.Settings.Default.Save(); // Speichern der Einstellungen

            Properties.Settings.Default.BlacklistedApps = backupBlacklist;
            Properties.Settings.Default.AppsChecked = backupChecklist;
            Properties.Settings.Default.Save();

            _Actions.SavedActions.Clear();
            _SideForm.AllAppsList.Items.Clear();

            LoadSettings();         // Lade die nun zurückgesetzten Werte
            _Actions.UpdateActionList();   // UI aktualisieren
            _SideForm.btnRefreshWindows();
            _SideForm.reloadCheckedApps();
        }





        //Form1
        public Form1()
        {
            InitializeComponent();

            _SideForm = new SideForm(this);
            _SideForm.FormBorderStyle = FormBorderStyle.None;
            _SideForm.ShowInTaskbar = false;
            _SideForm.TopMost = false;
            _SideForm.Show();
            _SideForm.Visible = false;
            _SideForm.Width = 0;

            this.MinimumSizeChanged += MainForm_MinimizeChanged;
            this.LocationChanged += MainForm_LocationChanged;
            this.SizeChanged += MainForm_LocationChanged;
            MainForm_LocationChanged(null, null); // direkt initial setzen

            _Actions = new ActionsFunc(this);
            recorderFunc = new RecorderFunc(this);
            _ColorClick = new ColorClickerFunc(this);

            hotkeyTimer = new System.Windows.Forms.Timer();
            hotkeyTimer.Interval = 10;
            hotkeyTimer.Tick += HotkeyTimer_Tick;
            hotkeyTimer.Start();

            Sidebartimer = new System.Windows.Forms.Timer();
            Sidebartimer.Interval = 15;
            Sidebartimer.Tick += SidebarTimer_Tick;

            this.KeyPreview = true;
            //this.KeyDown += Form1_KeyDown;

            Switch_to_SavePositon.Click += (s, e) =>
            {
                _Actions.Switch_To_Save_PositionFunc();
            };
            Switch_to_SaveKey.Click += (s, e) =>
            {
                _Actions.Switch_To_Save_KeyFunc();
            };
            Switch_to_Wait.Click += (s, e) =>
            {
                _Actions.Switch_To_WaitFunc();
            };
            ActionMenuSetHold.Click += (s, e) =>
            {
                int selectedIndex = CurserPositionList.SelectedIndex;
                if (selectedIndex == -1)
                {
                    Setinfotextfast("No Action selected to Set Hold.");
                    return;
                }
                ActionType type = _Actions.SavedActions[selectedIndex].Type;
                _Actions.Set_Hold_MessageFunc(selectedIndex);
            };
            Settings_OpenInfoLog.Click += (s, e) => OpenLogWindow();
            Color_Clickakey_setkey.Click += (s, e) =>
            {
                if (Color_SelectActionsbox.SelectedIndex == 1)
                {
                    _ColorClick.Set_Key_ButtonClick();
                }
                else
                {
                    _ColorClick.Set_Position_ButtonClick();
                }
            };
            Move_MoveUP.Click += (s, e) => _Actions.MoveActions(true);
            Move_MoveDown.Click += (s, e) => _Actions.MoveActions(false);
            Move_MoveToPos.Click += (s, e) => _Actions.MoveToNUMFunc();
        }

        public void SidebarToggle()
        {
            sidebarexpanded = !sidebarexpanded;
            SideBarOC.Text = sidebarexpanded ? "▼" : "▶";
            Sidebartimer.Start();
        }

        private void SidebarTimer_Tick(object sender, EventArgs e)
        {
            if (sidebarexpanded && _SideForm.Width < 268)
            {
                _SideForm.Visible = true;
                _SideForm.Width += 10;
            }
            else if (!sidebarexpanded && _SideForm.Width > 0)
            {
                _SideForm.Width -= 10;
                if (_SideForm.Width <= 5)
                {
                    _SideForm.Visible = false;
                }
            }
            else
                Sidebartimer.Stop();
        }

        private void MainForm_LocationChanged(object? sender, EventArgs e)
        {
            if (_SideForm != null && !_SideForm.IsDisposed)
            {
                _SideForm.Location = new Point(this.Right - 7, this.Top + 31);
                _SideForm.Height = 279;
            }
        }

        private void MainForm_MinimizeChanged(object? sender, EventArgs e)
        {
            if (_SideForm != null && !_SideForm.IsDisposed)
            {
                _SideForm.Width = 0;
                _SideForm.Visible = false;
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
            Color_SelectActionsbox.SelectedIndex = 0;
            LoadSettings();
            _SideForm.btnRefreshWindows();
            _SideForm.reloadCheckedApps();
            Setinfotextfast("Infos LOL");

            ActionRightclick.Opening += (s, e) =>
            {
                _Actions.Menu_On_OpenFunc();
            };
            ActionRightclick.Closed += (s, e) =>
            {
                _Actions.Menu_On_CloseFunc();
            };
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SettingsSaveonexit.Checked)
            {
                if (SaveAppsToOnExitMenu.Checked)
                {
                    var collection = new System.Collections.Specialized.StringCollection();
                    collection.AddRange(_SideForm.BlacklistedWindowTitles.ToArray());
                    Properties.Settings.Default.BlacklistedApps = collection;
                }
                SaveSettings();
            }
            if (_SideForm != null && !_SideForm.IsDisposed)
                _SideForm.Close();
        }






        //Folder saver
        static string GetBaseFolder(string? customPath = null)
        {
            if (!string.IsNullOrWhiteSpace(customPath))
                return customPath;

            string downloads = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "Auto_Clicker"
            );

            Directory.CreateDirectory(downloads);
            return downloads;
        }

        string GetPresetFolder()
        {
            string folder = Properties.Settings.Default.SMainFolder;

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                Setinfotextfast("No Folder Found: Creating new Folder");
                folder = GetBaseFolder(); // Downloads\autoclicker

                MessageBox.Show(
                            $"Preset folder was not found.\nA new folder was created:\n\n{folder}",
                            "Folder created",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                Properties.Settings.Default.SMainFolder = folder;
                Properties.Settings.Default.Save();
            }
            return folder;
        }

        void FolderSaveActions(string fileName)
        {
            string folder = GetPresetFolder();
            string path = Path.Combine(folder, fileName + ".json");

            var data = _Actions.SavedActions.Select(a => new ActionData
            {
                Type = a.Type,
                X = a.MousePosition.X,
                Y = a.MousePosition.Y,
                HoldClickMS = a.HoldClickMS,
                Mousepress = a.Mousepress,
                Key = a.Key,
                ToWait = a.ToWait
            }).ToList();

            File.WriteAllText(path,
                JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            Setinfotextfast("Actions Saved as file: " + fileName);
        }

        void LoadActions(string filePath)
        {
            var data = JsonSerializer.Deserialize<List<ActionData>>(
                File.ReadAllText(filePath));

            _Actions.SavedActions.Clear();

            foreach (var d in data)
            {
                switch (d.Type)
                {
                    case ActionType.MouseClick:
                        _Actions.SavedActions.Add(new ClickOrKeyAction
                        {
                            Type = ActionType.MouseClick,
                            MousePosition = new Point(d.X, d.Y),
                            HoldClickMS = d.HoldClickMS,
                            Mousepress = d.Mousepress
                        });
                        break;

                    case ActionType.KeyPress:
                        _Actions.SavedActions.Add(new ClickOrKeyAction
                        {
                            Type = ActionType.KeyPress,
                            Key = d.Key,
                            HoldClickMS = d.HoldClickMS
                        });
                        break;

                    case ActionType.Waittime:
                        _Actions.SavedActions.Add(new ClickOrKeyAction
                        {
                            Type = ActionType.Waittime,
                            ToWait = d.ToWait
                        });
                        break;
                }
            }
            _Actions.UpdateActionList();
        }

        private void Actions_Set_Preset_Folder_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Preset-Ordner für Actions auswählen",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string folder = dialog.SelectedPath;

                Directory.CreateDirectory(folder);

                Properties.Settings.Default.SMainFolder = folder;
                Properties.Settings.Default.Save();
            }
        }

        private void Actions_Save_List_Click(object sender, EventArgs e)
        {
            if (CurserPositionList.Items.Count == 0)
            {
                Setinfotextfast("No Actions to save");
                return;
            }
            var (result, Foldername) = Save_Actions_MessageFunc();
            if (result == DialogResult.OK)
            {
                FolderSaveActions(Foldername);
            }

        }

        public (DialogResult Result, String Filename) Save_Actions_MessageFunc()
        {
            Form form = new Form
            {
                Text = "Save Actions in a file",
                Size = new Size(300, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                TopMost = true
            };

            Label lbl = new Label
            {
                Text = "Save the Actions to a File\nSet Folder name:",
                AutoSize = false,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Size = new Size(260, 60),
                Location = new Point(20, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            TextBox Filename = new TextBox
            {
                Location = new Point(100, 55),
                Size = new Size(100, 35)
            };
            Button DoneButton = new Button
            {
                Text = "Done",
                Location = new Point(60, 95),
                Size = new Size(80, 30)
            };
            Button CancleButton = new Button
            {
                Text = "Cancle",
                DialogResult = DialogResult.Cancel,
                Location = new Point(160, 95),
                Size = new Size(80, 30)
            };

            char[] invalidChars = Path.GetInvalidFileNameChars();

            DoneButton.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(Filename.Text))
                {
                    MessageBox.Show("Please enter a folder name.");
                    return;
                }

                string folder = GetPresetFolder();
                string filePath = Path.Combine(folder, Filename.Text + ".json");

                if (File.Exists(filePath))
                {
                    var overwrite = MessageBox.Show(
                        "A preset with this name already exists.\nDo you want to overwrite it?",
                        "File exists",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (overwrite != DialogResult.Yes)
                        return; // ❗ Dialog bleibt offen
                }

                form.DialogResult = DialogResult.OK;
                form.Close();
            };
            CancleButton.Click += (s, e) =>
            {
                form.Close();
            };
            Filename.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar))
                    return;

                if (invalidChars.Contains(e.KeyChar))
                    e.Handled = true;
            };
            Filename.Leave += (s, e) =>
            {
                foreach (char c in Path.GetInvalidFileNameChars())
                    Filename.Text = Filename.Text.Replace(c.ToString(), "");
            };

            form.Controls.Add(lbl);
            form.Controls.Add(Filename);
            form.Controls.Add(DoneButton);
            form.Controls.Add(CancleButton);

            form.AcceptButton = DoneButton;

            var result = form.ShowDialog();
            return (result, Filename.Text);
        }

        private void Actions_Load_List_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select AutoClicker preset",
                Filter = "AutoClicker Preset (*.json)|*.json",
                InitialDirectory = GetPresetFolder(), // dein gespeicherter Ordner
                Multiselect = false
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                LoadActions(dialog.FileName);
                Setinfotextfast("Loaded the File: " + dialog.FileName);
            }
        }

        private void Presets_Open_Folder_Click(object sender, EventArgs e)
        {
            String Folder = GetPresetFolder();

            if (!Directory.Exists(Folder))
            {
                MessageBox.Show("Folder not found.");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = Folder,
                UseShellExecute = true
            });
        }




        public void DoClick(Keys ToClick, long holdtime = 0)
        {
            if (holdtime == 0)
            {
                if (ToClick == Keys.LButton)
                {
                    new InputSimulator().Mouse
                        .LeftButtonClick();
                }
                else if (ToClick == Keys.RButton)
                {
                    new InputSimulator().Mouse
                        .RightButtonClick();
                }
                else if (ToClick == Keys.MButton)
                {
                    new InputSimulator().Mouse
                        .MiddleButtonClick();
                }
                else if (ToClick == Keys.XButton1)
                {
                    new InputSimulator().Mouse
                        .XButtonClick(XBUTTON1);
                }
                else if (ToClick == Keys.XButton2)
                {
                    new InputSimulator().Mouse
                        .XButtonClick(XBUTTON2);
                }
                else
                {
                    if (DataStings.keyMap.TryGetValue(ToClick.ToString(), out VirtualKeyCode vk))
                    {
                        new InputSimulator().Keyboard
                            .KeyPress(vk);
                    }
                }
            }
            else
            {
                if (ToClick == Keys.LButton)
                {
                    new InputSimulator().Mouse
                        .LeftButtonDown();
                    waitforholding();
                    new InputSimulator().Mouse
                        .LeftButtonUp();
                }
                else if (ToClick == Keys.RButton)
                {
                    new InputSimulator().Mouse
                        .RightButtonDown();
                    waitforholding();
                    new InputSimulator().Mouse
                        .RightButtonUp();
                }
                else if (ToClick == Keys.MButton)
                {
                    new InputSimulator().Mouse
                        .MiddleButtonDown();
                    waitforholding();
                    new InputSimulator().Mouse
                        .MiddleButtonUp();
                }
                else if (ToClick == Keys.XButton1)
                {
                    new InputSimulator().Mouse
                        .XButtonDown(XBUTTON1);
                    waitforholding();
                    new InputSimulator().Mouse
                        .XButtonUp(XBUTTON1);
                }
                else if (ToClick == Keys.XButton2)
                {
                    new InputSimulator().Mouse
                        .XButtonDown(XBUTTON2);
                    waitforholding();
                    new InputSimulator().Mouse
                        .XButtonUp(XBUTTON2);
                }
                if (DataStings.keyMap.TryGetValue(ToClick.ToString(), out VirtualKeyCode vk))
                {
                    new InputSimulator().Keyboard
                        .KeyDown(vk);
                    waitforholding();
                    new InputSimulator().Keyboard
                        .KeyUp(vk);
                }
            }
            void waitforholding()
            {
                Stopwatch time = new Stopwatch();
                time.Restart();
                while (clicking)
                {
                    if (time.ElapsedMilliseconds > holdtime)
                        break;
                    Thread.Sleep(50);
                }
                time.Stop();
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
                        var (proc, _) = _SideForm.GetActiveProcessName();
                        if (WhitelistappsCheck.Checked && _SideForm.AppsCheckedlist.Contains(proc) || !WhitelistappsCheck.Checked && !_SideForm.AppsCheckedlist.Contains(proc)) //(!BlacklistedWindowTitles.Contains(proc))
                        {
                            if (switchinfotext)
                                Setinfotextfast("Auto clicker running.....", true);
                            switchinfotext = false;

                            //Debug.WriteLine("is Clicking.... " + clickIndex + pos.X + " " + pos.Y);
                            DoClick(clickKey);
                        }
                        else
                        {
                            if (!switchinfotext)
                                Setinfotextfast("Auto clicker ON: Waiting for None Blacklisted window.......", true);
                            switchinfotext = true;
                        }

                        // 🔀 ZUFÄLLIGES INTERVALL
                        double jitterPercent = (double)Num_Randomizer.Value / 100; // ±10 %
                        double randomizedInterval = intervalMs;

                        if (jitterPercent > 0)
                        {
                            double jitter = intervalMs * jitterPercent;
                            randomizedInterval += (_rng.Value!.NextDouble() * 2 - 1) * jitter;

                            // Micro-Pause NUR dann
                            if (_rng.Value.Next(0, 100) == 0)
                                randomizedInterval += _rng.Value.Next(30, 120);
                        }

                        double remaining = randomizedInterval - sw.Elapsed.TotalMilliseconds;

                        Debug.WriteLine(intervalMs+"  "+randomizedInterval);

                        if (remaining > 2)
                            Thread.Sleep((int)(remaining - 1));

                        while (sw.Elapsed.TotalMilliseconds < randomizedInterval)
                        {
                            if (!clicking || token.IsCancellationRequested)
                                break;

                            Thread.SpinWait(5);
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
                        var (proc, _) = _SideForm.GetActiveProcessName();
                        if (WhitelistappsCheck.Checked && _SideForm.AppsCheckedlist.Contains(proc) || !WhitelistappsCheck.Checked && !_SideForm.AppsCheckedlist.Contains(proc))//(!BlacklistedWindowTitles.Contains(proc))
                        {
                            if (switchinfotext)
                                Setinfotextfast("Auto clicker running.....", true);
                            switchinfotext = false;

                            //Debug.WriteLine("is Clicking.... ");
                            DoClick(clickKey);
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
                        BeginInvoke(new Action(StopClicking));
                        return;
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
                Clickoverlay = null;
            }
            if (InvokeRequired)
                Setinfotextfast("Auto clicker Stopped.....", true);
            else
                Setinfotextfast("Auto clicker Stopped.....");
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
                            _Actions.StartClickingAction();
                            break;
                        case 2:
                            _ColorClick.StartClickingColor();
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
                                _Actions.StartClickingAction();
                                break;
                            case 2:
                                _ColorClick.StartClickingColor();
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






        //Helper
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

        void OpenLogWindow()
        {
            Form logForm = new Form
            {
                Text = "Logs",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent
            };

            TextBox logBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f)
            };

            logBox.Text = string.Join(Environment.NewLine, _infoLogs);

            logForm.Controls.Add(logBox);
            logForm.Show(this);
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

        public void Setinfotextfast(string Text = "", bool isinvoke = false)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {Text}";
            _infoLogs.Add(line);
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

        public (Keys, long) RecordKeysSend(bool Mousefind, bool keyboardfind, bool isHotkey = false, bool isKeypress = false, bool isAction = false, bool recHold = false)
        {
            bool breakloop = false;
            Keys detectedKey = Keys.None;
            bool ShowError = false;
            string ErrortoShow = "";
            long washolding = 0;
            Stopwatch sw = new Stopwatch();

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
                    if (recHold && CheckAddHold.Checked)
                    {
                        sw.Restart();
                        while ((GetAsyncKeyState(detectedKey) & 0x8000) != 0)
                        {
                            long swtime = sw.ElapsedMilliseconds;
                            List<String> parts = _Actions.Ms_to_PartsString(swtime);
                            if (swtime > 50)
                                overlay.SetMessage($"Key detected: {detectedKey}\nRecoding of Holding: " + string.Join(" ", parts));
                            overlay.Refresh();
                            Thread.Sleep(10);
                        }
                        sw.Stop();
                        if (sw.ElapsedMilliseconds > 50)
                        {
                            washolding = sw.ElapsedMilliseconds;
                        }
                    }
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
            return (detectedKey, washolding);
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
            (Keys PressedKey, long holding) = RecordKeysSend(true, true, true);
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
            (Keys PressedKey, long holding) = RecordKeysSend(true, true, false, true);
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
            Num_Randomizer.Enabled = true;
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
            ResetPerTime.Enabled = true;
            SwitchToClick.Checked = true;

            HoldToClick.Enabled = false;
            ClickPerSecNum.Enabled = false;
            Num_Randomizer.Enabled = false;

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
            _SideForm.btnRefreshWindows();
            _SideForm.reloadCheckedApps();
            if (WhitelistappsCheck.Checked)
            {
                Setinfotextfast("Whitelist mode enabled");
                _SideForm.BlackWhiteListAppsGroup.Text = "Whitelist Apps";
                WhitelistappsCheck.Text = "Using Whitelist";
            }
            else
            {
                Setinfotextfast("Blacklist mode enabled");
                _SideForm.BlackWhiteListAppsGroup.Text = "Blacklist Apps";
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

        public void setwindowsizeFunc()
        {
            var tab = TabPages.SelectedTab;

            // Größe basierend auf Inhalt
            Size needed = tab.PreferredSize;

            // + TopBars berücksichtigen
            int extraHeight =
                panelTopInfo.Height +
                panelHotkey.Height +
                menuStrip1.Height;

            this.ClientSize = new Size(
                Math.Max(this.ClientSize.Width, needed.Width),
                needed.Height + extraHeight + 10
            );
        }

        private void TabPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            setwindowsizeFunc();
            if (AutoUsePageCheck.Checked)
                SelectedFuncUse.SelectedIndex = TabPages.SelectedIndex;
        }

        private void ShowAllPositionsCheck_CheckedChanged(object sender, EventArgs e)
        {
            _Actions.ShowAllPositionens();
        }

        private void PositionSave_Click(object sender, EventArgs e)
        {
            _Actions.PositionSave();
        }

        private void KeySaveInList_Click(object sender, EventArgs e)
        {
            _Actions.KeySaveInAction();
        }

        private void PositionClear_Click(object sender, EventArgs e)
        {
            _Actions.ClearSavedActions();
        }

        private void PositionRemove_Click(object sender, EventArgs e)
        {
            _Actions.RemoveSelectedActions();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _Actions.RemoveSelectedActions();
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _Actions.EditActionsSelected();
        }

        private void CurserPositionList_SelectedIndexChanged(object sender, EventArgs e)
        {
            _Actions.SelectedAction();
        }

        private void WaitAddButton_Click(object sender, EventArgs e)
        {
            _Actions.AddWaitTime();
        }

        private void SideBarOC_Click(object sender, EventArgs e)
        {
            _SideForm.btnRefreshWindows();
            _SideForm.reloadCheckedApps();
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
            var result = _ColorClick.SelectRectangle();

            if (!result.Area.IsEmpty)
            {
                // Screenshot in der PictureBox anzeigen
                //ColorScreenshotArea.Image = result.Screenshot;

                // Falls du die Koordinaten brauchst:
                Setinfotextfast($"Area Saved: {result.Area}");
                _ColorClick.scanArea = result.Area;
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
                Form marker = _ColorClick.ShowAreaMarker(_ColorClick.scanArea);
                _ColorClick.ActiveMarker = marker;
            }
            else
            {
                _ColorClick.ActiveMarker?.Close();
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
                _ColorClick.scanArea = bounds;
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
                    string hex = $"Hex:#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";
                    Setinfotextfast($"Gewählte Farbe: {selectedColor} {hex}");
                    LabelColorPicked.Text = ($"{selectedColor} {hex}");
                }
            }
        }

        private void ColorPickFromScreen_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            var picker = new ColorClickerFunc.ColorPicker();
            Color picked = picker.Show();
            ColorSetColor.BackColor = picked;
            string hex = $"Hex:#{picked.R:X2}{picked.G:X2}{picked.B:X2}";
            Setinfotextfast($"Gewählte Farbe: {picked} {hex}");
            LabelColorPicked.Text = ($"{picked} {hex}");
            this.WindowState = FormWindowState.Normal;
        }



        private void Color_SelectActionsbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (Color_SelectActionsbox.SelectedIndex)
            {
                case 0:
                    Color_Clickakey_setkey.Visible = false;
                    Color_Clickakey_setkey_Label.Visible = false;
                    break;
                case 1:
                    Color_Clickakey_setkey.Visible = true;
                    Color_Clickakey_setkey_Label.Visible = true;
                    Color_Clickakey_setkey.Text = "Set Key";
                    Color_Clickakey_setkey_Label.Text = "Key: " + _ColorClick.ClickaKey_Key.ToString();
                    break;
                case 2:
                    Color_Clickakey_setkey.Visible = true;
                    Color_Clickakey_setkey_Label.Visible = true;
                    Color_Clickakey_setkey.Text = "Set Position";
                    Color_Clickakey_setkey_Label.Text = $"X:{_ColorClick.ClickPosition.X}, Y:{_ColorClick.ClickPosition.Y} <{_ColorClick.ClickPos_Key.ToString()}>";
                    break;
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

            SetWindowDisplayAffinity(Handle, WDA_EXCLUDEFROMCAPTURE);

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
        const uint WDA_NONE = 0x0;
        const uint WDA_EXCLUDEFROMCAPTURE = 0x11;

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
        [DllImport("user32.dll")]
        static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

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

    public class ActionData
    {
        public ActionType Type { get; set; }

        public int X { get; set; }
        public int Y { get; set; }

        public long HoldClickMS { get; set; }
        public Keys? Mousepress { get; set; }

        public Keys? Key { get; set; }

        public long ToWait { get; set; }
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
