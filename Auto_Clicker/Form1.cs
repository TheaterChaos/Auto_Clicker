using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsInput;



namespace Auto_Clicker
{

    public partial class Form1 : Form
    {
        string[] AllowedKeyboardList =
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
            "Left",
            "Right",
            "Up",
            "Down",
            "Enter",
            "Return",
            "CapsLock",
            "Space",
            "Back",

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
            "Oem5",
            "OemQuestion"
        ];

        Dictionary<string, VirtualKeyCode> keyMap = new Dictionary<string, VirtualKeyCode>
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
        };

        string[] AllowedMouseList =
        {
            "LButton",    // Linksklick
            "RButton",   // Rechtsklick
            "MButton",     // Mausrad-Klick
            "XButton1",   // Zusätzliche Taste 1 (z. B. Daumentaste)
            "XButton2"    // Zusätzliche Taste 2
        };

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

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




        /*private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;*/
        private const int MOUSEEVENTF_XDOWN = 0x0080;
        private const int MOUSEEVENTF_XUP = 0x0100;
        private const int XBUTTON1 = 0x0001;
        private const int XBUTTON2 = 0x0002;


        private bool clicking = false;
        private Keys hotkey = Keys.F6;
        private Keys clickKey = Keys.LButton; // Standard: Linksklick
        private bool keyWasDown = false;

        private List<ClickOrKeyAction> SavedActions = new List<ClickOrKeyAction>();

        private List<Form> activeMarkers = new();

        private int clickIndex = 0;


        private System.Windows.Forms.Timer hotkeyTimer;

        private void SaveSettings()
        {
            Properties.Settings.Default.UseMouseMode = UseMouse.Checked;

            Properties.Settings.Default.Hotkey = hotkey.ToString();
            Properties.Settings.Default.HoldOrSwitch = HoldToClick.Checked ? "hold" : "switch";
            Properties.Settings.Default.PositionButton = PositionIsChecked.Checked;

            Properties.Settings.Default.Clickkey = clickKey.ToString();

            Properties.Settings.Default.ClickMode = ClicksPersSecButton.Checked ? "cps" : "time";
            Properties.Settings.Default.ClicksPerSec = ClickPerSecNum.Value;
            Properties.Settings.Default.ClickTimeValue = PerTimeNum.Value;
            Properties.Settings.Default.ClickTimeUnit = PerTimeValue.SelectedItem?.ToString() ?? "ms";

            Properties.Settings.Default.RepeatInfinite = RepeatUnlimited.Checked;
            Properties.Settings.Default.RepeatCount = RepeatTimes.Value;

            var parts = SavedActions.Select(action =>
            {
                if (action.Type == ActionType.MouseClick)
                    return $"M:{action.MousePosition.X}:{action.MousePosition.Y}";
                else if (action.Type == ActionType.KeyPress && action.Key.HasValue)
                    return $"K:{action.Key}";
                return null;
            }).Where(s => s != null);
            Properties.Settings.Default.SavedPoints = string.Join(";", parts);

            Properties.Settings.Default.ShowPointCLick = ShowPointOnClick.Checked;

            Properties.Settings.Default.SaveOnExit = SettingsSaveonexit.Checked;
            Properties.Settings.Default.SetOnTop = setTopMostMenu.Checked;
            Properties.Settings.Default.DisableWindowOnPosition = disableWindowOnPositionMenu.Checked;
            Properties.Settings.Default.DisableredBox = disableRedBoxMenu.Checked;

            Properties.Settings.Default.Save();
        }


        private void LoadSettings()
        {
            if (Properties.Settings.Default.UseMouseMode)
            {
                UseMouse.Checked = true;
            }
            else
            {
                UseKeyboard.Checked = true;
            }

            if (Properties.Settings.Default.PositionButton)
            {
                PositionIsChecked.Checked = true;
            }
            else
            {
                PositionIsChecked.Checked = false;
            }

            if (Properties.Settings.Default.ShowPointCLick)
            {
                ShowPointOnClick.Checked = true;
            }
            else
            {
                ShowPointOnClick.Checked = false;
            }
            if (Properties.Settings.Default.SetOnTop)
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
            if (Properties.Settings.Default.DisableWindowOnPosition)
            {
                disableWindowOnPositionMenu.Checked = true;
                disableWindowOnPositionMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
            else
            {
                disableWindowOnPositionMenu.Checked = false;
                disableWindowOnPositionMenu.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }
            if (Properties.Settings.Default.DisableredBox)
            {
                disableRedBoxMenu.Checked = true;
                disableRedBoxMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
            else
            {
                disableRedBoxMenu.Checked = false;
                disableRedBoxMenu.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }
            if (Properties.Settings.Default.SaveOnExit)
            {
                SettingsSaveonexit.Checked = true;
                SettingsSaveonexit.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
            else
            {
                SettingsSaveonexit.Checked = false;
                SettingsSaveonexit.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.Hotkey))
            {
                hotkey = (Keys)Enum.Parse(typeof(Keys), Properties.Settings.Default.Hotkey);
                Hotkeypressvalue.SelectedItem = hotkey.ToString();
            }

            if (Properties.Settings.Default.HoldOrSwitch == "hold")
            {
                HoldToClick.Checked = true;
            }
            else
            {
                SwitchToClick.Checked = true;
            }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.Clickkey))
            {
                clickKey = (Keys)Enum.Parse(typeof(Keys), Properties.Settings.Default.Clickkey);
                KeyToPress.SelectedItem = clickKey.ToString();
            }

            if (Properties.Settings.Default.ClicksPerSec > 0)
            {
                ClickPerSecNum.Value = Properties.Settings.Default.ClicksPerSec;
            }

            if (Properties.Settings.Default.ClickMode == "cps")
            {
                ClicksPersSecButton.Checked = true;
            }
            else
            {
                PerTimeButton.Checked = true;
            }

            if (Properties.Settings.Default.RepeatInfinite)
            {
                RepeatUnlimited.Checked = true;
            }
            else
            {
                RepeatRepeat.Checked = true;
            }

            PerTimeNum.Value = Properties.Settings.Default.ClickTimeValue;
            if (PerTimeValue.Items.Contains(Properties.Settings.Default.ClickTimeUnit))
                PerTimeValue.SelectedItem = Properties.Settings.Default.ClickTimeUnit;

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
            UpdateActionList();
        }

        private void ResetSettings()
        {
            Properties.Settings.Default.Reset();  // Setzt auf Standardwerte zurück
            Properties.Settings.Default.Save();
            LoadSettings();         // Lade die nun zurückgesetzten Werte
            SavedActions.Clear(); // RAM-liste leeren
            UpdateActionList();   // UI aktualisieren

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

        private void SaveKeyPress(Keys key)
        {
            SavedActions.Add(new ClickOrKeyAction
            {
                Type = ActionType.KeyPress,
                Key = key
            });

            UpdateActionList();
        }

        private void ClearSavedPositions()
        {
            SavedActions.Clear();
            CurserPositionList.Items.Clear();
            InfoLabel.Text = ("All saved Actions deleted.");
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
                {
                    e.Graphics.FillEllipse(pointBrush, 0, 0, size - 1, size - 1);
                }

                // Text vorbereiten
                string text = $"{Posindex}. X={pos.X} Y={pos.Y}";
                using (Font font = new Font("Segoe UI", 10))
                {
                    SizeF textSize = e.Graphics.MeasureString(text, font);
                    RectangleF textRect = new RectangleF(size + 2, 0, textSize.Width + 4, textSize.Height);

                    // Hintergrund (halbtransparent schwarz)
                    using (Brush bgBrush = new SolidBrush(Color.FromArgb(60, 60, 60))) // dunkelgrau
                    {
                        e.Graphics.FillRectangle(bgBrush, textRect);
                    }

                    // Text in Weiß
                    using (Brush textBrush = new SolidBrush(Color.White))
                    {
                        e.Graphics.DrawString(text, font, textBrush, size + 4, 0);
                    }
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
                if (keyMap.TryGetValue(keyName, out VirtualKeyCode vk))
                {
                    new InputSimulator().Keyboard
                        .KeyPress(vk);
                }
                //SendKeys.SendWait(clickKey.ToString());
            }
        }

        private void StartClicking()
        {
            clicking = true;

            if (ClicksPersSecButton.Checked)
            {
                decimal cps = ClickPerSecNum.Value;
                double secondsPerClick = 1.0 / (double)cps;
                double intervalMs = 1000.0 / (double)cps;

                Task.Run(() =>
                {
                    Stopwatch sw = new Stopwatch();

                    while (clicking)
                    {
                        sw.Restart();
                        if (SavedActions.Count != 0 && PositionIsChecked.Checked)
                        {

                            var action = SavedActions[clickIndex];
                            clickIndex = (clickIndex + 1) % SavedActions.Count;

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
                                if (keyMap.TryGetValue(keyName, out VirtualKeyCode vk))
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

                        // Schlafen, um ungefähr das Zielintervall zu treffen
                        int sleepTime = (int)Math.Max(0, intervalMs - 1);
                        Thread.Sleep(sleepTime);

                        // Feinjustierung durch aktives Warten (wenige µs)
                        while (sw.Elapsed.TotalMilliseconds < intervalMs)
                        {
                            Thread.SpinWait(10); // sehr kleine Warteinheiten
                        }
                    }
                });


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

                    while ((infinite || clickCount < repeatCount) && clicking)
                    {
                        sw.Restart();
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
                                        if (keyMap.TryGetValue(keyName, out VirtualKeyCode vk))
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

                        int sleepTime = (int)Math.Max(0, intervalMs - 1);
                        Thread.Sleep(sleepTime);

                        while (sw.Elapsed.TotalMilliseconds < intervalMs)
                        {
                            Thread.SpinWait(10);
                        }
                    }
                    Invoke(new Action(() =>
                    {
                        if (RepeatRepeat.Checked)
                        {
                            InfoLabel.Text = "Autoclicker Stopped.....";
                            StopClicking();
                        }
                    }));
                });
            }
        }

        private void StopClicking()
        {
            clicking = false;
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
                    if (AllowedKeyboardList.Contains(name))
                        KeyToPress.Items.Add(name);
                }
                KeyToPress.SelectedItem = Keys.A.ToString();
                clickKey = Keys.A;
            }
        }

        public Form1()
        {
            InitializeComponent();

            hotkeyTimer = new System.Windows.Forms.Timer();
            hotkeyTimer.Interval = 50;
            hotkeyTimer.Tick += HotkeyTimer_Tick;
            hotkeyTimer.Start();

            this.KeyPreview = true;
            //this.KeyDown += Form1_KeyDown;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (var name in Enum.GetNames(typeof(Keys)))
            {
                if (AllowedMouseList.Contains(name) && (name != "LButton" && name != "RButton") || AllowedKeyboardList.Contains(name))
                    Hotkeypressvalue.Items.Add(name);
            }
            AddKeysToPress(true);

            PerTimeValue.Items.Add("ms");
            PerTimeValue.Items.Add("s");
            PerTimeValue.Items.Add("m");
            PerTimeValue.Items.Add("h");
            PerTimeValue.SelectedItem = "ms";

            Hotkeypressvalue.SelectedItem = hotkey.ToString();
            KeyToPress.SelectedItem = clickKey.ToString();
            PerTimeValue.SelectedItem = "ms";

            // Lade die Einstellungen
            LoadSettings();

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SettingsSaveonexit.Checked)
            {
                SaveSettings();
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
                        else if (Keypressed && detectedKey == hotkey && !isHotkey)
                        {
                            ShowError = true;
                            ErrortoShow = $"Not a Valid Key (Same as Hotkey):/ {key} / Try again after it get back Orange\nBack in: ";
                        }
                        else if (Mousefind && Keypressed && AllowedMouseList.Contains(key.ToString())) // Taste ist gedrückt
                        {
                            if (isHotkey && (key.ToString() == "LButton" || key.ToString() == "RButton"))
                            {
                                ShowError = true;
                                ErrortoShow = $"Not a Valid Key (Hotkey diasabled keys: LButton, RButton):/ {key} / Try again after it get back Orange\nBack in: ";
                            }
                            else if (isKeypress && !UseMouse.Checked)
                            {
                                ShowError = true;
                                ErrortoShow = $"Not a Valid Key (You have to Check Use Mouse):/ {key} / Try again after it get back Orange\nBack in: ";
                            }
                            else
                            {
                                detectedKey = key;
                                break;
                            }
                        }
                        else if (keyboardfind && Keypressed && AllowedKeyboardList.Contains(key.ToString())) // Taste ist gedrückt
                        {
                            if (keyboardfind && isKeypress && !UseKeyboard.Checked)
                            {
                                ShowError = true;
                                ErrortoShow = $"Not a Valid Key (You have to Check Use Keyboard):/ {key} / Try again after it get back Orange\nBack in: ";
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
                            ErrortoShow = $"Not a Valid Key:/ {key} / Try again after it get back Orange\nBack in: ";
                        }
                    }
                    overlay.Focus();
                    if (ShowError)
                    {
                        overlay.BackColor = Color.Red;
                        //overlay.SetMessage($"Not a Valid Key: {detectedKey} Try again after it get back Orange\nBack in: 3");
                        overlay.SetMessage(ErrortoShow + TimerTogoback);
                        overlay.Refresh();
                        while (TimerTogoback > 0)
                        {
                            //overlay.SetMessage($"Not a Valid Key: {detectedKey} Try again after it get back Orange\nBack in: {TimerTogoback}");
                            overlay.SetMessage(ErrortoShow + TimerTogoback);
                            overlay.Refresh();
                            TimerTogoback--;
                            Thread.Sleep(1000);
                        }
                        overlay.BackColor = Color.Orange;
                        overlay.SetMessage("Press a Valid key.\nPRESS:  ESC  to cancle this procces");
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
                    Thread.Sleep(1000); // Kurze Pause, um die Anzeige zu sehen
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
                    InfoLabel.Text = "Autoclicker running.....";
                    StartClicking();
                }
                else if (!keyDown && clicking)
                {
                    InfoLabel.Text = "Autoclicker Stopped.....";
                    StopClicking();
                }
            }
            else if (SwitchToClick.Checked)
            {
                if (keyDown && !keyWasDown)
                {
                    if (!clicking)
                    {
                        InfoLabel.Text = "Autoclicker running.....";
                        StartClicking();
                    }
                    else
                    {
                        InfoLabel.Text = "Autoclicker Stopped.....";
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
            ClickRepeatgroup.Visible = false;
            PerTimeNum.Enabled = false;
            PerTimeValue.Enabled = false;
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            PerTimeNum.Enabled = true;
            PerTimeValue.Enabled = true;
            ClickRepeatgroup.Visible = true;
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

        private void button2_Click(object sender, EventArgs e)
        {

            this.WindowState = FormWindowState.Minimized;

            Keys detectedKey = Keys.None;

            while (detectedKey == Keys.None)
            {
                if ((GetAsyncKeyState(Keys.LButton) & 0x8000) != 0)
                {
                    detectedKey = Keys.LButton;
                }
                Thread.Sleep(50); // Kurze Pause, um CPU-Last zu reduzieren
            }

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
                    MessageBox.Show($"Position Saved: X={selectedPos.X}, Y={selectedPos.Y}");
                }
            }

        }

        private void PositionClear_Click(object sender, EventArgs e)
        {
            ClearSavedPositions();
        }

        private void PositionRemove_Click(object sender, EventArgs e)
        {
            RemoveSelectedPosition();
        }

        private void CurserPositionList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ShowPointOnClick.Checked && CurserPositionList.SelectedItem != null && !CurserPositionList.SelectedItem.ToString().Contains("Key"))
            {
                int indexpos = CurserPositionList.SelectedIndex;
                Point pos = SavedActions[indexpos].MousePosition;
                Debug.WriteLine("Selected: " + pos.X + " " + pos.Y);
                ShowPositionMarker(pos);
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
            if (SettingsSaveonexit.Checked)
            {
                SettingsSaveonexit.Checked = false;
                SettingsSaveonexit.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }
            else
            {
                SettingsSaveonexit.Checked = true;
                SettingsSaveonexit.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
        }

        private void saveToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            SaveSettings();
            InfoLabel.Text = "Settings saved.";
        }

        private void disableWindowOnPositionMenu_Click(object sender, EventArgs e)
        {
            if (disableWindowOnPositionMenu.Checked)
            {
                disableWindowOnPositionMenu.Checked = false;
                disableWindowOnPositionMenu.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }
            else
            {
                disableWindowOnPositionMenu.Checked = true;
                disableWindowOnPositionMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
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
        }

        private void disableRedBoxMenu_Click(object sender, EventArgs e)
        {
            if (disableRedBoxMenu.Checked)
            {
                disableRedBoxMenu.Checked = false;
                disableRedBoxMenu.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            }
            else
            {
                disableRedBoxMenu.Checked = true;
                disableRedBoxMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            }
        }

        private void PositionIsChecked_CheckedChanged(object sender, EventArgs e)
        {
            if (PositionIsChecked.Checked)
            {
                LabelUsingActions.Visible = true;
            }
            else
            {
                LabelUsingActions.Visible = false;
            }
        }

        private void ShowAllPositionsCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (ShowAllPositionsCheck.Checked)
            {

            }
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


            // Fenstergröße auf 50% des Bildschirms setzen
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
            messageLabel.Text = "Press a Valid key.\nPRESS: ESC   to cancle this procces";
            messageLabel.Font = new Font("Segoe UI", 20, FontStyle.Regular);
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

}
