namespace SpesnetTogglSync
{
    partial class SyncForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            topPanel = new Panel();
            syncFromLabel = new Label();
            StartSyncDateTimeControl = new DateTimePicker();
            StartSyncButton = new Button();
            StatusLabel = new Label();
            UseMockSpesnetCheckBox = new CheckBox();
            mainTabControl = new TabControl();
            logTabPage = new TabPage();
            LogTextBox = new TextBox();
            mappingTabPage = new TabPage();
            SaveMappingsButton = new Button();
            MappingDirtyLabel = new Label();
            RefreshSpesnetButton = new Button();
            RefreshTogglButton = new Button();
            MappingGrid = new DataGridView();
            settingsTabPage = new TabPage();
            SaveSettingsButton = new Button();
            DataDirectoryTextBox = new TextBox();
            dataDirectoryLabel = new Label();
            SpesnetDomainTextBox = new TextBox();
            spesnetDomainLabel = new Label();
            SpesnetPasswordTextBox = new TextBox();
            spesnetPasswordLabel = new Label();
            SpesnetUsernameTextBox = new TextBox();
            spesnetUsernameLabel = new Label();
            TogglApiTokenTextBox = new TextBox();
            togglApiTokenLabel = new Label();
            StatusColumn = new DataGridViewComboBoxColumn();
            TogglClientColumn = new DataGridViewComboBoxColumn();
            TogglProjectColumn = new DataGridViewComboBoxColumn();
            SpesnetProjectColumn = new DataGridViewComboBoxColumn();
            SpesnetClientColumn = new DataGridViewComboBoxColumn();
            SpesnetWorkTaskColumn = new DataGridViewComboBoxColumn();
            topPanel.SuspendLayout();
            mainTabControl.SuspendLayout();
            logTabPage.SuspendLayout();
            mappingTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)MappingGrid).BeginInit();
            settingsTabPage.SuspendLayout();
            SuspendLayout();
            // 
            // topPanel
            // 
            topPanel.Controls.Add(syncFromLabel);
            topPanel.Controls.Add(StartSyncDateTimeControl);
            topPanel.Controls.Add(StartSyncButton);
            topPanel.Controls.Add(StatusLabel);
            topPanel.Controls.Add(UseMockSpesnetCheckBox);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Padding = new Padding(12);
            topPanel.Size = new Size(1550, 72);
            topPanel.TabIndex = 0;
            // 
            // syncFromLabel
            // 
            syncFromLabel.AutoSize = true;
            syncFromLabel.Location = new Point(15, 18);
            syncFromLabel.Name = "syncFromLabel";
            syncFromLabel.Size = new Size(75, 20);
            syncFromLabel.TabIndex = 0;
            syncFromLabel.Text = "Sync from";
            // 
            // StartSyncDateTimeControl
            // 
            StartSyncDateTimeControl.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            StartSyncDateTimeControl.Format = DateTimePickerFormat.Custom;
            StartSyncDateTimeControl.Location = new Point(94, 14);
            StartSyncDateTimeControl.Name = "StartSyncDateTimeControl";
            StartSyncDateTimeControl.Size = new Size(175, 27);
            StartSyncDateTimeControl.TabIndex = 1;
            // 
            // StartSyncButton
            // 
            StartSyncButton.Location = new Point(284, 13);
            StartSyncButton.Name = "StartSyncButton";
            StartSyncButton.Size = new Size(110, 29);
            StartSyncButton.TabIndex = 2;
            StartSyncButton.Text = "Start Sync";
            StartSyncButton.UseVisualStyleBackColor = true;
            StartSyncButton.Click += StartSyncButton_Click;
            // 
            // StatusLabel
            // 
            StatusLabel.AutoEllipsis = true;
            StatusLabel.Location = new Point(15, 44);
            StatusLabel.Name = "StatusLabel";
            StatusLabel.Size = new Size(1260, 20);
            StatusLabel.TabIndex = 3;
            StatusLabel.Text = "Ready";
            // 
            // UseMockSpesnetCheckBox
            // 
            UseMockSpesnetCheckBox.AutoSize = true;
            UseMockSpesnetCheckBox.Checked = true;
            UseMockSpesnetCheckBox.CheckState = CheckState.Checked;
            UseMockSpesnetCheckBox.Location = new Point(410, 15);
            UseMockSpesnetCheckBox.Name = "UseMockSpesnetCheckBox";
            UseMockSpesnetCheckBox.Size = new Size(151, 24);
            UseMockSpesnetCheckBox.TabIndex = 4;
            UseMockSpesnetCheckBox.Text = "Use mock Spesnet";
            UseMockSpesnetCheckBox.UseVisualStyleBackColor = true;
            UseMockSpesnetCheckBox.CheckedChanged += UseMockSpesnetCheckBox_CheckedChanged;
            // 
            // mainTabControl
            // 
            mainTabControl.Controls.Add(logTabPage);
            mainTabControl.Controls.Add(mappingTabPage);
            mainTabControl.Controls.Add(settingsTabPage);
            mainTabControl.Dock = DockStyle.Fill;
            mainTabControl.Location = new Point(0, 72);
            mainTabControl.Name = "mainTabControl";
            mainTabControl.SelectedIndex = 0;
            mainTabControl.Size = new Size(1550, 489);
            mainTabControl.TabIndex = 1;
            // 
            // logTabPage
            // 
            logTabPage.Controls.Add(LogTextBox);
            logTabPage.Location = new Point(4, 29);
            logTabPage.Name = "logTabPage";
            logTabPage.Padding = new Padding(8);
            logTabPage.Size = new Size(1542, 456);
            logTabPage.TabIndex = 0;
            logTabPage.Text = "Sync Log";
            logTabPage.UseVisualStyleBackColor = true;
            // 
            // LogTextBox
            // 
            LogTextBox.Dock = DockStyle.Fill;
            LogTextBox.Location = new Point(8, 8);
            LogTextBox.Multiline = true;
            LogTextBox.Name = "LogTextBox";
            LogTextBox.ReadOnly = true;
            LogTextBox.ScrollBars = ScrollBars.Vertical;
            LogTextBox.Size = new Size(1526, 440);
            LogTextBox.TabIndex = 0;
            // 
            // mappingTabPage
            // 
            mappingTabPage.Controls.Add(MappingGrid);
            mappingTabPage.Controls.Add(SaveMappingsButton);
            mappingTabPage.Controls.Add(MappingDirtyLabel);
            mappingTabPage.Controls.Add(RefreshSpesnetButton);
            mappingTabPage.Controls.Add(RefreshTogglButton);
            mappingTabPage.Location = new Point(4, 29);
            mappingTabPage.Name = "mappingTabPage";
            mappingTabPage.Padding = new Padding(8);
            mappingTabPage.Size = new Size(1542, 456);
            mappingTabPage.TabIndex = 1;
            mappingTabPage.Text = "Mapping";
            mappingTabPage.UseVisualStyleBackColor = true;
            // 
            // SaveMappingsButton
            // 
            SaveMappingsButton.Location = new Point(8, 8);
            SaveMappingsButton.Name = "SaveMappingsButton";
            SaveMappingsButton.Size = new Size(130, 29);
            SaveMappingsButton.TabIndex = 0;
            SaveMappingsButton.Text = "Save Mappings";
            SaveMappingsButton.UseVisualStyleBackColor = true;
            SaveMappingsButton.Click += SaveMappingsButton_Click;
            // 
            // MappingDirtyLabel
            // 
            MappingDirtyLabel.AutoSize = true;
            MappingDirtyLabel.ForeColor = Color.DarkOrange;
            MappingDirtyLabel.Location = new Point(144, 12);
            MappingDirtyLabel.Name = "MappingDirtyLabel";
            MappingDirtyLabel.Size = new Size(175, 20);
            MappingDirtyLabel.TabIndex = 1;
            MappingDirtyLabel.Text = "Unsaved mapping changes";
            MappingDirtyLabel.Visible = false;
            // 
            // RefreshSpesnetButton
            // 
            RefreshSpesnetButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            RefreshSpesnetButton.Location = new Point(1152, 8);
            RefreshSpesnetButton.Name = "RefreshSpesnetButton";
            RefreshSpesnetButton.Size = new Size(220, 29);
            RefreshSpesnetButton.TabIndex = 2;
            RefreshSpesnetButton.Text = "Refresh Spesnet Reference Data";
            RefreshSpesnetButton.UseVisualStyleBackColor = true;
            RefreshSpesnetButton.Click += RefreshSpesnetButton_Click;
            // 
            // RefreshTogglButton
            // 
            RefreshTogglButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            RefreshTogglButton.Location = new Point(1378, 8);
            RefreshTogglButton.Name = "RefreshTogglButton";
            RefreshTogglButton.Size = new Size(153, 29);
            RefreshTogglButton.TabIndex = 3;
            RefreshTogglButton.Text = "Refresh from Toggl";
            RefreshTogglButton.UseVisualStyleBackColor = true;
            RefreshTogglButton.Click += RefreshTogglButton_Click;
            // 
            // MappingGrid
            // 
            MappingGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            MappingGrid.ColumnHeadersHeight = 29;
            MappingGrid.Location = new Point(8, 43);
            MappingGrid.Name = "MappingGrid";
            MappingGrid.RowHeadersWidth = 51;
            MappingGrid.Size = new Size(1526, 405);
            MappingGrid.TabIndex = 4;
            // 
            // settingsTabPage
            // 
            settingsTabPage.Controls.Add(SaveSettingsButton);
            settingsTabPage.Controls.Add(DataDirectoryTextBox);
            settingsTabPage.Controls.Add(dataDirectoryLabel);
            settingsTabPage.Controls.Add(SpesnetDomainTextBox);
            settingsTabPage.Controls.Add(spesnetDomainLabel);
            settingsTabPage.Controls.Add(SpesnetPasswordTextBox);
            settingsTabPage.Controls.Add(spesnetPasswordLabel);
            settingsTabPage.Controls.Add(SpesnetUsernameTextBox);
            settingsTabPage.Controls.Add(spesnetUsernameLabel);
            settingsTabPage.Controls.Add(TogglApiTokenTextBox);
            settingsTabPage.Controls.Add(togglApiTokenLabel);
            settingsTabPage.Location = new Point(4, 29);
            settingsTabPage.Name = "settingsTabPage";
            settingsTabPage.Padding = new Padding(8);
            settingsTabPage.Size = new Size(1542, 456);
            settingsTabPage.TabIndex = 2;
            settingsTabPage.Text = "Settings";
            settingsTabPage.UseVisualStyleBackColor = true;
            // 
            // SaveSettingsButton
            // 
            SaveSettingsButton.Location = new Point(11, 339);
            SaveSettingsButton.Name = "SaveSettingsButton";
            SaveSettingsButton.Size = new Size(130, 29);
            SaveSettingsButton.TabIndex = 13;
            SaveSettingsButton.Text = "Save Settings";
            SaveSettingsButton.UseVisualStyleBackColor = true;
            SaveSettingsButton.Click += SaveSettingsButton_Click;
            // 
            // DataDirectoryTextBox
            // 
            DataDirectoryTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            DataDirectoryTextBox.Location = new Point(11, 243);
            DataDirectoryTextBox.Name = "DataDirectoryTextBox";
            DataDirectoryTextBox.Size = new Size(1516, 27);
            DataDirectoryTextBox.TabIndex = 10;
            // 
            // dataDirectoryLabel
            // 
            dataDirectoryLabel.AutoSize = true;
            dataDirectoryLabel.Location = new Point(11, 220);
            dataDirectoryLabel.Name = "dataDirectoryLabel";
            dataDirectoryLabel.Size = new Size(424, 20);
            dataDirectoryLabel.TabIndex = 9;
            dataDirectoryLabel.Text = "Data Directory (settings, mappings, sync state, logs)";
            // 
            // SpesnetDomainTextBox
            // 
            SpesnetDomainTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            SpesnetDomainTextBox.Location = new Point(11, 190);
            SpesnetDomainTextBox.Name = "SpesnetDomainTextBox";
            SpesnetDomainTextBox.Size = new Size(1516, 27);
            SpesnetDomainTextBox.TabIndex = 8;
            // 
            // spesnetDomainLabel
            // 
            spesnetDomainLabel.AutoSize = true;
            spesnetDomainLabel.Location = new Point(11, 167);
            spesnetDomainLabel.Name = "spesnetDomainLabel";
            spesnetDomainLabel.Size = new Size(118, 20);
            spesnetDomainLabel.TabIndex = 7;
            spesnetDomainLabel.Text = "Spesnet Domain";
            // 
            // SpesnetPasswordTextBox
            // 
            SpesnetPasswordTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            SpesnetPasswordTextBox.Location = new Point(11, 137);
            SpesnetPasswordTextBox.Name = "SpesnetPasswordTextBox";
            SpesnetPasswordTextBox.PasswordChar = '*';
            SpesnetPasswordTextBox.Size = new Size(1516, 27);
            SpesnetPasswordTextBox.TabIndex = 6;
            // 
            // spesnetPasswordLabel
            // 
            spesnetPasswordLabel.AutoSize = true;
            spesnetPasswordLabel.Location = new Point(11, 114);
            spesnetPasswordLabel.Name = "spesnetPasswordLabel";
            spesnetPasswordLabel.Size = new Size(126, 20);
            spesnetPasswordLabel.TabIndex = 5;
            spesnetPasswordLabel.Text = "Spesnet Password";
            // 
            // SpesnetUsernameTextBox
            // 
            SpesnetUsernameTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            SpesnetUsernameTextBox.Location = new Point(11, 84);
            SpesnetUsernameTextBox.Name = "SpesnetUsernameTextBox";
            SpesnetUsernameTextBox.Size = new Size(1516, 27);
            SpesnetUsernameTextBox.TabIndex = 4;
            // 
            // spesnetUsernameLabel
            // 
            spesnetUsernameLabel.AutoSize = true;
            spesnetUsernameLabel.Location = new Point(11, 61);
            spesnetUsernameLabel.Name = "spesnetUsernameLabel";
            spesnetUsernameLabel.Size = new Size(131, 20);
            spesnetUsernameLabel.TabIndex = 3;
            spesnetUsernameLabel.Text = "Spesnet Username";
            // 
            // TogglApiTokenTextBox
            // 
            TogglApiTokenTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            TogglApiTokenTextBox.Location = new Point(11, 31);
            TogglApiTokenTextBox.Name = "TogglApiTokenTextBox";
            TogglApiTokenTextBox.Size = new Size(1516, 27);
            TogglApiTokenTextBox.TabIndex = 2;
            // 
            // togglApiTokenLabel
            // 
            togglApiTokenLabel.AutoSize = true;
            togglApiTokenLabel.Location = new Point(11, 8);
            togglApiTokenLabel.Name = "togglApiTokenLabel";
            togglApiTokenLabel.Size = new Size(116, 20);
            togglApiTokenLabel.TabIndex = 1;
            togglApiTokenLabel.Text = "Toggl API Token";
            // 
            // StatusColumn
            // 
            StatusColumn.HeaderText = "Status";
            StatusColumn.MinimumWidth = 6;
            StatusColumn.Name = "StatusColumn";
            StatusColumn.Width = 90;
            // 
            // TogglClientColumn
            // 
            TogglClientColumn.HeaderText = "Toggl Client";
            TogglClientColumn.MinimumWidth = 6;
            TogglClientColumn.Name = "TogglClientColumn";
            TogglClientColumn.Width = 150;
            // 
            // TogglProjectColumn
            // 
            TogglProjectColumn.HeaderText = "Toggl Project";
            TogglProjectColumn.MinimumWidth = 6;
            TogglProjectColumn.Name = "TogglProjectColumn";
            TogglProjectColumn.Width = 160;
            // 
            // SpesnetProjectColumn
            // 
            SpesnetProjectColumn.HeaderText = "Spesnet Project";
            SpesnetProjectColumn.MinimumWidth = 6;
            SpesnetProjectColumn.Name = "SpesnetProjectColumn";
            SpesnetProjectColumn.Width = 420;
            // 
            // SpesnetClientColumn
            // 
            SpesnetClientColumn.HeaderText = "Spesnet Client";
            SpesnetClientColumn.MinimumWidth = 6;
            SpesnetClientColumn.Name = "SpesnetClientColumn";
            SpesnetClientColumn.Width = 140;
            // 
            // SpesnetWorkTaskColumn
            // 
            SpesnetWorkTaskColumn.HeaderText = "Spesnet Work Task";
            SpesnetWorkTaskColumn.MinimumWidth = 6;
            SpesnetWorkTaskColumn.Name = "SpesnetWorkTaskColumn";
            SpesnetWorkTaskColumn.Width = 480;
            // 
            // SyncForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1550, 561);
            Controls.Add(mainTabControl);
            Controls.Add(topPanel);
            MinimumSize = new Size(1550, 600);
            Name = "SyncForm";
            Text = "Toggl to Spesnet Sync";
            FormClosing += SyncForm_FormClosing;
            Load += SyncForm_Load;
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            mainTabControl.ResumeLayout(false);
            logTabPage.ResumeLayout(false);
            logTabPage.PerformLayout();
            mappingTabPage.ResumeLayout(false);
            mappingTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)MappingGrid).EndInit();
            settingsTabPage.ResumeLayout(false);
            settingsTabPage.PerformLayout();
            ResumeLayout(false);
        }

        private Panel topPanel;
        private Label syncFromLabel;
        private Button StartSyncButton;
        private DateTimePicker StartSyncDateTimeControl;
        private Label StatusLabel;
        private CheckBox UseMockSpesnetCheckBox;
        private TabControl mainTabControl;
        private TabPage logTabPage;
        private TextBox LogTextBox;
        private Button SaveMappingsButton;
        private Label MappingDirtyLabel;
        private Button RefreshSpesnetButton;
        private Button RefreshTogglButton;
        private TabPage mappingTabPage;
        private DataGridView MappingGrid;
        private TabPage settingsTabPage;
        private Label togglApiTokenLabel;
        private TextBox TogglApiTokenTextBox;
        private Label spesnetUsernameLabel;
        private TextBox SpesnetUsernameTextBox;
        private Label spesnetPasswordLabel;
        private TextBox SpesnetPasswordTextBox;
        private Label spesnetDomainLabel;
        private TextBox SpesnetDomainTextBox;
        private Label dataDirectoryLabel;
        private TextBox DataDirectoryTextBox;
        private Button SaveSettingsButton;
        private DataGridViewComboBoxColumn StatusColumn;
        private DataGridViewComboBoxColumn TogglClientColumn;
        private DataGridViewComboBoxColumn TogglProjectColumn;
        private DataGridViewComboBoxColumn SpesnetProjectColumn;
        private DataGridViewComboBoxColumn SpesnetClientColumn;
        private DataGridViewComboBoxColumn SpesnetWorkTaskColumn;
    }
}
