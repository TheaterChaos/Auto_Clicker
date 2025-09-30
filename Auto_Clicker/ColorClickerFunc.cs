using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using static Auto_Clicker.ActionsFunc;

namespace Auto_Clicker
{
    public class ColorClickerFunc
    {
        private Form1 mainForm;
        private System.Windows.Forms.Timer scanTimer;
        public Rectangle scanArea;
        public Form ActiveMarker;


        public ColorClickerFunc(Form1 form)
        {
            mainForm = form;
        }

        public void StartClickingColor()
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
            bool FoundColor = false;

            int intervalMs = (int)mainForm.ColorIntervalScan.Value;

            Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();

                while (!token.IsCancellationRequested)
                {
                    sw.Restart();
                    var (proc, _) = mainForm.sideForm.GetActiveProcessName();
                    if (mainForm.WhitelistappsCheck.Checked && mainForm.sideForm.AppsCheckedlist.Contains(proc) || !mainForm.WhitelistappsCheck.Checked && !mainForm.sideForm.AppsCheckedlist.Contains(proc))//(!BlacklistedWindowTitles.Contains(proc))
                    {
                        if (switchinfotext)
                            mainForm.Setinfotextfast("Auto clicker running.....", true);
                        switchinfotext = false;

                        if (!mainForm.clicking || token.IsCancellationRequested)
                            break;

                        using (Bitmap bmp = new Bitmap(scanArea.Width, scanArea.Height))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                g.CopyFromScreen(scanArea.Location, Point.Empty, scanArea.Size);
                            }

                            for (int x = 0; x < bmp.Width; x++)
                            {
                                if (FoundColor) break;
                                for (int y = 0; y < bmp.Height; y++)
                                {
                                    if (FoundColor) break;
                                    Color pixel = bmp.GetPixel(x, y);

                                    if (IsColorMatch(pixel, mainForm.ColorSetColor.BackColor, (int)mainForm.ColorToleranzenScan.Value))
                                    {
                                        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                                        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                                        int screenX = scanArea.X + x;
                                        int screenY = scanArea.Y + y;

                                        double absoluteX = screenX * 65535.0 / (screenWidth - 1);
                                        double absoluteY = screenY * 65535.0 / (screenHeight - 1);

                                        new InputSimulator().Mouse.MoveMouseTo(absoluteX, absoluteY);
                                        mainForm.DoClick(true, false, Keys.LButton);
                                        Thread.Sleep(10);
                                        mainForm.DoClick(true, false, Keys.LButton);
                                        Debug.WriteLine($"Clicked at ({screenX}, {screenY}) Color: {pixel}");

                                        FoundColor = true;
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        if (!switchinfotext)
                            mainForm.Setinfotextfast("Auto clicker ON: Waiting for None Blacklisted window.......", true);
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
                        if (!mainForm.clicking || token.IsCancellationRequested)
                            break;

                        Thread.SpinWait(5); // Weniger Spins reicht für 100 CPS
                    }
                }
            }, token);
        }

        // Vergleich mit Toleranz
        private bool IsColorMatch(Color c1, Color c2, int tolerance)
        {
            int dr = c1.R - c2.R;
            int dg = c1.G - c2.G;
            int db = c1.B - c2.B;
            double distance = Math.Sqrt(dr * dr + dg * dg + db * db);
            return distance < tolerance;
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

            mainForm.MakeClickThrough(marker);

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
                        mainForm.Setinfotextfast("Selection too small.");
                        return new SelectionResult { Area = Rectangle.Empty };
                    }
                    mainForm.UpdateColorClickAreaText(overlay.SelectedRectangle);
                    return new SelectionResult { Area = overlay.SelectedRectangle };
                }
            }

            return new SelectionResult { Area = Rectangle.Empty };
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
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(cursorPos, Point.Empty, new Size(1, 1));
                    }
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

            public PreviewForm()
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.StartPosition = FormStartPosition.Manual;
                this.Size = new Size(20, 20);
                this.TopMost = true;
            }

            public void UpdateColor(Color color, Point cursorPos)
            {
                CurrentColor = color;
                this.BackColor = color; // ganze Form färben
                this.Location = new Point(cursorPos.X + 15, cursorPos.Y + 15);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
    }
}
