using Microsoft.VisualBasic;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Pkcs;
using System.Windows.Forms;
using WindowsInput;

namespace Auto_Clicker
{
    public class ActionsFunc
    {
        private Form1 _Main;


        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);


        public List<ClickOrKeyAction> SavedActions = new List<ClickOrKeyAction>();

        private List<MarkerHandle> activeMarkers = new List<MarkerHandle>();

        public struct TimeParts
        {
            public long Milliseconds;
            public long Seconds;
            public long Minutes;
            public long Hours;
        }


        public ActionsFunc(Form1 form)
        {
            _Main = form;
        }



        public (Point, Keys, long, bool) SavePositionInList_Click(bool Edit = false)
        {
            _Main.hotkeyTimer.Stop();
            _Main.WindowState = FormWindowState.Minimized;

            Point selectedPos = Point.Empty;
            Keys detectedKey = Keys.None;

            bool setanother = false;

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

            Keys[] mouseKeys = new Keys[]
            {
                Keys.LButton,
                Keys.RButton,
                Keys.MButton,
                Keys.XButton1,
                Keys.XButton2
            };

            Stopwatch sw = new Stopwatch();

            while (detectedKey == Keys.None)
            {
                foreach (var key in mouseKeys)
                {
                    if ((GetAsyncKeyState(key) & 0x8000) != 0)
                    {
                        detectedKey = key;
                        break;
                    }
                }

                Point cursorPos = Cursor.Position;

                // Marker neu positionieren (neben dem Cursor z. B. +20px)
                marker.Location = new Point(cursorPos.X + 10, cursorPos.Y + 10);
                coordLabel.Text = $"X={cursorPos.X}, Y={cursorPos.Y}";

                Application.DoEvents();
                Thread.Sleep(30); // Kurze Pause, um CPU-Last zu reduzieren
            }
            sw.Restart();
            if (_Main.CheckAddHold.Checked)
            {
                while ((GetAsyncKeyState(detectedKey) & 0x8000) != 0)
                {
                    if (sw.ElapsedMilliseconds > 50)
                    {
                        coordLabel.Text = $"Rec: " + Ms_to_PartsString(sw.ElapsedMilliseconds);
                        Application.DoEvents();
                    }
                    Thread.Sleep(10); // Warten, bis die Taste losgelassen wird
                }
            }
            sw.Stop();
            long washolding = sw.ElapsedMilliseconds;

            marker.Close();

            if (detectedKey != Keys.None)
            {
                selectedPos = Cursor.Position;
                //SaveMouseClick(selectedPos, detectedKey);
                string infotextmessage = "";
                if (washolding > 50)
                {
                    infotextmessage = $"Position Saved: X={selectedPos.X}, Y={selectedPos.Y} <{detectedKey.ToString()}> Hold({washolding})";
                }
                else
                {
                    washolding = 0;
                    infotextmessage = $"Position Saved: X={selectedPos.X}, Y={selectedPos.Y} <{detectedKey.ToString()}>";
                }

                if (_Main.disableWindowOnPositionMenu.Checked || Edit)
                {
                    _Main.Setinfotextfast(infotextmessage + " (Window Disabled)");
                }
                else
                {
                    //MessageBox.Show($"Position Saved: X={selectedPos.X}, Y={selectedPos.Y}");
                    var (dialogResult, another) = ShowCustomMessage(infotextmessage);
                    _Main.Setinfotextfast(infotextmessage);

                    if (another)
                    {
                        setanother = true;
                    }
                }
            }

            while ((GetAsyncKeyState(detectedKey) & 0x8000) != 0)
            {
                Thread.Sleep(10); // Warten, bis die Taste losgelassen wird
            }
            _Main.WindowState = FormWindowState.Normal;
            _Main.Activate(); // In den Vordergrund holen
            _Main.hotkeyTimer.Start();
            return (selectedPos, detectedKey, washolding, setanother);
        }

        public static (DialogResult result, bool anotherClicked) ShowCustomMessage(string message)
        {
            bool anotherClicked = false;

            Form form = new Form
            {
                Text = "Info",
                Size = new Size(300, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
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

        public void Set_Hold_MessageFunc(int index)
        {
            Form form = new Form
            {
                Text = "Set Hold or Remove it",
                Size = new Size(300, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                TopMost = true
            };

            Label lbl = new Label
            {
                Text = "Set the Hold amount in Ms\neverything under 50ms\nwill remove Hold",
                AutoSize = false,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Size = new Size(260, 60),
                Location = new Point(20, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            NumericUpDown Holdtimems = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 1000000,
                Value = 100,
                Location = new Point(100, 65),
                Size = new Size(100, 25)
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
                Location = new Point(160, 95),
                Size = new Size(80, 30)
            };

            DoneButton.Click += (s, e) =>
            {
                var a = SavedActions[index];
                ActionType type = a.Type;
                Point pos = a.MousePosition;
                Keys? press = a.Mousepress;
                Keys? Keypress = a.Key;
                long holding = 0;
                if (Holdtimems.Value > 50)
                {
                    holding = (long)Holdtimems.Value;
                }

                if (type == ActionType.MouseClick)
                {
                    SavedActions.RemoveAt(index);
                    SavedActions.Insert(index, new ClickOrKeyAction
                    {
                        Type = ActionType.MouseClick,
                        MousePosition = pos,
                        Mousepress = press,
                        HoldClickMS = holding
                    });
                }
                else
                {
                    SavedActions.RemoveAt(index);
                    SavedActions.Insert(index, new ClickOrKeyAction
                    {
                        Type = ActionType.KeyPress,
                        Key = Keypress,
                        HoldKey = a.HoldKey,
                        HoldClickMS = holding
                    });
                }
                UpdateActionList();
                form.Close();
            };

            CancleButton.Click += (s, e) =>
            {
                form.Close();
            };

            form.Controls.Add(lbl);
            form.Controls.Add(Holdtimems);
            form.Controls.Add(DoneButton);
            form.Controls.Add(CancleButton);

            form.AcceptButton = DoneButton;

            var result = form.ShowDialog();
        }

        public void Move_To_PositionMessage(int index)
        {
            Form form = new Form
            {
                Text = "Move To Window",
                Size = new Size(300, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                TopMost = true
            };

            Label lbl = new Label
            {
                Text = "Set the Position you want it to be",
                AutoSize = false,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Size = new Size(260, 60),
                Location = new Point(20, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            NumericUpDown Positiontoset = new NumericUpDown
            {
                Minimum = 1,
                Maximum = _Main.CurserPositionList.Items.Count,
                Value = 1,
                Location = new Point(100, 65),
                Size = new Size(100, 25)
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
                Location = new Point(160, 95),
                Size = new Size(80, 30)
            };

            DoneButton.Click += (s, e) =>
            {
                var item = SavedActions[index];
                SavedActions.RemoveAt(index);
                SavedActions.Insert((int)Positiontoset.Value - 1, item);
                UpdateActionList();
                form.Close();
            };

            CancleButton.Click += (s, e) =>
            {
                form.Close();
            };

            form.Controls.Add(lbl);
            form.Controls.Add(Positiontoset);
            form.Controls.Add(DoneButton);
            form.Controls.Add(CancleButton);

            form.AcceptButton = DoneButton;

            var result = form.ShowDialog();
        }

        public bool CheckappWB()
        {
            var (proc, _) = _Main._SideForm.GetActiveProcessName();
            return _Main.WhitelistappsCheck.Checked && _Main._SideForm.AppsCheckedlist.Contains(proc) || !_Main.WhitelistappsCheck.Checked && !_Main._SideForm.AppsCheckedlist.Contains(proc);
        }


        public void StartClickingAction()
        {
            lock (_Main.clickLock)
            {
                if (_Main.clicking) return;
                _Main.clicking = true;
                _Main.clickCts = new CancellationTokenSource();
            }

            if (_Main.ShowHideMenu.Checked)
            {
                if (_Main.Clickoverlay == null || _Main.Clickoverlay.IsDisposed)
                {
                    _Main.Clickoverlay = new CursorOverlayForm();
                    _Main.Clickoverlay.Show();
                }
            }

            CancellationToken token = _Main.clickCts.Token;

            bool switchinfotext = false;
            _Main.clickIndex = 0;
            var TextActionShow = "";

            int repeatCount = (int)_Main.ActionRepeatTimes.Value; // wie oft klicken

            long waitTime = 0;

            // Klick-Intervall berechnen
            bool infinity = _Main.ActionRepeatTimes.Value == 0;
            int clickCount = 0;
            string infoworking = "";

            Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();

                while ((infinity || clickCount < repeatCount) && !token.IsCancellationRequested)
                {
                    if (CheckappWB())
                    {
                        clickCount++;
                        if (switchinfotext)
                            _Main.Setinfotextfast("Auto clicker running.....", true);
                        switchinfotext = false;

                        int i = 0;
                        foreach (var action in SavedActions)
                        {
                            while (!CheckappWB())
                            {
                                if (!_Main.clicking || token.IsCancellationRequested)
                                    break;
                                if (!switchinfotext)
                                    _Main.Setinfotextfast("Auto clicker ON: Waiting for None Blacklisted window.......", true);
                                switchinfotext = true;
                                Thread.Sleep(200);
                            }
                            i++;
                            sw.Restart();
                            if (!_Main.clicking || token.IsCancellationRequested)
                                break;

                            waitTime = 0;
                            if (Screen.PrimaryScreen != null)
                            {
                                if (action.Type == ActionType.MouseClick)
                                {
                                    int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                                    int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                                    double absoluteX = action.MousePosition.X * 65535.0 / (screenWidth - 1);
                                    double absoluteY = action.MousePosition.Y * 65535.0 / (screenHeight - 1);

                                    new InputSimulator().Mouse.MoveMouseTo(absoluteX, absoluteY);

                                    if (action.Mousepress != null && action.Mousepress.Value != Keys.Modifiers)
                                    {
                                        infoworking = $"Auto clicker ON   Count: {clickCount} Working on: {i} <{action.Mousepress.Value.ToString()}> Hold: ";
                                        _Main.Setinfotextfast(infoworking, true);
                                        if (action.Mousepress.Value != _Main.hotkey)
                                        {
                                            if (action.HoldClickMS > 0)
                                            {
                                                sw.Stop();
                                                _Main.DoClick(action.Mousepress.Value, action.HoldClickMS, infoworking);
                                                sw.Start();
                                            }
                                            else
                                            {
                                                _Main.DoClick(action.Mousepress.Value);
                                            }
                                            TextActionShow = action.Mousepress.Value.ToString();
                                        }
                                    }
                                    //new InputSimulator().Mouse
                                    //    .LeftButtonClick();
                                }
                                else if (action.Type == ActionType.HoldAndPress && action.Key.HasValue)
                                {
                                    infoworking = $"Auto clicker ON   Count: {clickCount} Working on: {i} Hold: <{action.HoldKey.Value}> + Press: <{action.Key.Value}> ";
                                    _Main.Setinfotextfast(infoworking, true);

                                    var input = new InputSimulator();
                                    bool holdKeyboard = DataStings.keyMap.TryGetValue(action.HoldKey.Value.ToString(), out VirtualKeyCode holdVk);
                                    bool holdMouse = DataStings.AllowedMouseList.Contains(action.HoldKey.Value.ToString());

                                    Stopwatch holdandpresstimer = new Stopwatch();

                                    if (holdKeyboard)
                                    {
                                        input.Keyboard.KeyDown(holdVk);
                                    }
                                    else if (holdMouse)
                                    {
                                        if (action.HoldKey.Value == Keys.LButton)
                                            input.Mouse.LeftButtonDown();
                                        else if (action.HoldKey.Value == Keys.RButton)
                                            input.Mouse.RightButtonDown();
                                        else if (action.HoldKey.Value == Keys.MButton)
                                            input.Mouse.MiddleButtonDown();
                                        else if (action.HoldKey.Value == Keys.XButton1)
                                            input.Mouse.XButtonDown(0x0001);
                                        else if (action.HoldKey.Value == Keys.XButton2)
                                            input.Mouse.XButtonDown(0x0002);
                                    }

                                    holdandpresstimer.Restart();

                                    try
                                    {
                                        while (holdandpresstimer.ElapsedMilliseconds < action.HoldClickMS / 2)
                                        {
                                        }

                                        sw.Stop();
                                        _Main.DoClick(action.Key.Value);
                                        sw.Start();
                                    }
                                    finally
                                    {
                                        while (holdandpresstimer.ElapsedMilliseconds < action.HoldClickMS)
                                        {
                                        }

                                        if (holdKeyboard)
                                        {
                                            input.Keyboard.KeyUp(holdVk);
                                        }
                                        else if (holdMouse)
                                        {
                                            if (action.HoldKey.Value == Keys.LButton)
                                                input.Mouse.LeftButtonUp();
                                            else if (action.HoldKey.Value == Keys.RButton)
                                                input.Mouse.RightButtonUp();
                                            else if (action.HoldKey.Value == Keys.MButton)
                                                input.Mouse.MiddleButtonUp();
                                            else if (action.HoldKey.Value == Keys.XButton1)
                                                input.Mouse.XButtonUp(0x0001);
                                            else if (action.HoldKey.Value == Keys.XButton2)
                                                input.Mouse.XButtonUp(0x0002);
                                        }
                                    }
                                    TextActionShow = action.Key.Value.ToString();
                                }
                                else if (action.Type == ActionType.KeyPress && action.Key.HasValue)
                                {
                                    infoworking = $"Auto clicker ON   Count: {clickCount} Working on: {i} <{action.Key.Value.ToString()}> Hold: ";
                                    _Main.Setinfotextfast(infoworking, true);
                                    string keyName = action.Key.Value.ToString();
                                    if (DataStings.keyMap.ContainsKey(keyName) || DataStings.AllowedMouseList.Contains(keyName))
                                    {
                                        if (action.HoldClickMS > 0)
                                        {
                                            sw.Stop();
                                            _Main.DoClick(action.Key.Value, action.HoldClickMS, infoworking);
                                            sw.Start();
                                        }
                                        else
                                        {
                                            _Main.DoClick(action.Key.Value);
                                        }
                                        TextActionShow = action.Key.Value.ToString();
                                    }
                                }
                                else if (action.Type == ActionType.Waittime && action.ToWait > 0)
                                {
                                    waitTime = action.ToWait;
                                    //TextActionShow = $"Wait {waitTime}ms";
                                    sw.Restart();
                                }
                            }
                            String Texttoshow = $"Auto clicker ON   Count: {clickCount}";
                            long timetowait = 0;
                            if (waitTime > 0)
                            {
                                timetowait = (long)waitTime;
                            }
                            if (_Main.IgnoreWaitCheck.Checked && waitTime == 0)
                            {
                                timetowait = (long)_Main.TimeBetweenAction.Value;
                            }
                            else if (!_Main.IgnoreWaitCheck.Checked)
                            {
                                timetowait += (long)_Main.TimeBetweenAction.Value;
                            }
                            if (timetowait < 5)
                            {
                                timetowait = 5;
                            }
                            long updateInterval = timetowait >= 100 ? 100 : timetowait;
                            long nextUpdate = 0;


                            while (sw.ElapsedMilliseconds < timetowait)
                            {
                                if (!_Main.clicking || token.IsCancellationRequested)
                                    break;

                                long elapsed = sw.ElapsedMilliseconds;
                                long remaining = timetowait - elapsed;

                                if (elapsed >= nextUpdate && timetowait > 100)
                                {
                                    string Timestring;

                                    if (remaining > 1000)
                                    {
                                        // ⏱ über 1 Sekunde → Sekunden-Anzeige
                                        long seconds = remaining / 1000;
                                        Timestring = $"{seconds}s";
                                    }
                                    else
                                    {
                                        // 🔢 auf 100ms runden
                                        long roundedMs = (remaining / 100) * 100;

                                        // optional: nie 0 anzeigen, solange noch gewartet wird
                                        if (roundedMs == 0 && remaining > 0)
                                            roundedMs = 100;

                                        Timestring = $"{roundedMs}ms";
                                    }

                                    _Main.Setinfotextfast(
                                        $"{Texttoshow} Last Actions {i} <{TextActionShow}>. Waiting: {Timestring}",
                                        true
                                    );

                                    nextUpdate += updateInterval;
                                }
                                //Debug.WriteLine($"{remaining}  {timetowait}  {nextUpdate}");
                                if (remaining > 2)
                                    Thread.Sleep(1);
                                else
                                    Thread.SpinWait(5);
                            }
                        }
                    }
                    else
                    {
                        if (!switchinfotext)
                            _Main.Setinfotextfast("Auto clicker ON: Waiting for None Blacklisted window.......", true);
                        switchinfotext = true;
                    }
                }
                if (!infinity)
                {
                    _Main.BeginInvoke(new Action(_Main.StopClicking));
                    return;
                }
            }, token);
        }


        public void SaveMouseClick(Point pos, Keys Pressed = Keys.None, long holding = 0)
        {
            if (pos == Point.Empty)
                pos = Cursor.Position;

            SavedActions.Add(new ClickOrKeyAction
            {
                Type = ActionType.MouseClick,
                MousePosition = pos,
                Mousepress = Pressed,
                HoldClickMS = _Main.CheckAddHold.Checked ? holding : 0
            });

            UpdateActionList();
        }

        public void SaveKeyPress(Keys key, long holding)
        {
            SavedActions.Add(new ClickOrKeyAction
            {
                Type = ActionType.KeyPress,
                Key = key,
                HoldClickMS = _Main.CheckAddHold.Checked ? holding : 0
            });

            UpdateActionList();
        }

        public void SaveWaitTime(long time)
        {
            SavedActions.Add(new ClickOrKeyAction
            {
                Type = ActionType.Waittime,
                ToWait = time
            });

            UpdateActionList();
        }

        public void AddWaitTime()
        {
            if (_Main.WaitTimeMs.Value == 0 && _Main.WaitTimeSec.Value == 0 && _Main.WaitTimeMin.Value == 0 && _Main.WaitTimeHour.Value == 0)
            {
                _Main.Setinfotextfast("No time to add Everything: 0");
                return;
            }

            TimeParts got = new TimeParts();
            got.Milliseconds = (long)_Main.WaitTimeMs.Value;
            got.Seconds = (long)_Main.WaitTimeSec.Value;
            got.Minutes = (long)_Main.WaitTimeMin.Value;
            got.Hours = (long)_Main.WaitTimeHour.Value;
            long totalMs = PartsToMs(got);
            SaveWaitTime(totalMs);
            _Main.Setinfotextfast("Wait Added: " + Ms_to_PartsString(totalMs));
        }

        public TimeParts MsToParts(long ms)
        {
            TimeParts t = new TimeParts();
            t.Milliseconds = ms % 1000;
            t.Seconds = (ms / 1000) % 60;
            t.Minutes = (ms / (1000 * 60)) % 60;
            t.Hours = ms / (1000 * 60 * 60);
            return t;
        }

        public long PartsToMs(TimeParts t)
        {
            return t.Milliseconds +
                   t.Seconds * 1000 +
                   t.Minutes * 60 * 1000 +
                   t.Hours * 60 * 60 * 1000;
        }

        public string Ms_to_PartsString(long ms)
        {
            long milliseconds = ms % 1000;
            long seconds = (ms / 1000) % 60;
            long minutes = (ms / (1000 * 60)) % 60;
            long hours = ms / (1000 * 60 * 60);

            var parts = new List<string>();
            if (hours > 0) parts.Add($"{hours}h");
            if (minutes > 0) parts.Add($"{minutes}m");
            if (seconds > 0) parts.Add($"{seconds}s");
            if (milliseconds > 0) parts.Add($"{milliseconds}ms");

            String DoneString = string.Join(" ", parts);

            return DoneString;
        }

        public void SelectedAction()
        {
            if (_Main.CurserPositionList.SelectedItem == null)
                return;

            if (_Main.ShowPointOnClick.Checked && !_Main.CurserPositionList.SelectedItem.ToString().Contains("Key") && !_Main.ShowAllPositionsCheck.Checked && !_Main.CurserPositionList.SelectedItem.ToString().Contains("Wait") && !_Main.CurserPositionList.SelectedItem.ToString().Contains("+"))
            {
                int indexpos = _Main.CurserPositionList.SelectedIndex;
                Point pos = SavedActions[indexpos].MousePosition;
                Keys presskey = SavedActions[indexpos].Mousepress ?? Keys.None;
                //Debug.WriteLine("Selected: " + pos.X + " " + pos.Y);
                ShowPositionMarker(pos, 2, presskey);
            }
        }

        public void MoveActions(bool Direction)
        {
            if (_Main.CurserPositionList.SelectedIndex == -1)
            {
                _Main.Setinfotextfast("No Action selected to move.");
                return;
            }
            int selectedIndex = _Main.CurserPositionList.SelectedIndex;
            if (Direction)
            {
                if (selectedIndex > 0)
                {
                    var item = SavedActions[selectedIndex];
                    SavedActions.RemoveAt(selectedIndex);
                    SavedActions.Insert(selectedIndex - 1, item);
                    UpdateActionList();
                    _Main.CurserPositionList.SelectedIndex = selectedIndex - 1;
                }
            }
            else
            {
                if (selectedIndex < _Main.CurserPositionList.Items.Count - 1 && selectedIndex != -1)
                {
                    var item = SavedActions[selectedIndex];
                    SavedActions.RemoveAt(selectedIndex);
                    SavedActions.Insert(selectedIndex + 1, item);
                    UpdateActionList();
                    _Main.CurserPositionList.SelectedIndex = selectedIndex + 1;
                }
            }
        }

        public void MoveToNUMFunc()
        {
            if (_Main.CurserPositionList.SelectedIndex == -1)
            {
                _Main.Setinfotextfast("No Action selected to move.");
                return;
            }
            int selectedIndex = _Main.CurserPositionList.SelectedIndex;
            Move_To_PositionMessage(selectedIndex);

        }

        public void Menu_On_OpenFunc()
        {
            int selectedIndex = _Main.CurserPositionList.SelectedIndex;
            if (selectedIndex == -1)
            {
                return;
            }
            ActionType type = SavedActions[selectedIndex].Type;

            if (type == ActionType.MouseClick)
            {
                _Main.Switch_to_SavePositon.Visible = false;
            }
            else if (type == ActionType.KeyPress)
            {
                _Main.Switch_to_SaveKey.Visible = false;
            }
            else if (type == ActionType.Waittime)
            {
                _Main.Switch_to_Wait.Visible = false;
                _Main.ActionMenuSetHold.Visible = false;
            }
        }

        public void Menu_On_CloseFunc()
        {
            _Main.Switch_to_SaveKey.Visible = true;
            _Main.Switch_to_SavePositon.Visible = true;
            _Main.Switch_to_Wait.Visible = true;
            _Main.ActionMenuSetHold.Visible = true;
        }

        public void Switch_To_Save_PositionFunc()
        {
            int selectedIndex = _Main.CurserPositionList.SelectedIndex;
            if (selectedIndex == -1)
            {
                _Main.Setinfotextfast("No Action selected to Edit.");
                return;
            }

            (Point Pos, Keys KeyPress, long holding, bool another) = SavePositionInList_Click(true);

            SavedActions.RemoveAt(selectedIndex);
            SavedActions.Insert(selectedIndex, new ClickOrKeyAction
            {
                Type = ActionType.MouseClick,
                MousePosition = Pos,
                Mousepress = KeyPress,
                HoldClickMS = _Main.CheckAddHold.Checked ? holding : 0
            });
            UpdateActionList();
        }
        public void Switch_To_Save_KeyFunc()
        {
            int selectedIndex = _Main.CurserPositionList.SelectedIndex;
            if (selectedIndex == -1)
            {
                _Main.Setinfotextfast("No Action selected to Edit.");
                return;
            }

            Keys? existingHoldKey = SavedActions[selectedIndex].HoldKey;
            (Keys PressedKey, long holding) = _Main.RecordKeysSend(true, true, false, false, true, true);
            if (PressedKey == Keys.None)
            {
                _Main.Setinfotextfast("No Key found or it got canceled");
            }
            else
            {
                SavedActions.RemoveAt(selectedIndex);
                SavedActions.Insert(selectedIndex, new ClickOrKeyAction
                {
                    Type = ActionType.KeyPress,
                    Key = PressedKey,
                    HoldKey = existingHoldKey,
                    HoldClickMS = _Main.CheckAddHold.Checked ? holding : 0
                });
            }
            UpdateActionList();
        }
        public void Switch_To_WaitFunc()
        {
            int selectedIndex = _Main.CurserPositionList.SelectedIndex;
            if (selectedIndex == -1)
            {
                _Main.Setinfotextfast("No Action selected to Edit.");
                return;
            }

            if (_Main.WaitTimeMs.Value == 0 && _Main.WaitTimeSec.Value == 0 && _Main.WaitTimeMin.Value == 0 && _Main.WaitTimeHour.Value == 0)
            {
                _Main.Setinfotextfast("No Wait time set to Edit.");
                return;
            }
            TimeParts got = new TimeParts();
            got.Milliseconds = (long)_Main.WaitTimeMs.Value;
            got.Seconds = (long)_Main.WaitTimeSec.Value;
            got.Minutes = (long)_Main.WaitTimeMin.Value;
            got.Hours = (long)_Main.WaitTimeHour.Value;
            long totalMs = PartsToMs(got);
            SavedActions.RemoveAt(selectedIndex);
            SavedActions.Insert(selectedIndex, new ClickOrKeyAction
            {
                Type = ActionType.Waittime,
                ToWait = totalMs
            });
            UpdateActionList();
        }

        public void EditActionsSelected()
        {
            int selectedIndex = _Main.CurserPositionList.SelectedIndex;
            if (selectedIndex == -1)
            {
                _Main.Setinfotextfast("No Action selected to Edit.");
                return;
            }

            ActionType MouseKey = SavedActions[selectedIndex].Type;
            if (MouseKey == ActionType.MouseClick)
            {
                (Point Pos, Keys KeyPress, long holding, bool another) = SavePositionInList_Click(true);

                SavedActions.RemoveAt(selectedIndex);
                SavedActions.Insert(selectedIndex, new ClickOrKeyAction
                {
                    Type = ActionType.MouseClick,
                    MousePosition = Pos,
                    Mousepress = KeyPress,
                    HoldClickMS = _Main.CheckAddHold.Checked ? holding : 0
                });
            }
            else if (MouseKey == ActionType.HoldAndPress)
            {
                (Keys HoldKey, long holding) = _Main.RecordKeysSend(true, true, false, false, true, _Main.CheckAddHold.Checked);
                if (HoldKey == Keys.None)
                {
                    _Main.Setinfotextfast("Hold key canceled.");
                    return;
                }

                (Keys PressKey, long __) = _Main.RecordKeysSend(true, true, false, false, true, false, HoldKey);
                if (PressKey == Keys.None)
                {
                    _Main.Setinfotextfast("Press key canceled.");
                    return;
                }

                SavedActions.RemoveAt(selectedIndex);
                SavedActions.Insert(selectedIndex, new ClickOrKeyAction
                {
                    Type = ActionType.HoldAndPress,
                    HoldKey = HoldKey,
                    Key = PressKey,
                    HoldClickMS = holding
                });
            }
            else if (MouseKey == ActionType.KeyPress)
            {
                Keys? existingHoldKey = SavedActions[selectedIndex].HoldKey;
                (Keys PressedKey, long holding) = _Main.RecordKeysSend(true, true, false, false, true, true);
                if (PressedKey == Keys.None)
                {
                    _Main.Setinfotextfast("No Key found or it got canceled");
                }
                else
                {
                    SavedActions.RemoveAt(selectedIndex);
                    SavedActions.Insert(selectedIndex, new ClickOrKeyAction
                    {
                        Type = ActionType.KeyPress,
                        Key = PressedKey,
                        HoldKey = existingHoldKey,
                        HoldClickMS = _Main.CheckAddHold.Checked ? holding : 0
                    });
                }
            }
            else if (MouseKey == ActionType.Waittime)
            {
                if (_Main.WaitTimeMs.Value == 0 && _Main.WaitTimeSec.Value == 0 && _Main.WaitTimeMin.Value == 0 && _Main.WaitTimeHour.Value == 0)
                {
                    _Main.Setinfotextfast("No Wait time set to Edit.");
                    return;
                }
                TimeParts got = new TimeParts();
                got.Milliseconds = (long)_Main.WaitTimeMs.Value;
                got.Seconds = (long)_Main.WaitTimeSec.Value;
                got.Minutes = (long)_Main.WaitTimeMin.Value;
                got.Hours = (long)_Main.WaitTimeHour.Value;
                long totalMs = PartsToMs(got);
                SavedActions.RemoveAt(selectedIndex);
                SavedActions.Insert(selectedIndex, new ClickOrKeyAction
                {
                    Type = ActionType.Waittime,
                    ToWait = totalMs
                });
            }
            UpdateActionList();
        }



        public void PositionSave()
        {
            (Point selectedPos, Keys detectedKey, long holding, bool another) = SavePositionInList_Click();
            SaveMouseClick(selectedPos, detectedKey, holding);
            if (another)
            {
                _Main.PositionSave.PerformClick();
            }
        }

        public void AddHoldAndPressAction()
        {
            (Keys HoldKey, long holding) = _Main.RecordKeysSend(true, true, false, false, true, _Main.CheckAddHold.Checked);
            if (HoldKey == Keys.None)
            {
                _Main.Setinfotextfast("Hold key canceled.");
                return;
            }

            (Keys PressKey, long __) = _Main.RecordKeysSend(true, true, false, false, true, false, HoldKey);
            if (PressKey == Keys.None)
            {
                _Main.Setinfotextfast("Press key canceled.");
                return;
            }

            SavedActions.Add(new ClickOrKeyAction
            {
                Type = ActionType.HoldAndPress,
                HoldKey = HoldKey,
                Key = PressKey,
                HoldClickMS = holding
            });

            _Main.Setinfotextfast($"Action Saved: Hold <{HoldKey}> + Press <{PressKey}>");
            UpdateActionList();
        }

        public void KeySaveInAction()
        {
            if (_Main.CheckAddHoldWithPress.Checked)
            {
                AddHoldAndPressAction();
                return;
            }
            (Keys PressedKey, long holding) = _Main.RecordKeysSend(true, true, false, false, true, true);
            if (PressedKey == Keys.None)
            {
                _Main.Setinfotextfast("No Key found or it got canceled");
            }
            else
            {
                _Main.Setinfotextfast($"Action Saved: Key {PressedKey}");
                SaveKeyPress(PressedKey, holding);
            }
        }

        public void RemoveSelectedActions()
        {
            if (_Main.CurserPositionList.SelectedIndex == -1)
            {
                _Main.Setinfotextfast("No Action selected to Remove.");
                return;
            }
            int selectedIndex = _Main.CurserPositionList.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < SavedActions.Count)
            {
                _Main.Setinfotextfast("Removed Action: " + _Main.CurserPositionList.Items[selectedIndex].ToString());
                SavedActions.RemoveAt(selectedIndex);
                UpdateActionList();
                if (_Main.CurserPositionList.Items.Count > 0)
                    if (_Main.CurserPositionList.Items.Count > selectedIndex)
                        _Main.CurserPositionList.SelectedIndex = selectedIndex;
                    else
                        _Main.CurserPositionList.SelectedIndex = selectedIndex - 1;

            }
        }

        public void ClearSavedActions()
        {
            SavedActions.Clear();
            _Main.CurserPositionList.Items.Clear();
            _Main.Setinfotextfast(("All saved Actions deleted."));
            ReloadMarkers();
        }

        public void UpdateActionList()
        {
            _Main.CurserPositionList.Items.Clear();
            for (int i = 0; i < SavedActions.Count; i++)
            {
                _Main.CurserPositionList.Items.Add($"{i + 1}. {SavedActions[i]}");
            }
            ReloadMarkers();
        }


        private MarkerHandle ShowPositionMarker(Point pos, int setTime = 2, Keys topress = Keys.None)
        {
            int size = 10;
            var marker = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.Magenta, // Grün statt Magenta
                TransparencyKey = Color.Magenta,
                TopMost = true,
                ShowInTaskbar = false,
                Size = new Size(size + 170, size),
                Location = new Point(pos.X - size / 2, pos.Y - size / 2)
            };

            // Größe des Punkts (z. B. 6×6 Pixel)
            int Posindex = SavedActions.FindIndex(a => a.MousePosition.X == pos.X && a.MousePosition.Y == pos.Y) + 1;
            RectangleF RectBox;

            marker.Paint += (s, e) =>
            {

                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
                // Punkt zeichnen
                using (Brush pointBrush = new SolidBrush(Color.Red))
                    e.Graphics.FillEllipse(pointBrush, 0, 0, size - 1, size - 1);

                // Text vorbereiten
                string text = $"{Posindex}. X:{pos.X} Y:{pos.Y} <{topress}>";
                using (Font font = new Font("Segoe UI", 10))
                {
                    SizeF textSize = e.Graphics.MeasureString(text, font);
                    RectangleF textRect = new RectangleF(size + 2, 0, textSize.Width + 4, textSize.Height);
                    RectBox = textRect;

                    // Hintergrund (halbtransparent schwarz)
                    //using (Brush bgBrush = new SolidBrush(Color.FromArgb(60, 60, 60))) // dunkelgrau
                    //    e.Graphics.FillRectangle(bgBrush, textRect);

                    // Text in Weiß
                    using (Brush textBrush = new SolidBrush(Color.White))
                        e.Graphics.DrawString(text, font, textBrush, size + 4, 0);
                }
            };


            var backgroundform = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.Magenta,
                TransparencyKey = Color.Magenta,
                Opacity = 0.5,
                TopMost = true,
                ShowInTaskbar = false,
                Size = new Size(size + 170, 1),
                Location = new Point(pos.X - size / 2 + 5, pos.Y - size / 2)
            };

            backgroundform.Paint += (s, e) =>
            {

                using (Brush bgBrush = new SolidBrush(Color.Black)) // dunkelgrau
                    e.Graphics.FillRectangle(bgBrush, size, 0, 165, 20);
            };

            backgroundform.Show();
            marker.Show();

            _Main.MakeClickThrough(backgroundform);
            _Main.MakeClickThrough(marker);

            var handle = new MarkerHandle(backgroundform, marker);

            // Kreis nach kurzer Zeit wieder entfernen
            if (setTime > 0)
            {
                var t = new System.Windows.Forms.Timer();
                t.Interval = setTime * 1000; // Zeit in Millisekunden
                t.Tick += (s, e) =>
                {
                    t.Stop();
                    handle.Close();

                };
                t.Start();
            }
            return handle;
        }

        public void ReloadMarkers()
        {
            if (_Main.ShowAllPositionsCheck.Checked)
            {
                ShowAllPositionens();
            }
        }

        public void ShowAllPositionens()
        {
            // Erst alte Marker entfernen
            foreach (var marker in activeMarkers)
                marker.Close();
            activeMarkers.Clear();

            if (_Main.ShowAllPositionsCheck.Checked)
            {
                foreach (var action in SavedActions)
                {
                    if (action.Type == ActionType.MouseClick && action.Mousepress != null)
                    {
                        var marker = ShowPositionMarker(action.MousePosition, 0, action.Mousepress.Value);
                        activeMarkers.Add(marker);
                    }
                }
            }
        }
    }

    public class MarkerHandle
    {
        public Form BackgroundForm { get; }
        public Form MarkerForm { get; }

        public MarkerHandle(Form backgroundForm, Form markerForm = null)
        {
            BackgroundForm = backgroundForm;
            MarkerForm = markerForm;
        }

        public void Close()
        {
            if (BackgroundForm != null && !BackgroundForm.IsDisposed)
                BackgroundForm.Close();

            if (MarkerForm != null && !MarkerForm.IsDisposed)
                MarkerForm.Close();
        }
    }

    public enum ActionType
    {
        MouseClick,
        KeyPress,
        Waittime,
        HoldAndPress
    }

    public class ClickOrKeyAction
    {
        public ActionType Type { get; set; }
        public Point MousePosition { get; set; }
        public Keys? Mousepress { get; set; }
        public Keys? Key { get; set; }
        public Keys? HoldKey { get; set; }
        public long ToWait { get; set; }
        public long HoldClickMS { get; set; }


        public override string ToString()
        {
            List<string> tranformtoparts(long ms)
            {
                long milliseconds = ms % 1000;
                long seconds = (ms / 1000) % 60;
                long minutes = (ms / (1000 * 60)) % 60;
                long hours = ms / (1000 * 60 * 60);

                var parts = new List<string>();
                if (hours > 0) parts.Add($"{hours}h");
                if (minutes > 0) parts.Add($"{minutes}m");
                if (seconds > 0) parts.Add($"{seconds}s");
                if (milliseconds > 0) parts.Add($"{milliseconds}ms");

                return parts;
            }

            if (ToWait > 0)
            {
                var parts = tranformtoparts(ToWait);
                string waitText = "Wait: " + string.Join(" ", parts);
                return waitText;//Environment.NewLine + waitText;
            }

            if (HoldKey.HasValue && Key.HasValue)
            {
                var parts = tranformtoparts(HoldClickMS);
                if (parts.Count > 0)
                {
                    return $"H: <{HoldKey.Value} [{string.Join(" ", parts)}]> + P: <{Key.Value}>";
                }
                return $"H: <{HoldKey.Value}> + P: <{Key.Value}>";
            }

            if (HoldClickMS > 0)
            {
                var parts = tranformtoparts(HoldClickMS);
                return Type == ActionType.MouseClick
                    ? $"X:{MousePosition.X}, Y:{MousePosition.Y} <{Mousepress}> Hold(" + string.Join(" ", parts) + ")"
                    : $"Key: <{Key}> Hold(" + string.Join(" ", parts) + ")";
            }
            else
            {
                return Type == ActionType.MouseClick
                    ? $"X:{MousePosition.X}, Y:{MousePosition.Y} <{Mousepress}>"
                    : $"Key: <{Key}>";
            }

        }
    }
}
