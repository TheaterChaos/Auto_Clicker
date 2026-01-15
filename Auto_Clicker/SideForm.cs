using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Auto_Clicker
{
    public partial class SideForm : Form
    {
        private Form1 mainForm;
        private System.Windows.Forms.Timer timer;

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

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;

        public List<string> BlacklistedWindowTitles = new List<string>();

        public List<string> AppsCheckedlist = new List<string>();

        public List<string> AllFoundAppsList = new List<string>();

        public SideForm(Form1 mainForm)
        {
            InitializeComponent();
            this.mainForm = mainForm;
        }

        public (string processName, string windowTitle) GetActiveProcessName()
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

        public void reloadCheckedApps()
        {
            BlacklistedWindowTitles.Clear();
            AppsCheckedlist.Clear();

            for (int i = 0; i < AllAppsList.Items.Count; i++)
            {
                object item = AllAppsList.Items[i];
                string selectedItem = item.ToString();
                string[] parts = selectedItem.Split(new[] { " - " }, StringSplitOptions.None);
                if (parts.Length >= 2)
                {
                    string processName = parts[0];
                    string windowTitle = parts[1];

                    if (mainForm.WhitelistappsCheck.Checked)
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

        private void WBRefresh_Click(object sender, EventArgs e)
        {
            btnRefreshWindows();
            reloadCheckedApps();
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
            mainForm.Setinfotextfast("App List saved.");
        }

        private void ResetBlacklistList_Click(object sender, EventArgs e)
        {
            BlacklistedWindowTitles.Clear();
            AppsCheckedlist.Clear();

            var collection = new System.Collections.Specialized.StringCollection();
            collection.AddRange(BlacklistedWindowTitles.ToArray());
            Properties.Settings.Default.BlacklistedApps = collection;

            Properties.Settings.Default.Save();
            btnRefreshWindows();
            reloadCheckedApps();

            mainForm.Setinfotextfast("App List Has Been Reset");
        }

        private void AllAppsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            reloadCheckedApps();
        }
    }
}
