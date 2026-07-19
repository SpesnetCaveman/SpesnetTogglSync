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
            togglClientsTabPage = new TabPage();
            RefreshTogglButton = new Button();
            TogglClientsCheckedListBox = new CheckedListBox();
            clientMappingTabPage = new TabPage();
            ClientMappingGrid = new DataGridView();
            projectMappingTabPage = new TabPage();
            ProjectMappingGrid = new DataGridView();
            settingsTabPage = new TabPage();
            SaveSettingsButton = new Button();
            RefreshSpesnetButton = new Button();
            AspNetUserIdTextBox = new TextBox();
            aspNetUserIdLabel = new Label();
            SpesnetDomainTextBox = new TextBox();
            spesnetDomainLabel = new Label();
            SpesnetPasswordTextBox = new TextBox();
            spesnetPasswordLabel = new Label();
            SpesnetUsernameTextBox = new TextBox();
            spesnetUsernameLabel = new Label();
            TogglApiTokenTextBox = new TextBox();
            togglApiTokenLabel = new Label();
            TogglClientColumn = new DataGridViewComboBoxColumn();
            SpesnetProjectColumn = new DataGridViewComboBoxColumn();
            SpesnetClientColumn = new DataGridViewComboBoxColumn();
            TogglProjectColumn = new DataGridViewComboBoxColumn();
            SpesnetWorkTaskColumn = new DataGridViewComboBoxColumn();
            topPanel.SuspendLayout();
            mainTabControl.SuspendLayout();
            logTabPage.SuspendLayout();
            togglClientsTabPage.SuspendLayout();
            clientMappingTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ClientMappingGrid).BeginInit();
            projectMappingTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ProjectMappingGrid).BeginInit();
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
            topPanel.Size = new Size(984, 72);
            topPanel.TabIndex = 0;
            // 
            // syncFromLabel
            // 
            syncFromLabel.AutoSize = true;
            syncFromLabel.Location = new Point(15, 18);
            syncFromLabel.Name = "syncFromLabel";
            syncFromLabel.Size = new Size(73, 20);
            syncFromLabel.TabIndex = 0;
            syncFromLabel.Text = "Sync from";
            // 
            // StartSyncDateTimeControl
            // 
            StartSyncDateTimeControl.CustomFormat = "yyyy-MM-dd HH:mm";
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
            StatusLabel.Size = new Size(700, 20);
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
            UseMockSpesnetCheckBox.Size = new Size(152, 24);
            UseMockSpesnetCheckBox.TabIndex = 4;
            UseMockSpesnetCheckBox.Text = "Use mock Spesnet";
            UseMockSpesnetCheckBox.UseVisualStyleBackColor = true;
            UseMockSpesnetCheckBox.CheckedChanged += UseMockSpesnetCheckBox_CheckedChanged;
            // 
            // mainTabControl
            // 
            mainTabControl.Controls.Add(logTabPage);
            mainTabControl.Controls.Add(togglClientsTabPage);
            mainTabControl.Controls.Add(clientMappingTabPage);
            mainTabControl.Controls.Add(projectMappingTabPage);
            mainTabControl.Controls.Add(settingsTabPage);
            mainTabControl.Dock = DockStyle.Fill;
            mainTabControl.Location = new Point(0, 72);
            mainTabControl.Name = "mainTabControl";
            mainTabControl.SelectedIndex = 0;
            mainTabControl.Size = new Size(984, 489);
            mainTabControl.TabIndex = 1;
            // 
            // logTabPage
            // 
            logTabPage.Controls.Add(LogTextBox);
            logTabPage.Location = new Point(4, 29);
            logTabPage.Name = "logTabPage";
            logTabPage.Padding = new Padding(8);
            logTabPage.Size = new Size(976, 456);
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
            LogTextBox.Size = new Size(960, 440);
            LogTextBox.TabIndex = 0;
            // 
            // togglClientsTabPage
            // 
            togglClientsTabPage.Controls.Add(RefreshTogglButton);
            togglClientsTabPage.Controls.Add(TogglClientsCheckedListBox);
            togglClientsTabPage.Location = new Point(4, 29);
            togglClientsTabPage.Name = "togglClientsTabPage";
            togglClientsTabPage.Padding = new Padding(8);
            togglClientsTabPage.Size = new Size(976, 456);
            togglClientsTabPage.TabIndex = 1;
            togglClientsTabPage.Text = "Toggl Clients";
            togglClientsTabPage.UseVisualStyleBackColor = true;
            // 
            // RefreshTogglButton
            // 
            RefreshTogglButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            RefreshTogglButton.Location = new Point(812, 11);
            RefreshTogglButton.Name = "RefreshTogglButton";
            RefreshTogglButton.Size = new Size(153, 29);
            RefreshTogglButton.TabIndex = 1;
            RefreshTogglButton.Text = "Refresh from Toggl";
            RefreshTogglButton.UseVisualStyleBackColor = true;
            RefreshTogglButton.Click += RefreshTogglButton_Click;
            // 
            // TogglClientsCheckedListBox
            // 
            TogglClientsCheckedListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            TogglClientsCheckedListBox.CheckOnClick = true;
            TogglClientsCheckedListBox.FormattingEnabled = true;
            TogglClientsCheckedListBox.Location = new Point(11, 46);
            TogglClientsCheckedListBox.Name = "TogglClientsCheckedListBox";
            TogglClientsCheckedListBox.Size = new Size(954, 378);
            TogglClientsCheckedListBox.TabIndex = 0;
            // 
            // clientMappingTabPage
            // 
            clientMappingTabPage.Controls.Add(ClientMappingGrid);
            clientMappingTabPage.Location = new Point(4, 29);
            clientMappingTabPage.Name = "clientMappingTabPage";
            clientMappingTabPage.Padding = new Padding(8);
            clientMappingTabPage.Size = new Size(976, 456);
            clientMappingTabPage.TabIndex = 2;
            clientMappingTabPage.Text = "Client Mapping";
            clientMappingTabPage.UseVisualStyleBackColor = true;
            // 
            // ClientMappingGrid
            // 
            ClientMappingGrid.Dock = DockStyle.Fill;
            ClientMappingGrid.Location = new Point(8, 8);
            ClientMappingGrid.Name = "ClientMappingGrid";
            ClientMappingGrid.RowHeadersWidth = 51;
            ClientMappingGrid.Size = new Size(960, 440);
            ClientMappingGrid.TabIndex = 0;
            // 
            // projectMappingTabPage
            // 
            projectMappingTabPage.Controls.Add(ProjectMappingGrid);
            projectMappingTabPage.Location = new Point(4, 29);
            projectMappingTabPage.Name = "projectMappingTabPage";
            projectMappingTabPage.Padding = new Padding(8);
            projectMappingTabPage.Size = new Size(976, 456);
            projectMappingTabPage.TabIndex = 3;
            projectMappingTabPage.Text = "Project Mapping";
            projectMappingTabPage.UseVisualStyleBackColor = true;
            // 
            // ProjectMappingGrid
            // 
            ProjectMappingGrid.Dock = DockStyle.Fill;
            ProjectMappingGrid.Location = new Point(8, 8);
            ProjectMappingGrid.Name = "ProjectMappingGrid";
            ProjectMappingGrid.RowHeadersWidth = 51;
            ProjectMappingGrid.Size = new Size(960, 440);
            ProjectMappingGrid.TabIndex = 0;
            // 
            // settingsTabPage
            // 
            settingsTabPage.Controls.Add(SaveSettingsButton);
            settingsTabPage.Controls.Add(RefreshSpesnetButton);
            settingsTabPage.Controls.Add(AspNetUserIdTextBox);
            settingsTabPage.Controls.Add(aspNetUserIdLabel);
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
            settingsTabPage.Size = new Size(976, 456);
            settingsTabPage.TabIndex = 4;
            settingsTabPage.Text = "Settings";
            settingsTabPage.UseVisualStyleBackColor = true;
            // 
            // SaveSettingsButton
            // 
            SaveSettingsButton.Location = new Point(11, 286);
            SaveSettingsButton.Name = "SaveSettingsButton";
            SaveSettingsButton.Size = new Size(130, 29);
            SaveSettingsButton.TabIndex = 11;
            SaveSettingsButton.Text = "Save Settings";
            SaveSettingsButton.UseVisualStyleBackColor = true;
            SaveSettingsButton.Click += SaveSettingsButton_Click;
            // 
            // RefreshSpesnetButton
            // 
            RefreshSpesnetButton.Location = new Point(157, 286);
            RefreshSpesnetButton.Name = "RefreshSpesnetButton";
            RefreshSpesnetButton.Size = new Size(220, 29);
            RefreshSpesnetButton.TabIndex = 12;
            RefreshSpesnetButton.Text = "Refresh Spesnet Reference Data";
            RefreshSpesnetButton.UseVisualStyleBackColor = true;
            RefreshSpesnetButton.Click += RefreshSpesnetButton_Click;
            // 
            // AspNetUserIdTextBox
            // 
            AspNetUserIdTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            AspNetUserIdTextBox.Location = new Point(11, 243);
            AspNetUserIdTextBox.Name = "AspNetUserIdTextBox";
            AspNetUserIdTextBox.Size = new Size(950, 27);
            AspNetUserIdTextBox.TabIndex = 10;
            // 
            // aspNetUserIdLabel
            // 
            aspNetUserIdLabel.AutoSize = true;
            aspNetUserIdLabel.Location = new Point(11, 220);
            aspNetUserIdLabel.Name = "aspNetUserIdLabel";
            aspNetUserIdLabel.Size = new Size(99, 20);
            aspNetUserIdLabel.TabIndex = 9;
            aspNetUserIdLabel.Text = "AspNet User Id";
            // 
            // SpesnetDomainTextBox
            // 
            SpesnetDomainTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            SpesnetDomainTextBox.Location = new Point(11, 190);
            SpesnetDomainTextBox.Name = "SpesnetDomainTextBox";
            SpesnetDomainTextBox.Size = new Size(950, 27);
            SpesnetDomainTextBox.TabIndex = 8;
            // 
            // spesnetDomainLabel
            // 
            spesnetDomainLabel.AutoSize = true;
            spesnetDomainLabel.Location = new Point(11, 167);
            spesnetDomainLabel.Name = "spesnetDomainLabel";
            spesnetDomainLabel.Size = new Size(110, 20);
            spesnetDomainLabel.TabIndex = 7;
            spesnetDomainLabel.Text = "Spesnet Domain";
            // 
            // SpesnetPasswordTextBox
            // 
            SpesnetPasswordTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            SpesnetPasswordTextBox.Location = new Point(11, 137);
            SpesnetPasswordTextBox.Name = "SpesnetPasswordTextBox";
            SpesnetPasswordTextBox.PasswordChar = '*';
            SpesnetPasswordTextBox.Size = new Size(950, 27);
            SpesnetPasswordTextBox.TabIndex = 6;
            // 
            // spesnetPasswordLabel
            // 
            spesnetPasswordLabel.AutoSize = true;
            spesnetPasswordLabel.Location = new Point(11, 114);
            spesnetPasswordLabel.Name = "spesnetPasswordLabel";
            spesnetPasswordLabel.Size = new Size(120, 20);
            spesnetPasswordLabel.TabIndex = 5;
            spesnetPasswordLabel.Text = "Spesnet Password";
            // 
            // SpesnetUsernameTextBox
            // 
            SpesnetUsernameTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            SpesnetUsernameTextBox.Location = new Point(11, 84);
            SpesnetUsernameTextBox.Name = "SpesnetUsernameTextBox";
            SpesnetUsernameTextBox.Size = new Size(950, 27);
            SpesnetUsernameTextBox.TabIndex = 4;
            // 
            // spesnetUsernameLabel
            // 
            spesnetUsernameLabel.AutoSize = true;
            spesnetUsernameLabel.Location = new Point(11, 61);
            spesnetUsernameLabel.Name = "spesnetUsernameLabel";
            spesnetUsernameLabel.Size = new Size(125, 20);
            spesnetUsernameLabel.TabIndex = 3;
            spesnetUsernameLabel.Text = "Spesnet Username";
            // 
            // TogglApiTokenTextBox
            // 
            TogglApiTokenTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            TogglApiTokenTextBox.Location = new Point(11, 31);
            TogglApiTokenTextBox.Name = "TogglApiTokenTextBox";
            TogglApiTokenTextBox.Size = new Size(950, 27);
            TogglApiTokenTextBox.TabIndex = 2;
            // 
            // togglApiTokenLabel
            // 
            togglApiTokenLabel.AutoSize = true;
            togglApiTokenLabel.Location = new Point(11, 8);
            togglApiTokenLabel.Name = "togglApiTokenLabel";
            togglApiTokenLabel.Size = new Size(109, 20);
            togglApiTokenLabel.TabIndex = 1;
            togglApiTokenLabel.Text = "Toggl API Token";
            // 
            // TogglClientColumn
            // 
            TogglClientColumn.HeaderText = "Toggl Client";
            TogglClientColumn.MinimumWidth = 6;
            TogglClientColumn.Name = "TogglClientColumn";
            TogglClientColumn.Width = 250;
            // 
            // SpesnetProjectColumn
            // 
            SpesnetProjectColumn.HeaderText = "Spesnet Project";
            SpesnetProjectColumn.MinimumWidth = 6;
            SpesnetProjectColumn.Name = "SpesnetProjectColumn";
            SpesnetProjectColumn.Width = 300;
            // 
            // SpesnetClientColumn
            // 
            SpesnetClientColumn.HeaderText = "Spesnet Client";
            SpesnetClientColumn.MinimumWidth = 6;
            SpesnetClientColumn.Name = "SpesnetClientColumn";
            SpesnetClientColumn.Width = 250;
            // 
            // TogglProjectColumn
            // 
            TogglProjectColumn.HeaderText = "Toggl Project";
            TogglProjectColumn.MinimumWidth = 6;
            TogglProjectColumn.Name = "TogglProjectColumn";
            TogglProjectColumn.Width = 350;
            // 
            // SpesnetWorkTaskColumn
            // 
            SpesnetWorkTaskColumn.HeaderText = "Spesnet Work Task";
            SpesnetWorkTaskColumn.MinimumWidth = 6;
            SpesnetWorkTaskColumn.Name = "SpesnetWorkTaskColumn";
            SpesnetWorkTaskColumn.Width = 350;
            // 
            // SyncForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 561);
            Controls.Add(mainTabControl);
            Controls.Add(topPanel);
            MinimumSize = new Size(900, 600);
            Name = "SyncForm";
            Text = "Toggl to Spesnet Sync";
            FormClosing += SyncForm_FormClosing;
            Load += SyncForm_Load;
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            mainTabControl.ResumeLayout(false);
            logTabPage.ResumeLayout(false);
            logTabPage.PerformLayout();
            togglClientsTabPage.ResumeLayout(false);
            clientMappingTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)ClientMappingGrid).EndInit();
            projectMappingTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)ProjectMappingGrid).EndInit();
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
        private TabPage togglClientsTabPage;
        private CheckedListBox TogglClientsCheckedListBox;
        private Button RefreshTogglButton;
        private TabPage clientMappingTabPage;
        private DataGridView ClientMappingGrid;
        private TabPage projectMappingTabPage;
        private DataGridView ProjectMappingGrid;
        private TabPage settingsTabPage;
        private Label togglApiTokenLabel;
        private TextBox TogglApiTokenTextBox;
        private Label spesnetUsernameLabel;
        private TextBox SpesnetUsernameTextBox;
        private Label spesnetPasswordLabel;
        private TextBox SpesnetPasswordTextBox;
        private Label spesnetDomainLabel;
        private TextBox SpesnetDomainTextBox;
        private Label aspNetUserIdLabel;
        private TextBox AspNetUserIdTextBox;
        private Button SaveSettingsButton;
        private Button RefreshSpesnetButton;
        private DataGridViewComboBoxColumn TogglClientColumn;
        private DataGridViewComboBoxColumn SpesnetProjectColumn;
        private DataGridViewComboBoxColumn SpesnetClientColumn;
        private DataGridViewComboBoxColumn TogglProjectColumn;
        private DataGridViewComboBoxColumn SpesnetWorkTaskColumn;
    }
}
