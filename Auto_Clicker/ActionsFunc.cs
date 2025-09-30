using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;

namespace Auto_Clicker
{
    public class ActionsFunc
    {
        private Form1 mainForm;


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
            mainForm = form;
        }



        public (Point, Keys, bool) SavePositionInList_Click(bool Edit = false)
        {
            mainForm.hotkeyTimer.Stop();
            mainForm.WindowState = FormWindowState.Minimized;

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
            int TimerCounter = 0;

            if (detectedKey == Keys.LButton)
            {
                while ((GetAsyncKeyState(Keys.LButton) & 0x8000) != 0)
                {
                    Thread.Sleep(10); // Warten, bis die Taste losgelassen wird
                    TimerCounter += 1;
                    if (TimerCounter >= 70)
                    {
                        break;
                    }
                }
            }

            marker.Close();

            if (TimerCounter >= 70)
            {
                detectedKey = Keys.Modifiers;
            }

            if (detectedKey != Keys.None)
            {
                selectedPos = Cursor.Position;
                //SaveMouseClick(selectedPos, detectedKey);
                mainForm.WindowState = FormWindowState.Normal;
                mainForm.Activate(); // In den Vordergrund holen
                string infotextmessage = $"Position Saved: X={selectedPos.X}, Y={selectedPos.Y} <{detectedKey.ToString()}>";
                if (detectedKey == Keys.Modifiers)
                {
                    infotextmessage = $"Position Saved: X={selectedPos.X}, Y={selectedPos.Y} <Move>";
                }

                if (mainForm.disableWindowOnPositionMenu.Checked || Edit)
                {
                    mainForm.Setinfotextfast(infotextmessage + " (Window Disabled)");
                }
                else
                {
                    //MessageBox.Show($"Position Saved: X={selectedPos.X}, Y={selectedPos.Y}");
                    var (dialogResult, another) = ShowCustomMessage(infotextmessage);
                    mainForm.Setinfotextfast(infotextmessage);

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
            mainForm.hotkeyTimer.Start();
            return (selectedPos, detectedKey, setanother);
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


        public void StartClickingAction()
        {
            lock (mainForm.clickLock)
            {
                if (mainForm.clicking) return;
                mainForm.clicking = true;
                mainForm.clickCts = new CancellationTokenSource();
            }

            if (mainForm.ShowHideMenu.Checked)
            {
                if (mainForm.Clickoverlay == null || mainForm.Clickoverlay.IsDisposed)
                {
                    mainForm.Clickoverlay = new CursorOverlayForm();
                    mainForm.Clickoverlay.Show();
                }
            }

            CancellationToken token = mainForm.clickCts.Token;

            bool switchinfotext = false;
            mainForm.clickIndex = 0;
            var TextActionShow = "";

            int repeatCount = (int)mainForm.ActionRepeatTimes.Value; // wie oft klicken

            long waitTime = 0;

            // Klick-Intervall berechnen
            bool infinity = mainForm.ActionRepeatTimes.Value == 0;
            int clickCount = 1;

            Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();

                while ((infinity || clickCount < repeatCount) && !token.IsCancellationRequested)
                {
                    sw.Restart();
                    var (proc, _) = mainForm.sideForm.GetActiveProcessName();
                    if (mainForm.WhitelistappsCheck.Checked && mainForm.sideForm.AppsCheckedlist.Contains(proc) || !mainForm.WhitelistappsCheck.Checked && !mainForm.sideForm.AppsCheckedlist.Contains(proc))//(!BlacklistedWindowTitles.Contains(proc))
                    {
                        if (switchinfotext)
                            mainForm.Setinfotextfast("Auto clicker running.....", true);
                        switchinfotext = false;

                        int i = 1;
                        foreach (var action in SavedActions)
                        {
                            if (!mainForm.clicking || token.IsCancellationRequested)
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
                                        if (action.Mousepress.Value != mainForm.hotkey)
                                        {
                                            mainForm.DoClick(true, false, action.Mousepress.Value);
                                            TextActionShow = action.Mousepress.Value.ToString();
                                        }
                                    }
                                    //new InputSimulator().Mouse
                                    //    .LeftButtonClick();
                                }
                                else if (action.Type == ActionType.KeyPress && action.Key.HasValue)
                                {
                                    string keyName = action.Key.Value.ToString();
                                    if (DataStings.keyMap.TryGetValue(keyName, out VirtualKeyCode vk))
                                    {
                                        new InputSimulator().Keyboard
                                            .KeyPress(vk);
                                    }
                                    else if (DataStings.AllowedMouseList.Contains(action.Key.Value.ToString()))
                                    {
                                        if (action.Key.Value != mainForm.hotkey)
                                        {
                                            //Debug.WriteLine(action.Key.Value.ToString());
                                            mainForm.DoClick(true, false, action.Key.Value);
                                        }
                                    }
                                    TextActionShow = action.Key.Value.ToString();
                                }
                                else if (action.Type == ActionType.Waittime && action.ToWait > 0)
                                {
                                    waitTime = action.ToWait;
                                    //TextActionShow = $"Wait {waitTime}ms";
                                    sw.Restart();
                                }
                            }
                            String Texttoshow = $"Auto clicker ON   Count: {clickCount}";
                            long nextUpdate = 0;

                            while (sw.ElapsedMilliseconds < waitTime)
                            {
                                if (!mainForm.clicking || token.IsCancellationRequested)
                                    break;

                                if (sw.ElapsedMilliseconds >= nextUpdate)
                                {
                                    TimeParts waitTimeParts = new TimeParts();

                                    if (waitTime < 1000)
                                        waitTimeParts = MsToParts(waitTime - (sw.ElapsedMilliseconds + 50) / 100 * 100);
                                    else
                                        waitTimeParts = MsToParts(waitTime - (sw.ElapsedMilliseconds + 500) / 1000 * 1000);
                                    String Timestring =
                                        (waitTimeParts.Hours > 0 ? $"{waitTimeParts.Hours}h " : "") +
                                        (waitTimeParts.Minutes > 0 ? $"{waitTimeParts.Minutes}min " : "") +
                                        (waitTimeParts.Seconds > 0 ? $"{waitTimeParts.Seconds}s " : "") +
                                        (waitTime < 1000 ? $"{waitTimeParts.Milliseconds}ms" : "");

                                    if (nextUpdate < 100)
                                        TextActionShow = $"Wait {Timestring}";

                                    mainForm.Setinfotextfast(Texttoshow + $" Actions: Wait {Timestring}", true);
                                    //Debug.WriteLine($"Update time {Timestring}");

                                    if (waitTime < 1000)
                                        nextUpdate += 100;
                                    else
                                        nextUpdate += 1000;
                                }
                                //Debug.WriteLine($"Wait {sw.ElapsedMilliseconds}");
                                double remaining = nextUpdate - sw.ElapsedMilliseconds;
                                if (remaining > 2)
                                    Thread.Sleep((int)(remaining - 1));
                                else
                                    Thread.SpinWait(5);
                            }
                            Texttoshow = Texttoshow + $" Last Actions {i}. {TextActionShow}";
                            mainForm.Setinfotextfast(Texttoshow, true);
                            if (!mainForm.IgnoreWaitCheck.Checked)
                                Thread.Sleep((int)mainForm.TimeBetweenAction.Value);
                            else
                            {
                                if (waitTime != 0)
                                    Thread.Sleep(10);
                                else
                                    Thread.Sleep((int)mainForm.TimeBetweenAction.Value);
                            }
                            i++;
                        }

                        //Debug.WriteLine("Pause");
                        clickCount++;
                    }
                    else
                    {
                        if (!switchinfotext)
                            mainForm.Setinfotextfast("Auto clicker ON: Waiting for None Blacklisted window.......", true);
                        switchinfotext = true;
                    }
                }
                if (!infinity)
                {
                    mainForm.Setinfotextfast("Auto clicker Stopped.....", true);
                    mainForm.StopClicking();
                }
            }, token);
        }


        public void SaveMouseClick(Point pos, Keys Pressed = Keys.None)
        {
            if (pos == Point.Empty)
                pos = Cursor.Position;

            SavedActions.Add(new ClickOrKeyAction
            {
                Type = ActionType.MouseClick,
                MousePosition = pos,
                Mousepress = Pressed

            });

            UpdateActionList();
        }

        public void SaveKeyPress(Keys key)
        {
            SavedActions.Add(new ClickOrKeyAction
            {
                Type = ActionType.KeyPress,
                Key = key
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
            if (mainForm.WaitTimeMs.Value == 0 && mainForm.WaitTimeSec.Value == 0 && mainForm.WaitTimeMin.Value == 0 && mainForm.WaitTimeHour.Value == 0)
                return;
            TimeParts got = new TimeParts();
            got.Milliseconds = (long)mainForm.WaitTimeMs.Value;
            got.Seconds = (long)mainForm.WaitTimeSec.Value;
            got.Minutes = (long)mainForm.WaitTimeMin.Value;
            got.Hours = (long)mainForm.WaitTimeHour.Value;
            long totalMs = PartsToMs(got);
            SaveWaitTime(totalMs);
        }

        public static TimeParts MsToParts(long ms)
        {
            TimeParts t = new TimeParts();
            t.Milliseconds = ms % 1000;
            t.Seconds = (ms / 1000) % 60;
            t.Minutes = (ms / (1000 * 60)) % 60;
            t.Hours = ms / (1000 * 60 * 60);
            return t;
        }

        public static long PartsToMs(TimeParts t)
        {
            return t.Milliseconds +
                   t.Seconds * 1000 +
                   t.Minutes * 60 * 1000 +
                   t.Hours * 60 * 60 * 1000;
        }

        public void SelectedAction()
        {
            if (mainForm.CurserPositionList.SelectedItem == null)
                return;

            if (mainForm.ShowPointOnClick.Checked && !mainForm.CurserPositionList.SelectedItem.ToString().Contains("Key") && !mainForm.ShowAllPositionsCheck.Checked && !mainForm.CurserPositionList.SelectedItem.ToString().Contains("Wait"))
            {
                int indexpos = mainForm.CurserPositionList.SelectedIndex;
                Point pos = SavedActions[indexpos].MousePosition;
                Keys presskey = SavedActions[indexpos].Mousepress ?? Keys.None;
                //Debug.WriteLine("Selected: " + pos.X + " " + pos.Y);
                ShowPositionMarker(pos, 2, presskey);
            }
        }

        public void MoveActions(bool Direction)
        {
            if (mainForm.CurserPositionList.SelectedIndex == -1)
            {
                mainForm.Setinfotextfast("No Action selected to move.");
                return;
            }
            int selectedIndex = mainForm.CurserPositionList.SelectedIndex;
            if (Direction)
            {
                if (selectedIndex > 0)
                {
                    var item = SavedActions[selectedIndex];
                    SavedActions.RemoveAt(selectedIndex);
                    SavedActions.Insert(selectedIndex - 1, item);
                    UpdateActionList();
                    mainForm.CurserPositionList.SelectedIndex = selectedIndex - 1;
                }
            }
            else
            {
                if (selectedIndex < mainForm.CurserPositionList.Items.Count - 1 && selectedIndex != -1)
                {
                    var item = SavedActions[selectedIndex];
                    SavedActions.RemoveAt(selectedIndex);
                    SavedActions.Insert(selectedIndex + 1, item);
                    UpdateActionList();
                    mainForm.CurserPositionList.SelectedIndex = selectedIndex + 1;
                }
            }
        }

        public void EditActionsSelected()
        {
            int selectedIndex = mainForm.CurserPositionList.SelectedIndex;
            if (selectedIndex == -1)
            {
                mainForm.Setinfotextfast("No Action selected to Edit.");
                return;
            }

            ActionType MouseKey = SavedActions[selectedIndex].Type;
            if (MouseKey == ActionType.MouseClick)
            {
                (Point Pos, Keys KeyPress, bool another) = SavePositionInList_Click(true);

                SavedActions.RemoveAt(selectedIndex);
                SavedActions.Insert(selectedIndex, new ClickOrKeyAction
                {
                    Type = ActionType.MouseClick,
                    MousePosition = Pos,
                    Mousepress = KeyPress
                });

            }
            else if (MouseKey == ActionType.KeyPress)
            {
                Keys PressedKey = mainForm.RecordKeysSend(true, true, false, false, true);
                if (PressedKey == Keys.None)
                {
                    mainForm.Setinfotextfast("No Key found or it got canceled");
                }
                else
                {
                    SavedActions.RemoveAt(selectedIndex);
                    SavedActions.Insert(selectedIndex, new ClickOrKeyAction
                    {
                        Type = ActionType.KeyPress,
                        Key = PressedKey
                    });
                }
            }
            else if (MouseKey == ActionType.Waittime)
            {
                if (mainForm.WaitTimeMs.Value == 0 && mainForm.WaitTimeSec.Value == 0 && mainForm.WaitTimeMin.Value == 0 && mainForm.WaitTimeHour.Value == 0)
                {
                    mainForm.Setinfotextfast("No Wait time set to Edit.");
                    return;
                }
                TimeParts got = new TimeParts();
                got.Milliseconds = (long)mainForm.WaitTimeMs.Value;
                got.Seconds = (long)mainForm.WaitTimeSec.Value;
                got.Minutes = (long)mainForm.WaitTimeMin.Value;
                got.Hours = (long)mainForm.WaitTimeHour.Value;
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
            (Point selectedPos, Keys detectedKey, bool another) = SavePositionInList_Click();
            SaveMouseClick(selectedPos, detectedKey);
            if (another)
            {
                mainForm.PositionSave.PerformClick();
            }
        }

        public void KeySaveInAction()
        {
            Keys PressedKey = mainForm.RecordKeysSend(true, true, false, false, true);
            if (PressedKey == Keys.None)
            {
                mainForm.Setinfotextfast("No Key found or it got canceled");
            }
            else
            {
                mainForm.Setinfotextfast($"Action Saved: Key {PressedKey}");
                SaveKeyPress(PressedKey);
            }
        }

        public void RemoveSelectedActions()
        {
            if (mainForm.CurserPositionList.SelectedIndex == -1)
            {
                mainForm.Setinfotextfast("No Action selected to Remove.");
                return;
            }
            int selectedIndex = mainForm.CurserPositionList.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < SavedActions.Count)
            {
                SavedActions.RemoveAt(selectedIndex);
                UpdateActionList();
            }
        }

        public void ClearSavedActions()
        {
            SavedActions.Clear();
            mainForm.CurserPositionList.Items.Clear();
            mainForm.Setinfotextfast(("All saved Actions deleted."));
            ReloadMarkers();
        }

        public void UpdateActionList()
        {
            mainForm.CurserPositionList.Items.Clear();
            for (int i = 0; i < SavedActions.Count; i++)
            {
                mainForm.CurserPositionList.Items.Add($"{i + 1}. {SavedActions[i]}");
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
                Location = new Point(pos.X - size / 2 +5, pos.Y - size / 2)
            };

            backgroundform.Paint += (s, e) =>
            {

                using (Brush bgBrush = new SolidBrush(Color.Black)) // dunkelgrau
                    e.Graphics.FillRectangle(bgBrush, size, 0, 165, 20);
            };

            backgroundform.Show();
            marker.Show();

            mainForm.MakeClickThrough(backgroundform);
            mainForm.MakeClickThrough(marker);

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
            if (mainForm.ShowAllPositionsCheck.Checked)
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

            if (mainForm.ShowAllPositionsCheck.Checked)
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
        Waittime
    }

    public class ClickOrKeyAction
    {
        public ActionType Type { get; set; }
        public Point MousePosition { get; set; }
        public Keys? Mousepress { get; set; }
        public Keys? Key { get; set; }
        public long ToWait { get; set; }


        public override string ToString()
        {
            if (ToWait > 0)
            {
                long ms = ToWait;
                long milliseconds = ms % 1000;
                long seconds = (ms / 1000) % 60;
                long minutes = (ms / (1000 * 60)) % 60;
                long hours = ms / (1000 * 60 * 60);

                var parts = new List<string>();
                if (hours > 0) parts.Add($"{hours}h");
                if (minutes > 0) parts.Add($"{minutes}m");
                if (seconds > 0) parts.Add($"{seconds}s");
                if (milliseconds > 0) parts.Add($"{milliseconds}ms");

                string waitText = "Wait: " + string.Join(" ", parts);

                return Environment.NewLine + waitText;
            }

            if (Mousepress != null && Mousepress.Value == Keys.Modifiers)
            {
                return Type == ActionType.MouseClick
                    ? $"X:{MousePosition.X}, Y:{MousePosition.Y} <Move>"
                    : "Key: Move";
            }
            else
            {
                return Type == ActionType.MouseClick
                    ? $"X:{MousePosition.X}, Y:{MousePosition.Y} <{Mousepress}>"
                    : $"Key: {Key}";
            }

        }
    }
}
