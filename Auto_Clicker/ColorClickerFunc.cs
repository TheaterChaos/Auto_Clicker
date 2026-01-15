using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsInput;

namespace Auto_Clicker
{
    public class ColorClickerFunc
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        private Form1 _Main;
        private System.Windows.Forms.Timer scanTimer;
        public Rectangle scanArea;
        public Form ActiveMarker;
        public Keys ClickaKey_Key = Keys.A;
        public Keys ClickPos_Key = Keys.LButton;
        public Point ClickPosition = new Point(0, 0);


        public ColorClickerFunc(Form1 form)
        {
            _Main = form;
        }

        public void StartClickingColor()
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
            bool FoundColor = false;
            int intervalMs = (int)_Main.ColorIntervalScan.Value;
            int currentindex = _Main.Color_SelectActionsbox.SelectedIndex;
            Keys ToPress = Keys.A;

            Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();

                while (!token.IsCancellationRequested)
                {
                    sw.Restart();
                    var (proc, _) = _Main._SideForm.GetActiveProcessName();
                    if (_Main.WhitelistappsCheck.Checked && _Main._SideForm.AppsCheckedlist.Contains(proc) || !_Main.WhitelistappsCheck.Checked && !_Main._SideForm.AppsCheckedlist.Contains(proc))//(!BlacklistedWindowTitles.Contains(proc))
                    {
                        if (switchinfotext)
                            _Main.Setinfotextfast("Auto clicker running.....", true);
                        switchinfotext = false;

                        if (!_Main.clicking || token.IsCancellationRequested)
                            break;

                        using (Bitmap bmp = new Bitmap(scanArea.Width, scanArea.Height))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                g.CopyFromScreen(scanArea.Location, Point.Empty, scanArea.Size);
                            }

                            Point? found = FindColorFromCenter(bmp, _Main.ColorSetColor.BackColor, (int)_Main.ColorToleranzenScan.Value);

                            if (found != null)
                            {
                                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                                int screenHeight = Screen.PrimaryScreen.Bounds.Height;
                                double absoluteX = 0;
                                double absoluteY = 0;
                                switch (currentindex)
                                {
                                    case 0:
                                        int screenX = scanArea.X + found.Value.X;
                                        int screenY = scanArea.Y + found.Value.Y;

                                        absoluteX = screenX * 65535.0 / (screenWidth - 1);
                                        absoluteY = screenY * 65535.0 / (screenHeight - 1);

                                        new InputSimulator().Mouse.MoveMouseTo(absoluteX, absoluteY);
                                        ToPress = Keys.LButton;
                                        break;
                                    case 1:
                                        ToPress = ClickaKey_Key;
                                        break;
                                    case 2:
                                        absoluteX = ClickPosition.X * 65535.0 / (screenWidth - 1);
                                        absoluteY = ClickPosition.Y * 65535.0 / (screenHeight - 1);

                                        new InputSimulator().Mouse.MoveMouseTo(absoluteX, absoluteY);
                                        ToPress = ClickPos_Key;
                                        break;
                                }
                                if (DataStings.keyMap.TryGetValue(ToPress.ToString(), out VirtualKeyCode vk))
                                {
                                    if (_Main.CheckIsHoldingon.Checked)
                                    {
                                        sw.Stop();
                                        _Main.DoClick(ToPress, (long)_Main.ColTimeHolding.Value);
                                        sw.Start();
                                    }
                                    else
                                    {
                                        _Main.DoClick(ToPress);
                                    }
                                }
                                else if (DataStings.AllowedMouseList.Contains(ToPress.ToString()))
                                {
                                    if (_Main.CheckIsHoldingon.Checked)
                                    {
                                        sw.Stop();
                                        _Main.DoClick(ToPress, (long)_Main.ColTimeHolding.Value);
                                        sw.Start();
                                    }
                                    else
                                    {
                                        _Main.DoClick(ToPress);
                                    }
                                }
                                FoundColor = true;
                            }
                        }

                    }
                    else
                    {
                        if (!switchinfotext)
                            _Main.Setinfotextfast("Auto clicker ON: Waiting for None Blacklisted window.......", true);
                        switchinfotext = true;
                    }

                    FoundColor = false;

                    double remaining = intervalMs - sw.Elapsed.TotalMilliseconds;

                    if (remaining > 2)
                    {
                        Thread.Sleep((int)(remaining - 1)); // Grobschlaf
                    }

                    while (sw.Elapsed.TotalMilliseconds < intervalMs)
                    {
                        if (!_Main.clicking || token.IsCancellationRequested)
                            break;

                        Thread.SpinWait(5); // Weniger Spins reicht für 100 CPS
                    }
                }
            }, token);
        }

        Point? FindColorFromCenter(Bitmap bmp, Color targetColor, int tolerance)
        {
            int centerX = bmp.Width / 2;
            int centerY = bmp.Height / 2;

            int maxRadius = Math.Max(bmp.Width, bmp.Height);

            for (int r = 0; r < maxRadius; r++)
            {
                // obere & untere Kante
                for (int dx = -r; dx <= r; dx++)
                {
                    int x1 = centerX + dx;
                    int y1 = centerY + r;
                    int x2 = centerX + dx;
                    int y2 = centerY - r;

                    if (x1 >= 0 && y1 >= 0 && x1 < bmp.Width && y1 < bmp.Height)
                    {
                        Color p = bmp.GetPixel(x1, y1);
                        if (IsColorMatch(p, targetColor, tolerance))
                            return new Point(x1, y1);
                    }

                    if (x2 >= 0 && y2 >= 0 && x2 < bmp.Width && y2 < bmp.Height)
                    {
                        Color p = bmp.GetPixel(x2, y2);
                        if (IsColorMatch(p, targetColor, tolerance))
                            return new Point(x2, y2);
                    }
                }

                // linke & rechte Kante
                for (int dy = -r + 1; dy <= r - 1; dy++)
                {
                    int x1 = centerX + r;
                    int y1 = centerY + dy;
                    int x2 = centerX - r;
                    int y2 = centerY + dy;

                    if (x1 >= 0 && y1 >= 0 && x1 < bmp.Width && y1 < bmp.Height)
                    {
                        Color p = bmp.GetPixel(x1, y1);
                        if (IsColorMatch(p, targetColor, tolerance))
                            return new Point(x1, y1);
                    }

                    if (x2 >= 0 && y2 >= 0 && x2 < bmp.Width && y2 < bmp.Height)
                    {
                        Color p = bmp.GetPixel(x2, y2);
                        if (IsColorMatch(p, targetColor, tolerance))
                            return new Point(x2, y2);
                    }
                }
            }
            return null;
        }

        // Vergleich mit Toleranz
        private bool IsColorMatch(Color c1, Color c2, int tolerance)
        {
            int dr = c1.R - c2.R;
            int dg = c1.G - c2.G;
            int db = c1.B - c2.B;
            //double distance = Math.Sqrt(dr * dr + dg * dg + db * db);
            //return distance < tolerance;
            int distSq = dr * dr + dg * dg + db * db;
            return distSq <= tolerance * tolerance;
        }

        public Form ShowAreaMarker(Rectangle area, int setTime = 0)
        {
            var marker = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.Magenta, // Magenta als durchsichtig
                TransparencyKey = Color.Magenta, // Magenta als durchsichtig
                TopMost = true,
                ShowInTaskbar = false,
                Bounds = area // Größe + Position direkt vom Rectangle
            };

            marker.Paint += (s, e) =>
            {
                // Roter Rand
                using (Pen pen = new Pen(Color.Red, 3))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, area.Width - 1, area.Height - 1);
                }
            };

            marker.Show();

            _Main.MakeClickThrough(marker);

            if (setTime > 0)
            {
                var t = new System.Windows.Forms.Timer();
                t.Interval = setTime * 1000;
                t.Tick += (s, e) =>
                {
                    t.Stop();
                    marker.Close();
                };
                t.Start();
            }

            return marker;
        }

        /// <summary>
        /// Rechteck-Auswahl mit Maus (wie Screenshot-Tool).
        /// </summary>
        public SelectionResult SelectRectangle()
        {
            using (var overlay = new OverlayForm())
            {
                if (!overlay.Visible && overlay.ShowDialog() == DialogResult.OK)
                {
                    // Screenshot vom ausgewählten Bereich
                    if (overlay.SelectedRectangle.Width < 2 || overlay.SelectedRectangle.Height < 2)
                    {
                        _Main.Setinfotextfast("Selection too small.");
                        return new SelectionResult { Area = Rectangle.Empty };
                    }
                    _Main.UpdateColorClickAreaText(overlay.SelectedRectangle);
                    return new SelectionResult { Area = overlay.SelectedRectangle };
                }
            }

            return new SelectionResult { Area = Rectangle.Empty };
        }

        public (Point, Keys) SavePositionColor()
        {
            _Main.hotkeyTimer.Stop();
            _Main.WindowState = FormWindowState.Minimized;

            Point selectedPos = Point.Empty;
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
                coordLabel.Text = $"X:{cursorPos.X}, Y:{cursorPos.Y}";

                Application.DoEvents();
                Thread.Sleep(30); // Kurze Pause, um CPU-Last zu reduzieren
            }
            marker.Close();

            if (detectedKey != Keys.None)
            {
                selectedPos = Cursor.Position;
                string infotextmessage = $"Position Saved: X:{selectedPos.X}, Y:{selectedPos.Y} <{detectedKey.ToString()}>";
                _Main.Setinfotextfast(infotextmessage);
            }

            while ((GetAsyncKeyState(detectedKey) & 0x8000) != 0)
            {
                Thread.Sleep(10); // Warten, bis die Taste losgelassen wird
            }
            _Main.WindowState = FormWindowState.Normal;
            _Main.Activate(); // In den Vordergrund holen
            _Main.hotkeyTimer.Start();
            return (selectedPos, detectedKey);
        }

        public void Set_Key_ButtonClick()
        {
            (Keys PressedKey, long holding) = _Main.RecordKeysSend(true, true, false, false, true);
            if (PressedKey == Keys.None)
            {
                _Main.Setinfotextfast("No Key found or it got canceled");
                return;
            }
            ClickaKey_Key = PressedKey;
            _Main.Color_Clickakey_setkey_Label.Text = "Key: "+PressedKey.ToString();
        }

        public void Set_Position_ButtonClick()
        {
            (Point Pos, Keys Clickkey) = SavePositionColor();
            if (Clickkey == Keys.None)
                return;
            ClickPos_Key = Clickkey;
            ClickPosition = Pos;
            _Main.Color_Clickakey_setkey_Label.Text = $"X:{Pos.X}, Y:{Pos.Y} <{Clickkey.ToString()}>";
        }

        public class SelectionResult
        {
            public Rectangle Area { get; set; }
        }

        /// <summary>
        /// Innere Klasse für Overlay & Rechteck-Zeichnung
        /// </summary>
        private class OverlayForm : Form
        {
            //private Bitmap screenshot;
            private Point startPoint;
            private Point currentPoint;
            private bool isDrawing;

            public Rectangle SelectedRectangle { get; private set; }


            public OverlayForm()
            {
                //var currentScreen = Screen.FromPoint(Cursor.Position);

                Rectangle allScreens = SystemInformation.VirtualScreen;

                this.Opacity = 0.1;
                this.BackColor = Color.Black;

                this.FormBorderStyle = FormBorderStyle.None;
                this.TopMost = true;
                this.Cursor = Cursors.Cross;
                this.DoubleBuffered = true;
                this.ShowInTaskbar = false;

                this.StartPosition = FormStartPosition.Manual;
                //this.Bounds = currentScreen.Bounds;
                this.Bounds = allScreens;
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    startPoint = e.Location;
                    currentPoint = e.Location; // wichtig
                    isDrawing = true;
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                if (isDrawing)
                {
                    currentPoint = e.Location;
                    Invalidate(); // ganze Form neu zeichnen
                }
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                if (isDrawing)
                {
                    isDrawing = false;

                    Point screenStart = this.PointToScreen(startPoint);
                    Point screenCurrent = this.PointToScreen(currentPoint);

                    SelectedRectangle = GetRectangle(screenStart, screenCurrent);

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (isDrawing && startPoint != currentPoint)
                {
                    Rectangle rect = GetRectangle(startPoint, currentPoint);

                    // Heller halbtransparenter Rechteck-Overlay
                    using (Brush brush = new SolidBrush(Color.FromArgb(255, Color.Yellow)))
                        e.Graphics.FillRectangle(brush, rect);

                    using (Pen pen = new Pen(Color.Red, 2))
                        e.Graphics.DrawRectangle(pen, rect);
                }
            }


            private Rectangle GetRectangle(Point p1, Point p2)
            {
                int x = Math.Min(p1.X, p2.X);
                int y = Math.Min(p1.Y, p2.Y);
                int width = Math.Abs(p1.X - p2.X);
                int height = Math.Abs(p1.Y - p2.Y);
                return new Rectangle(x, y, width, height);
            }
        }

        public class ColorPicker
        {
            public Color SelectedColor { get; private set; }

            private OverlayPickForm overlay;
            private PreviewForm preview;

            public ColorPicker()
            {
                overlay = new OverlayPickForm();
                preview = new PreviewForm();
            }

            public Color Show()
            {
                overlay.MouseMoved += Overlay_MouseMoved;
                overlay.MouseClicked += Overlay_MouseClicked;

                preview.Show();
                overlay.ShowDialog();

                preview.Close();
                return SelectedColor;
            }

            private void Overlay_MouseMoved(object? sender, Point cursorPos)
            {
                using (Bitmap bmp = new Bitmap(1, 1))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(cursorPos, Point.Empty, new Size(1, 1));
                    preview.UpdateColor(bmp.GetPixel(0, 0), cursorPos);
                }
            }

            private void Overlay_MouseClicked(object? sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    SelectedColor = preview.CurrentColor;
                    overlay.Close();
                }
            }
        }

        // OverlayForm: unsichtbare Fullscreen-Form
        public class OverlayPickForm : Form
        {
            public event EventHandler<Point> MouseMoved;
            public event MouseEventHandler MouseClicked;

            public OverlayPickForm()
            {
                Rectangle allScreens = SystemInformation.VirtualScreen;

                this.FormBorderStyle = FormBorderStyle.None;
                this.StartPosition = FormStartPosition.Manual;
                this.TopMost = true;
                this.Bounds = allScreens;
                this.Opacity = 0.01;
                this.Cursor = Cursors.Cross;

                this.MouseMove += (s, e) => MouseMoved?.Invoke(this, Cursor.Position);
                this.MouseClick += (s, e) => MouseClicked?.Invoke(this, e);
            }
        }

        // PreviewForm: kleine Form zur Anzeige der Farbe
        public class PreviewForm : Form
        {
            public Color CurrentColor { get; private set; }

            const int PixelCount = 11;   // ungerade Zahl!
            const int Zoom = 10;

            Bitmap magnifierBmp;

            public PreviewForm()
            {
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                Size = new Size(PixelCount * Zoom, PixelCount * Zoom);
                TopMost = true;
                DoubleBuffered = true;

                magnifierBmp = new Bitmap(PixelCount, PixelCount);
            }

            public void UpdateColor(Color centerColor, Point cursorPos)
            {
                CapturePixels(cursorPos);
                CurrentColor = centerColor;

                Location = new Point(cursorPos.X + 20, cursorPos.Y + 20);
                Invalidate();
            }


            void CapturePixels(Point cursor)
            {
                int half = PixelCount / 2;

                using (Graphics g = Graphics.FromImage(magnifierBmp))
                {
                    g.CopyFromScreen(
                        cursor.X - half,
                        cursor.Y - half,
                        0, 0,
                        magnifierBmp.Size
                    );
                }
            }


            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.InterpolationMode =
                    System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                e.Graphics.PixelOffsetMode =
                    System.Drawing.Drawing2D.PixelOffsetMode.Half;

                e.Graphics.DrawImage(
                    magnifierBmp,
                    new Rectangle(0, 0, Width, Height)
                );

                DrawGrid(e.Graphics);
                DrawCrosshair(e.Graphics);
            }

            void DrawGrid(Graphics g)
            {
                using Pen p = new Pen(Color.FromArgb(80, Color.Black));
                for (int i = 0; i <= PixelCount; i++)
                {
                    g.DrawLine(p, i * Zoom, 0, i * Zoom, Height);
                    g.DrawLine(p, 0, i * Zoom, Width, i * Zoom);
                }
            }

            void DrawCrosshair(Graphics g)
            {
                int center = (PixelCount / 2) * Zoom;
                using Pen p = new Pen(Color.Red, 2);

                g.DrawRectangle(p, center, center, Zoom, Zoom);
            }

        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
    }
}
