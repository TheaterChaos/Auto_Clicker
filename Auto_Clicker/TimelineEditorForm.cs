using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Auto_Clicker
{
    public partial class TimelineEditorForm : Form
    {
        private readonly RecorderFunc recorder;
        private readonly BindingList<RecAction> timeline;

        private DataGridView grid;
        private Button btnUp, btnDown, btnDelete;

        public TimelineEditorForm(RecorderFunc recorderFunc)
        {
            recorder = recorderFunc;
            timeline = new BindingList<RecAction>(recorder.RecActions);

            Text = "Timeline Editor";
            Width = 900;
            Height = 500;

            InitUI();
        }

        private void InitUI()
        {
            grid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 380,
                AutoGenerateColumns = false,
                DataSource = timeline,
                AllowUserToAddRows = false
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Type",
                Name = "Type",
                ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Delay (ms)",
                Name = "DelayMs"
            });

            var keyColumn = new DataGridViewComboBoxColumn
            {
                Name = "Key",
                HeaderText = "Key",
                DataSource = Enum.GetValues(typeof(Keys)),
                ValueType = typeof(Keys),
                FlatStyle = FlatStyle.Flat
            };
            grid.Columns.Add(keyColumn);

            var btnColumn = new DataGridViewComboBoxColumn
            {
                Name = "Button",
                HeaderText = "Button",
                DataSource = Enum.GetValues(typeof(MouseButtons)),
                ValueType = typeof(MouseButtons),
                FlatStyle = FlatStyle.Flat
            };
            grid.Columns.Add(btnColumn);


            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "X",
                Name = "X"
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Y",
                Name = "Y"
            });

            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;

                var act = timeline[e.RowIndex];
                var col = grid.Columns[e.ColumnIndex].Name;

                if (col == "Key" && act is RecKeyAction k)
                {
                    if (e.Value == null)
                        e.Value = k.Key;
                }
                else if (col == "Button" && act is RecMouseButtonAction m)
                {
                    if (e.Value == null)
                        e.Value = m.Button;
                }
                else if (col == "Type")
                {
                    e.Value = act.Type;
                }
                else if (col == "DelayMs")
                {
                    e.Value = act.DelayMs;
                }
                else if (col == "X" && act is RecMouseMoveAction mm)
                {
                    e.Value = mm.X;
                }
                else if (col == "Y" && act is RecMouseMoveAction mm2)
                {
                    e.Value = mm2.Y;
                }
            };


            grid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex < 0) return;

                var act = timeline[e.RowIndex];
                var col = grid.Columns[e.ColumnIndex].Name;
                var val = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                if (col == "Key" && act is RecKeyAction k && val is Keys key)
                    k.Key = key;

                if (col == "Button" && act is RecMouseButtonAction m && val is MouseButtons btn)
                    m.Button = btn;
            };

            grid.CellBeginEdit += (s, e) =>
            {
                var act = timeline[e.RowIndex];
                var col = grid.Columns[e.ColumnIndex].Name;

                if (col == "Key" && act is not RecKeyAction)
                    e.Cancel = true;

                if (col == "Button" && act is not RecMouseButtonAction)
                    e.Cancel = true;

                if ((col == "X" || col == "Y") && act is not RecMouseMoveAction)
                    e.Cancel = true;
            };

            grid.DataError += (s, e) =>
            {
                e.ThrowException = false;
                e.Cancel = true;
            };

            grid.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex < 0) return;

                var act = timeline[e.RowIndex];
                var col = grid.Columns[e.ColumnIndex].Name;
                var val = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                try
                {
                    if (col == "DelayMs")
                    {
                        act.DelayMs = Math.Max(0, Convert.ToInt32(val));
                    }
                    else if (col == "X" && act is RecMouseMoveAction mm)
                    {
                        mm.X = Convert.ToInt32(val);
                    }
                    else if (col == "Y" && act is RecMouseMoveAction mm2)
                    {
                        mm2.Y = Convert.ToInt32(val);
                    }
                }
                catch
                {
                    MessageBox.Show("Ungültige Zahl");
                    grid.CancelEdit();
                }
            };

            grid.EditingControlShowing += (s, e) =>
            {
                if (e.Control is TextBox tb)
                {
                    tb.KeyPress -= OnlyNumbers;
                    tb.KeyPress += OnlyNumbers;
                }
            };

            void OnlyNumbers(object sender, KeyPressEventArgs e)
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    e.Handled = true;
            }



            btnUp = new Button { Text = "▲", Left = 10, Top = 390, Width = 50 };
            btnDown = new Button { Text = "▼", Left = 70, Top = 390, Width = 50 };
            btnDelete = new Button { Text = "Delete", Left = 130, Top = 390, Width = 80 };

            btnUp.Click += (_, __) => Move(-1);
            btnDown.Click += (_, __) => Move(1);
            btnDelete.Click += (_, __) => Delete();
            grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grid.IsCurrentCellDirty)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };


            Controls.Add(grid);
            Controls.Add(btnUp);
            Controls.Add(btnDown);
            Controls.Add(btnDelete);
        }

        private void Move(int dir)
        {
            if (grid.CurrentRow == null) return;

            int i = grid.CurrentRow.Index;
            int ni = i + dir;

            if (ni < 0 || ni >= timeline.Count) return;

            var item = timeline[i];
            timeline.RemoveAt(i);
            timeline.Insert(ni, item);

            grid.ClearSelection();
            grid.Rows[ni].Selected = true;
        }

        private void Delete()
        {
            if (grid.CurrentRow == null) return;
            timeline.RemoveAt(grid.CurrentRow.Index);
        }
    }
}
