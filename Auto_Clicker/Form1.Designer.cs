namespace Auto_Clicker
{
    partial class Form1
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            menuStrip1 = new MenuStrip();
            toolStripMenuItem1 = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            disableWindowOnPositionMenu = new ToolStripMenuItem();
            disableRedBoxMenu = new ToolStripMenuItem();
            SaveAppsToOnExitMenu = new ToolStripMenuItem();
            AddTooltipsMenu = new ToolStripMenuItem();
            clickCircleToolStripMenuItem = new ToolStripMenuItem();
            ShowHideMenu = new ToolStripMenuItem();
            custemizeCircleToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            saveToolStripMenuItem = new ToolStripMenuItem();
            SettingsSaveonexit = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            setTopMostMenu = new ToolStripMenuItem();
            UseMouse = new RadioButton();
            UseKeyboard = new RadioButton();
            groupBox2 = new GroupBox();
            WhitelistappsCheck = new CheckBox();
            SwitchToClick = new RadioButton();
            PositionIsChecked = new CheckBox();
            ResetAllSettings = new Button();
            HoldToClick = new RadioButton();
            HotkeyFindKey = new Button();
            Hotkeypressvalue = new ComboBox();
            GroupKeyPress = new GroupBox();
            KeyToPress = new ComboBox();
            ClickKeyFind = new Button();
            groupBox4 = new GroupBox();
            ClickPerSecNum = new NumericUpDown();
            ClicksPersSecButton = new RadioButton();
            PerTimeValue = new ComboBox();
            PerTimeButton = new RadioButton();
            PerTimeNum = new NumericUpDown();
            ClickRepeatgroup = new GroupBox();
            RepeatUnlimited = new RadioButton();
            RepeatRepeat = new RadioButton();
            RepeatTimes = new NumericUpDown();
            label1 = new Label();
            label2 = new Label();
            InfoLabel = new Label();
            groupBox5 = new GroupBox();
            ShowAllPositionsCheck = new CheckBox();
            LabelUsingActions = new Label();
            KeySaveInList = new Button();
            CurserPositionList = new ListBox();
            PositionSave = new Button();
            PositionClear = new Button();
            PositionRemove = new Button();
            ShowPointOnClick = new CheckBox();
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            ResetBlacklistList = new Button();
            SaveBlacklistList = new Button();
            BlackWhiteListAppsGroup = new GroupBox();
            button1 = new Button();
            AllAppsList = new CheckedListBox();
            AToolTips = new ToolTip(components);
            menuStrip1.SuspendLayout();
            groupBox2.SuspendLayout();
            GroupKeyPress.SuspendLayout();
            groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ClickPerSecNum).BeginInit();
            ((System.ComponentModel.ISupportInitialize)PerTimeNum).BeginInit();
            ClickRepeatgroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)RepeatTimes).BeginInit();
            groupBox5.SuspendLayout();
            BlackWhiteListAppsGroup.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            resources.ApplyResources(menuStrip1, "menuStrip1");
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1 });
            menuStrip1.Name = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { settingsToolStripMenuItem, clickCircleToolStripMenuItem, toolStripSeparator2, saveToolStripMenuItem, SettingsSaveonexit, toolStripSeparator3, setTopMostMenu });
            resources.ApplyResources(toolStripMenuItem1, "toolStripMenuItem1");
            toolStripMenuItem1.MergeIndex = 1;
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { disableWindowOnPositionMenu, disableRedBoxMenu, SaveAppsToOnExitMenu, AddTooltipsMenu });
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            resources.ApplyResources(settingsToolStripMenuItem, "settingsToolStripMenuItem");
            // 
            // disableWindowOnPositionMenu
            // 
            resources.ApplyResources(disableWindowOnPositionMenu, "disableWindowOnPositionMenu");
            disableWindowOnPositionMenu.Name = "disableWindowOnPositionMenu";
            disableWindowOnPositionMenu.Click += disableWindowOnPositionMenu_Click;
            // 
            // disableRedBoxMenu
            // 
            disableRedBoxMenu.Image = Properties.Resources.istockphoto_1904567040_612x612;
            disableRedBoxMenu.Name = "disableRedBoxMenu";
            resources.ApplyResources(disableRedBoxMenu, "disableRedBoxMenu");
            disableRedBoxMenu.Click += disableRedBoxMenu_Click;
            // 
            // SaveAppsToOnExitMenu
            // 
            SaveAppsToOnExitMenu.Checked = true;
            SaveAppsToOnExitMenu.CheckState = CheckState.Checked;
            SaveAppsToOnExitMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            SaveAppsToOnExitMenu.Image = Properties.Resources.istockphoto_1904567040_612x612;
            SaveAppsToOnExitMenu.Name = "SaveAppsToOnExitMenu";
            resources.ApplyResources(SaveAppsToOnExitMenu, "SaveAppsToOnExitMenu");
            SaveAppsToOnExitMenu.Click += SaveAppsToOnExitMenu_Click;
            // 
            // AddTooltipsMenu
            // 
            AddTooltipsMenu.Checked = true;
            AddTooltipsMenu.CheckState = CheckState.Checked;
            AddTooltipsMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            AddTooltipsMenu.Image = Properties.Resources.istockphoto_1904567040_612x612;
            AddTooltipsMenu.Name = "AddTooltipsMenu";
            resources.ApplyResources(AddTooltipsMenu, "AddTooltipsMenu");
            AddTooltipsMenu.Click += addTooltipsToolStripMenuItem_Click;
            // 
            // clickCircleToolStripMenuItem
            // 
            clickCircleToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { ShowHideMenu, custemizeCircleToolStripMenuItem });
            clickCircleToolStripMenuItem.Name = "clickCircleToolStripMenuItem";
            resources.ApplyResources(clickCircleToolStripMenuItem, "clickCircleToolStripMenuItem");
            // 
            // ShowHideMenu
            // 
            ShowHideMenu.Checked = true;
            ShowHideMenu.CheckState = CheckState.Checked;
            ShowHideMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
            ShowHideMenu.Image = Properties.Resources.istockphoto_1904567040_612x612;
            ShowHideMenu.Name = "ShowHideMenu";
            resources.ApplyResources(ShowHideMenu, "ShowHideMenu");
            ShowHideMenu.Click += showHideToolStripMenuItem_Click;
            // 
            // custemizeCircleToolStripMenuItem
            // 
            custemizeCircleToolStripMenuItem.Name = "custemizeCircleToolStripMenuItem";
            resources.ApplyResources(custemizeCircleToolStripMenuItem, "custemizeCircleToolStripMenuItem");
            custemizeCircleToolStripMenuItem.Click += custemizeCircleToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(toolStripSeparator2, "toolStripSeparator2");
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Image = Properties.Resources.Save_Icon;
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            resources.ApplyResources(saveToolStripMenuItem, "saveToolStripMenuItem");
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click_1;
            // 
            // SettingsSaveonexit
            // 
            SettingsSaveonexit.Checked = true;
            SettingsSaveonexit.CheckState = CheckState.Checked;
            SettingsSaveonexit.DisplayStyle = ToolStripItemDisplayStyle.Text;
            SettingsSaveonexit.Image = Properties.Resources.istockphoto_1904567040_612x612;
            SettingsSaveonexit.Name = "SettingsSaveonexit";
            resources.ApplyResources(SettingsSaveonexit, "SettingsSaveonexit");
            SettingsSaveonexit.Click += SettingsSaveonexit_Click_1;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(toolStripSeparator3, "toolStripSeparator3");
            // 
            // setTopMostMenu
            // 
            setTopMostMenu.Image = Properties.Resources.istockphoto_1904567040_612x612;
            setTopMostMenu.Name = "setTopMostMenu";
            resources.ApplyResources(setTopMostMenu, "setTopMostMenu");
            setTopMostMenu.Click += setTopMostToolStripMenuItem_Click;
            // 
            // UseMouse
            // 
            UseMouse.Checked = true;
            resources.ApplyResources(UseMouse, "UseMouse");
            UseMouse.Name = "UseMouse";
            UseMouse.TabStop = true;
            UseMouse.UseVisualStyleBackColor = true;
            UseMouse.CheckedChanged += radioButton1_CheckedChanged;
            // 
            // UseKeyboard
            // 
            resources.ApplyResources(UseKeyboard, "UseKeyboard");
            UseKeyboard.Name = "UseKeyboard";
            UseKeyboard.UseVisualStyleBackColor = true;
            UseKeyboard.CheckedChanged += radioButton2_CheckedChanged;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(WhitelistappsCheck);
            groupBox2.Controls.Add(SwitchToClick);
            groupBox2.Controls.Add(PositionIsChecked);
            groupBox2.Controls.Add(ResetAllSettings);
            groupBox2.Controls.Add(HoldToClick);
            groupBox2.Controls.Add(HotkeyFindKey);
            groupBox2.Controls.Add(Hotkeypressvalue);
            resources.ApplyResources(groupBox2, "groupBox2");
            groupBox2.Name = "groupBox2";
            groupBox2.TabStop = false;
            // 
            // WhitelistappsCheck
            // 
            resources.ApplyResources(WhitelistappsCheck, "WhitelistappsCheck");
            WhitelistappsCheck.Name = "WhitelistappsCheck";
            AToolTips.SetToolTip(WhitelistappsCheck, resources.GetString("WhitelistappsCheck.ToolTip"));
            WhitelistappsCheck.UseVisualStyleBackColor = true;
            WhitelistappsCheck.CheckedChanged += WhitelistappsCheck_CheckedChanged;
            // 
            // SwitchToClick
            // 
            SwitchToClick.Checked = true;
            resources.ApplyResources(SwitchToClick, "SwitchToClick");
            SwitchToClick.Name = "SwitchToClick";
            SwitchToClick.TabStop = true;
            AToolTips.SetToolTip(SwitchToClick, resources.GetString("SwitchToClick.ToolTip"));
            SwitchToClick.UseVisualStyleBackColor = true;
            // 
            // PositionIsChecked
            // 
            resources.ApplyResources(PositionIsChecked, "PositionIsChecked");
            PositionIsChecked.Name = "PositionIsChecked";
            AToolTips.SetToolTip(PositionIsChecked, resources.GetString("PositionIsChecked.ToolTip"));
            PositionIsChecked.UseVisualStyleBackColor = true;
            PositionIsChecked.CheckedChanged += PositionIsChecked_CheckedChanged;
            // 
            // ResetAllSettings
            // 
            resources.ApplyResources(ResetAllSettings, "ResetAllSettings");
            ResetAllSettings.Name = "ResetAllSettings";
            AToolTips.SetToolTip(ResetAllSettings, resources.GetString("ResetAllSettings.ToolTip"));
            ResetAllSettings.UseVisualStyleBackColor = true;
            ResetAllSettings.Click += ResetAllSettings_Click;
            // 
            // HoldToClick
            // 
            resources.ApplyResources(HoldToClick, "HoldToClick");
            HoldToClick.Name = "HoldToClick";
            AToolTips.SetToolTip(HoldToClick, resources.GetString("HoldToClick.ToolTip"));
            HoldToClick.UseVisualStyleBackColor = true;
            // 
            // HotkeyFindKey
            // 
            HotkeyFindKey.BackColor = SystemColors.ActiveCaption;
            resources.ApplyResources(HotkeyFindKey, "HotkeyFindKey");
            HotkeyFindKey.Name = "HotkeyFindKey";
            HotkeyFindKey.TabStop = false;
            HotkeyFindKey.UseVisualStyleBackColor = false;
            HotkeyFindKey.Click += HotkeyFindKey_Click;
            // 
            // Hotkeypressvalue
            // 
            Hotkeypressvalue.BackColor = SystemColors.ScrollBar;
            Hotkeypressvalue.DropDownStyle = ComboBoxStyle.DropDownList;
            Hotkeypressvalue.FormattingEnabled = true;
            resources.ApplyResources(Hotkeypressvalue, "Hotkeypressvalue");
            Hotkeypressvalue.Name = "Hotkeypressvalue";
            Hotkeypressvalue.TabStop = false;
            AToolTips.SetToolTip(Hotkeypressvalue, resources.GetString("Hotkeypressvalue.ToolTip"));
            Hotkeypressvalue.SelectedIndexChanged += Hotkeypressvalue_SelectedIndexChanged;
            // 
            // GroupKeyPress
            // 
            GroupKeyPress.Controls.Add(UseMouse);
            GroupKeyPress.Controls.Add(UseKeyboard);
            GroupKeyPress.Controls.Add(KeyToPress);
            GroupKeyPress.Controls.Add(ClickKeyFind);
            resources.ApplyResources(GroupKeyPress, "GroupKeyPress");
            GroupKeyPress.Name = "GroupKeyPress";
            GroupKeyPress.TabStop = false;
            // 
            // KeyToPress
            // 
            KeyToPress.BackColor = SystemColors.ScrollBar;
            KeyToPress.DropDownStyle = ComboBoxStyle.DropDownList;
            KeyToPress.FormattingEnabled = true;
            resources.ApplyResources(KeyToPress, "KeyToPress");
            KeyToPress.Name = "KeyToPress";
            KeyToPress.TabStop = false;
            AToolTips.SetToolTip(KeyToPress, resources.GetString("KeyToPress.ToolTip"));
            KeyToPress.SelectedIndexChanged += KeyToPress_SelectedIndexChanged;
            // 
            // ClickKeyFind
            // 
            ClickKeyFind.BackColor = SystemColors.ActiveCaption;
            resources.ApplyResources(ClickKeyFind, "ClickKeyFind");
            ClickKeyFind.Name = "ClickKeyFind";
            ClickKeyFind.TabStop = false;
            ClickKeyFind.UseVisualStyleBackColor = false;
            ClickKeyFind.Click += ClickKeyFind_Click;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(ClickPerSecNum);
            groupBox4.Controls.Add(ClicksPersSecButton);
            groupBox4.Controls.Add(PerTimeValue);
            groupBox4.Controls.Add(PerTimeButton);
            groupBox4.Controls.Add(PerTimeNum);
            resources.ApplyResources(groupBox4, "groupBox4");
            groupBox4.Name = "groupBox4";
            groupBox4.TabStop = false;
            // 
            // ClickPerSecNum
            // 
            resources.ApplyResources(ClickPerSecNum, "ClickPerSecNum");
            ClickPerSecNum.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            ClickPerSecNum.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            ClickPerSecNum.Name = "ClickPerSecNum";
            ClickPerSecNum.TabStop = false;
            ClickPerSecNum.Value = new decimal(new int[] { 50, 0, 0, 0 });
            // 
            // ClicksPersSecButton
            // 
            ClicksPersSecButton.Checked = true;
            resources.ApplyResources(ClicksPersSecButton, "ClicksPersSecButton");
            ClicksPersSecButton.Name = "ClicksPersSecButton";
            ClicksPersSecButton.TabStop = true;
            ClicksPersSecButton.UseVisualStyleBackColor = true;
            ClicksPersSecButton.CheckedChanged += radioButton6_CheckedChanged;
            // 
            // PerTimeValue
            // 
            PerTimeValue.BackColor = SystemColors.ScrollBar;
            PerTimeValue.DropDownStyle = ComboBoxStyle.DropDownList;
            resources.ApplyResources(PerTimeValue, "PerTimeValue");
            PerTimeValue.FormattingEnabled = true;
            PerTimeValue.Name = "PerTimeValue";
            PerTimeValue.TabStop = false;
            PerTimeValue.SelectedIndexChanged += PerTimeValue_SelectedIndexChanged;
            // 
            // PerTimeButton
            // 
            resources.ApplyResources(PerTimeButton, "PerTimeButton");
            PerTimeButton.Name = "PerTimeButton";
            AToolTips.SetToolTip(PerTimeButton, resources.GetString("PerTimeButton.ToolTip"));
            PerTimeButton.UseVisualStyleBackColor = true;
            PerTimeButton.CheckedChanged += radioButton5_CheckedChanged;
            // 
            // PerTimeNum
            // 
            resources.ApplyResources(PerTimeNum, "PerTimeNum");
            PerTimeNum.InterceptArrowKeys = false;
            PerTimeNum.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            PerTimeNum.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            PerTimeNum.Name = "PerTimeNum";
            PerTimeNum.TabStop = false;
            PerTimeNum.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // ClickRepeatgroup
            // 
            ClickRepeatgroup.Controls.Add(RepeatUnlimited);
            ClickRepeatgroup.Controls.Add(RepeatRepeat);
            ClickRepeatgroup.Controls.Add(RepeatTimes);
            ClickRepeatgroup.Controls.Add(label1);
            resources.ApplyResources(ClickRepeatgroup, "ClickRepeatgroup");
            ClickRepeatgroup.Name = "ClickRepeatgroup";
            ClickRepeatgroup.TabStop = false;
            // 
            // RepeatUnlimited
            // 
            RepeatUnlimited.Checked = true;
            resources.ApplyResources(RepeatUnlimited, "RepeatUnlimited");
            RepeatUnlimited.Name = "RepeatUnlimited";
            RepeatUnlimited.TabStop = true;
            RepeatUnlimited.UseVisualStyleBackColor = true;
            RepeatUnlimited.CheckedChanged += RepeatUnlimited_CheckedChanged;
            // 
            // RepeatRepeat
            // 
            resources.ApplyResources(RepeatRepeat, "RepeatRepeat");
            RepeatRepeat.Name = "RepeatRepeat";
            RepeatRepeat.UseVisualStyleBackColor = true;
            RepeatRepeat.CheckedChanged += radioButton2_CheckedChanged_1;
            // 
            // RepeatTimes
            // 
            resources.ApplyResources(RepeatTimes, "RepeatTimes");
            RepeatTimes.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            RepeatTimes.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            RepeatTimes.Name = "RepeatTimes";
            RepeatTimes.TabStop = false;
            RepeatTimes.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            // 
            // InfoLabel
            // 
            resources.ApplyResources(InfoLabel, "InfoLabel");
            InfoLabel.Name = "InfoLabel";
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(ShowAllPositionsCheck);
            groupBox5.Controls.Add(LabelUsingActions);
            groupBox5.Controls.Add(KeySaveInList);
            groupBox5.Controls.Add(CurserPositionList);
            groupBox5.Controls.Add(PositionSave);
            groupBox5.Controls.Add(PositionClear);
            groupBox5.Controls.Add(PositionRemove);
            groupBox5.Controls.Add(ShowPointOnClick);
            resources.ApplyResources(groupBox5, "groupBox5");
            groupBox5.Name = "groupBox5";
            groupBox5.TabStop = false;
            // 
            // ShowAllPositionsCheck
            // 
            resources.ApplyResources(ShowAllPositionsCheck, "ShowAllPositionsCheck");
            ShowAllPositionsCheck.Name = "ShowAllPositionsCheck";
            AToolTips.SetToolTip(ShowAllPositionsCheck, resources.GetString("ShowAllPositionsCheck.ToolTip"));
            ShowAllPositionsCheck.UseVisualStyleBackColor = true;
            ShowAllPositionsCheck.CheckedChanged += ShowAllPositionsCheck_CheckedChanged;
            // 
            // LabelUsingActions
            // 
            resources.ApplyResources(LabelUsingActions, "LabelUsingActions");
            LabelUsingActions.Name = "LabelUsingActions";
            // 
            // KeySaveInList
            // 
            KeySaveInList.BackColor = SystemColors.ActiveCaption;
            resources.ApplyResources(KeySaveInList, "KeySaveInList");
            KeySaveInList.Name = "KeySaveInList";
            AToolTips.SetToolTip(KeySaveInList, resources.GetString("KeySaveInList.ToolTip"));
            KeySaveInList.UseVisualStyleBackColor = false;
            KeySaveInList.Click += KeySaveInList_Click;
            // 
            // CurserPositionList
            // 
            CurserPositionList.FormattingEnabled = true;
            resources.ApplyResources(CurserPositionList, "CurserPositionList");
            CurserPositionList.Name = "CurserPositionList";
            CurserPositionList.SelectedIndexChanged += CurserPositionList_SelectedIndexChanged;
            // 
            // PositionSave
            // 
            PositionSave.BackColor = SystemColors.ActiveCaption;
            resources.ApplyResources(PositionSave, "PositionSave");
            PositionSave.Name = "PositionSave";
            AToolTips.SetToolTip(PositionSave, resources.GetString("PositionSave.ToolTip"));
            PositionSave.UseVisualStyleBackColor = false;
            PositionSave.Click += button2_Click;
            // 
            // PositionClear
            // 
            PositionClear.BackColor = SystemColors.ActiveCaption;
            resources.ApplyResources(PositionClear, "PositionClear");
            PositionClear.Name = "PositionClear";
            AToolTips.SetToolTip(PositionClear, resources.GetString("PositionClear.ToolTip"));
            PositionClear.UseVisualStyleBackColor = false;
            PositionClear.Click += PositionClear_Click;
            // 
            // PositionRemove
            // 
            PositionRemove.BackColor = SystemColors.ActiveCaption;
            resources.ApplyResources(PositionRemove, "PositionRemove");
            PositionRemove.Name = "PositionRemove";
            AToolTips.SetToolTip(PositionRemove, resources.GetString("PositionRemove.ToolTip"));
            PositionRemove.UseVisualStyleBackColor = false;
            PositionRemove.Click += PositionRemove_Click;
            // 
            // ShowPointOnClick
            // 
            ShowPointOnClick.Checked = true;
            ShowPointOnClick.CheckState = CheckState.Checked;
            resources.ApplyResources(ShowPointOnClick, "ShowPointOnClick");
            ShowPointOnClick.Name = "ShowPointOnClick";
            AToolTips.SetToolTip(ShowPointOnClick, resources.GetString("ShowPointOnClick.ToolTip"));
            ShowPointOnClick.UseVisualStyleBackColor = true;
            // 
            // ResetBlacklistList
            // 
            ResetBlacklistList.BackColor = SystemColors.ActiveCaption;
            resources.ApplyResources(ResetBlacklistList, "ResetBlacklistList");
            ResetBlacklistList.Name = "ResetBlacklistList";
            ResetBlacklistList.TabStop = false;
            AToolTips.SetToolTip(ResetBlacklistList, resources.GetString("ResetBlacklistList.ToolTip"));
            ResetBlacklistList.UseVisualStyleBackColor = false;
            ResetBlacklistList.Click += button2_Click_1;
            // 
            // SaveBlacklistList
            // 
            SaveBlacklistList.BackColor = SystemColors.ActiveCaption;
            resources.ApplyResources(SaveBlacklistList, "SaveBlacklistList");
            SaveBlacklistList.Name = "SaveBlacklistList";
            SaveBlacklistList.TabStop = false;
            AToolTips.SetToolTip(SaveBlacklistList, resources.GetString("SaveBlacklistList.ToolTip"));
            SaveBlacklistList.UseVisualStyleBackColor = false;
            SaveBlacklistList.Click += SaveBlacklistList_Click;
            // 
            // BlackWhiteListAppsGroup
            // 
            BlackWhiteListAppsGroup.Controls.Add(ResetBlacklistList);
            BlackWhiteListAppsGroup.Controls.Add(SaveBlacklistList);
            BlackWhiteListAppsGroup.Controls.Add(button1);
            BlackWhiteListAppsGroup.Controls.Add(AllAppsList);
            resources.ApplyResources(BlackWhiteListAppsGroup, "BlackWhiteListAppsGroup");
            BlackWhiteListAppsGroup.Name = "BlackWhiteListAppsGroup";
            BlackWhiteListAppsGroup.TabStop = false;
            // 
            // button1
            // 
            button1.BackColor = SystemColors.ActiveCaption;
            resources.ApplyResources(button1, "button1");
            button1.Name = "button1";
            button1.TabStop = false;
            AToolTips.SetToolTip(button1, resources.GetString("button1.ToolTip"));
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // AllAppsList
            // 
            AllAppsList.CheckOnClick = true;
            resources.ApplyResources(AllAppsList, "AllAppsList");
            AllAppsList.FormattingEnabled = true;
            AllAppsList.Name = "AllAppsList";
            AllAppsList.TabStop = false;
            AllAppsList.SelectedIndexChanged += AllAppsList_SelectedIndexChanged;
            // 
            // Form1
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ButtonFace;
            Controls.Add(BlackWhiteListAppsGroup);
            Controls.Add(groupBox5);
            Controls.Add(InfoLabel);
            Controls.Add(label2);
            Controls.Add(ClickRepeatgroup);
            Controls.Add(groupBox4);
            Controls.Add(GroupKeyPress);
            Controls.Add(groupBox2);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MainMenuStrip = menuStrip1;
            MaximizeBox = false;
            Name = "Form1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            groupBox2.ResumeLayout(false);
            GroupKeyPress.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)ClickPerSecNum).EndInit();
            ((System.ComponentModel.ISupportInitialize)PerTimeNum).EndInit();
            ClickRepeatgroup.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)RepeatTimes).EndInit();
            groupBox5.ResumeLayout(false);
            groupBox5.PerformLayout();
            BlackWhiteListAppsGroup.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.RadioButton UseMouse;
        private System.Windows.Forms.RadioButton UseKeyboard;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ComboBox Hotkeypressvalue;
        private System.Windows.Forms.Button HotkeyFindKey;
        private System.Windows.Forms.RadioButton SwitchToClick;
        private System.Windows.Forms.RadioButton HoldToClick;
        private System.Windows.Forms.GroupBox GroupKeyPress;
        private System.Windows.Forms.Button ClickKeyFind;
        private System.Windows.Forms.ComboBox KeyToPress;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.NumericUpDown PerTimeNum;
        private System.Windows.Forms.ComboBox PerTimeValue;
        private System.Windows.Forms.RadioButton ClicksPersSecButton;
        private System.Windows.Forms.RadioButton PerTimeButton;
        private System.Windows.Forms.NumericUpDown ClickPerSecNum;
        private System.Windows.Forms.GroupBox ClickRepeatgroup;
        private System.Windows.Forms.RadioButton RepeatRepeat;
        private System.Windows.Forms.RadioButton RepeatUnlimited;
        private System.Windows.Forms.NumericUpDown RepeatTimes;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label InfoLabel;
        private System.Windows.Forms.Button ResetAllSettings;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.ListBox CurserPositionList;
        private System.Windows.Forms.Button PositionSave;
        private System.Windows.Forms.Button PositionClear;
        private System.Windows.Forms.Button PositionRemove;
        private System.Windows.Forms.CheckBox PositionIsChecked;
        private System.Windows.Forms.CheckBox ShowPointOnClick;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem SettingsSaveonexit;
        private ToolStripMenuItem setTopMostMenu;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem disableWindowOnPositionMenu;
        private Button KeySaveInList;
        private ToolStripMenuItem disableRedBoxMenu;
        private Label LabelUsingActions;
        private CheckBox ShowAllPositionsCheck;
        private GroupBox BlackWhiteListAppsGroup;
        private CheckedListBox AllAppsList;
        private Button button1;
        private Button SaveBlacklistList;
        private Button ResetBlacklistList;
        private ToolStripMenuItem clickCircleToolStripMenuItem;
        private ToolStripMenuItem ShowHideMenu;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem custemizeCircleToolStripMenuItem;
        private ToolTip AToolTips;
        private ToolStripMenuItem SaveAppsToOnExitMenu;
        private ToolStripMenuItem AddTooltipsMenu;
        private CheckBox WhitelistappsCheck;
    }
}

