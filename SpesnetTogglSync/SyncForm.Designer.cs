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
            CreateTogglReportButton = new Button();
            StatusLabel = new Label();
            UseMockSpesnetCheckBox = new CheckBox();
            mainTabControl = new TabControl();
            logTabPage = new TabPage();
            LogTextBox = new TextBox();
            mappingTabPage = new TabPage();
            MappingGrid = new DataGridView();
            SaveMappingsButton = new Button();
            MappingDirtyLabel = new Label();
            RefreshSpesnetButton = new Button();
            RefreshTogglButton = new Button();
            settingsTabPage = new TabPage();
            SaveSettingsButton = new Button();
            RunAtStartupCheckBox = new CheckBox();
            BrowseInvoiceTemplateButton = new Button();
            InvoiceTemplateTextBox = new TextBox();
            invoiceTemplateLabel = new Label();
            billingCycleNoteLabel = new Label();
            BrowseBillingReportDirectoryButton = new Button();
            BillingReportDirectoryTextBox = new TextBox();
            billingReportDirectoryLabel = new Label();
            DailySyncTimePicker = new DateTimePicker();
            dailySyncTimeLabel = new Label();
            BillingCycleStartDayNumeric = new NumericUpDown();
            billingCycleStartDayLabel = new Label();
            HourlyRateNumeric = new NumericUpDown();
            InvoiceNumberNumeric = new NumericUpDown();
            invoiceNumberLabel = new Label();
            hourlyRateLabel = new Label();
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
            CommentPrefixColumn = new DataGridViewTextBoxColumn();
            topPanel.SuspendLayout();
            mainTabControl.SuspendLayout();
            logTabPage.SuspendLayout();
            mappingTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)MappingGrid).BeginInit();
            settingsTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)BillingCycleStartDayNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)HourlyRateNumeric).BeginInit();
            ((System.ComponentModel.ISupportInitialize)InvoiceNumberNumeric).BeginInit();
            SuspendLayout();
            // 
            // topPanel
            // 
            topPanel.Controls.Add(syncFromLabel);
            topPanel.Controls.Add(StartSyncDateTimeControl);
            topPanel.Controls.Add(StartSyncButton);
            topPanel.Controls.Add(CreateTogglReportButton);
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
            StartSyncDateTimeControl.Size = new Size(191, 27);
            StartSyncDateTimeControl.TabIndex = 1;
            StartSyncDateTimeControl.ValueChanged += StartSyncDateTimeControl_ValueChanged;
            // 
            // StartSyncButton
            // 
            StartSyncButton.Location = new Point(302, 14);
            StartSyncButton.Name = "StartSyncButton";
            StartSyncButton.Size = new Size(110, 29);
            StartSyncButton.TabIndex = 2;
            StartSyncButton.Text = "Start Sync";
            StartSyncButton.UseVisualStyleBackColor = true;
            StartSyncButton.Click += StartSyncButton_Click;
            // 
            // CreateTogglReportButton
            // 
            CreateTogglReportButton.Location = new Point(422, 14);
            CreateTogglReportButton.Name = "CreateTogglReportButton";
            CreateTogglReportButton.Size = new Size(216, 29);
            CreateTogglReportButton.TabIndex = 5;
            CreateTogglReportButton.Text = "Create invoice and report";
            CreateTogglReportButton.UseVisualStyleBackColor = true;
            CreateTogglReportButton.Click += CreateTogglReportButton_Click;
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
            UseMockSpesnetCheckBox.Location = new Point(694, 16);
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
            mainTabControl.Size = new Size(1550, 641);
            mainTabControl.TabIndex = 1;
            // 
            // logTabPage
            // 
            logTabPage.Controls.Add(LogTextBox);
            logTabPage.Location = new Point(4, 29);
            logTabPage.Name = "logTabPage";
            logTabPage.Padding = new Padding(8);
            logTabPage.Size = new Size(1542, 608);
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
            LogTextBox.Size = new Size(1526, 592);
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
            MappingDirtyLabel.Size = new Size(187, 20);
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
            // settingsTabPage
            // 
            settingsTabPage.Controls.Add(SaveSettingsButton);
            settingsTabPage.Controls.Add(RunAtStartupCheckBox);
            settingsTabPage.Controls.Add(BrowseInvoiceTemplateButton);
            settingsTabPage.Controls.Add(InvoiceTemplateTextBox);
            settingsTabPage.Controls.Add(invoiceTemplateLabel);
            settingsTabPage.Controls.Add(billingCycleNoteLabel);
            settingsTabPage.Controls.Add(BrowseBillingReportDirectoryButton);
            settingsTabPage.Controls.Add(BillingReportDirectoryTextBox);
            settingsTabPage.Controls.Add(billingReportDirectoryLabel);
            settingsTabPage.Controls.Add(DailySyncTimePicker);
            settingsTabPage.Controls.Add(dailySyncTimeLabel);
            settingsTabPage.Controls.Add(BillingCycleStartDayNumeric);
            settingsTabPage.Controls.Add(billingCycleStartDayLabel);
            settingsTabPage.Controls.Add(HourlyRateNumeric);
            settingsTabPage.Controls.Add(InvoiceNumberNumeric);
            settingsTabPage.Controls.Add(invoiceNumberLabel);
            settingsTabPage.Controls.Add(hourlyRateLabel);
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
            SaveSettingsButton.Location = new Point(12, 556);
            SaveSettingsButton.Name = "SaveSettingsButton";
            SaveSettingsButton.Size = new Size(130, 29);
            SaveSettingsButton.TabIndex = 14;
            SaveSettingsButton.Text = "Save Settings";
            SaveSettingsButton.UseVisualStyleBackColor = true;
            SaveSettingsButton.Click += SaveSettingsButton_Click;
            // 
            // RunAtStartupCheckBox
            // 
            RunAtStartupCheckBox.AutoSize = true;
            RunAtStartupCheckBox.Location = new Point(12, 516);
            RunAtStartupCheckBox.Name = "RunAtStartupCheckBox";
            RunAtStartupCheckBox.Size = new Size(523, 24);
            RunAtStartupCheckBox.TabIndex = 13;
            RunAtStartupCheckBox.Text = "Start with Windows (notification area; daily sync runs after the time above)";
            RunAtStartupCheckBox.UseVisualStyleBackColor = true;
            // 
            // BrowseInvoiceTemplateButton
            // 
            BrowseInvoiceTemplateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BrowseInvoiceTemplateButton.Location = new Point(1396, 465);
            BrowseInvoiceTemplateButton.Name = "BrowseInvoiceTemplateButton";
            BrowseInvoiceTemplateButton.Size = new Size(130, 29);
            BrowseInvoiceTemplateButton.TabIndex = 19;
            BrowseInvoiceTemplateButton.Text = "Import...";
            BrowseInvoiceTemplateButton.UseVisualStyleBackColor = true;
            BrowseInvoiceTemplateButton.Click += BrowseInvoiceTemplateButton_Click;
            // 
            // InvoiceTemplateTextBox
            // 
            InvoiceTemplateTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            InvoiceTemplateTextBox.Location = new Point(11, 467);
            InvoiceTemplateTextBox.Name = "InvoiceTemplateTextBox";
            InvoiceTemplateTextBox.ReadOnly = true;
            InvoiceTemplateTextBox.Size = new Size(1370, 27);
            InvoiceTemplateTextBox.TabIndex = 18;
            // 
            // invoiceTemplateLabel
            // 
            invoiceTemplateLabel.AutoSize = true;
            invoiceTemplateLabel.Location = new Point(11, 444);
            invoiceTemplateLabel.Name = "invoiceTemplateLabel";
            invoiceTemplateLabel.Size = new Size(170, 20);
            invoiceTemplateLabel.TabIndex = 17;
            invoiceTemplateLabel.Text = "Invoice template (app copy)";
            // 
            // billingCycleNoteLabel
            // 
            billingCycleNoteLabel.Location = new Point(11, 336);
            billingCycleNoteLabel.Name = "billingCycleNoteLabel";
            billingCycleNoteLabel.Size = new Size(1500, 40);
            billingCycleNoteLabel.TabIndex = 12;
            billingCycleNoteLabel.Text = "Each cycle runs from this day through the day before it next month. 20 means the 20th through the 19th. The Toggl report for a finished cycle is saved after that cycle has fully synced.";
            // 
            // BrowseBillingReportDirectoryButton
            // 
            BrowseBillingReportDirectoryButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BrowseBillingReportDirectoryButton.Location = new Point(1396, 405);
            BrowseBillingReportDirectoryButton.Name = "BrowseBillingReportDirectoryButton";
            BrowseBillingReportDirectoryButton.Size = new Size(130, 29);
            BrowseBillingReportDirectoryButton.TabIndex = 16;
            BrowseBillingReportDirectoryButton.Text = "Browse...";
            BrowseBillingReportDirectoryButton.UseVisualStyleBackColor = true;
            BrowseBillingReportDirectoryButton.Click += BrowseBillingReportDirectoryButton_Click;
            // 
            // BillingReportDirectoryTextBox
            // 
            BillingReportDirectoryTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            BillingReportDirectoryTextBox.Location = new Point(11, 407);
            BillingReportDirectoryTextBox.Name = "BillingReportDirectoryTextBox";
            BillingReportDirectoryTextBox.Size = new Size(1370, 27);
            BillingReportDirectoryTextBox.TabIndex = 15;
            // 
            // billingReportDirectoryLabel
            // 
            billingReportDirectoryLabel.AutoSize = true;
            billingReportDirectoryLabel.Location = new Point(11, 384);
            billingReportDirectoryLabel.Name = "billingReportDirectoryLabel";
            billingReportDirectoryLabel.Size = new Size(387, 20);
            billingReportDirectoryLabel.TabIndex = 14;
            billingReportDirectoryLabel.Text = "Billing report folder (empty saves into the data directory)";
            // 
            // DailySyncTimePicker
            // 
            DailySyncTimePicker.CustomFormat = "HH:mm";
            DailySyncTimePicker.Format = DateTimePickerFormat.Custom;
            DailySyncTimePicker.Location = new Point(420, 301);
            DailySyncTimePicker.Name = "DailySyncTimePicker";
            DailySyncTimePicker.ShowUpDown = true;
            DailySyncTimePicker.Size = new Size(120, 27);
            DailySyncTimePicker.TabIndex = 13;
            DailySyncTimePicker.Value = new DateTime(2026, 1, 1, 7, 0, 0, 0);
            // 
            // dailySyncTimeLabel
            // 
            dailySyncTimeLabel.AutoSize = true;
            dailySyncTimeLabel.Location = new Point(420, 278);
            dailySyncTimeLabel.Name = "dailySyncTimeLabel";
            dailySyncTimeLabel.Size = new Size(157, 20);
            dailySyncTimeLabel.TabIndex = 11;
            dailySyncTimeLabel.Text = "Daily sync time (SAST)";
            // 
            // BillingCycleStartDayNumeric
            // 
            BillingCycleStartDayNumeric.Location = new Point(220, 301);
            BillingCycleStartDayNumeric.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
            BillingCycleStartDayNumeric.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            BillingCycleStartDayNumeric.Name = "BillingCycleStartDayNumeric";
            BillingCycleStartDayNumeric.Size = new Size(80, 27);
            BillingCycleStartDayNumeric.TabIndex = 12;
            BillingCycleStartDayNumeric.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // billingCycleStartDayLabel
            // 
            billingCycleStartDayLabel.AutoSize = true;
            billingCycleStartDayLabel.Location = new Point(220, 278);
            billingCycleStartDayLabel.Name = "billingCycleStartDayLabel";
            billingCycleStartDayLabel.Size = new Size(149, 20);
            billingCycleStartDayLabel.TabIndex = 11;
            billingCycleStartDayLabel.Text = "Billing cycle start day";
            // 
            // HourlyRateNumeric
            // 
            HourlyRateNumeric.DecimalPlaces = 2;
            HourlyRateNumeric.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            HourlyRateNumeric.Location = new Point(11, 301);
            HourlyRateNumeric.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            HourlyRateNumeric.Name = "HourlyRateNumeric";
            HourlyRateNumeric.Size = new Size(140, 27);
            HourlyRateNumeric.TabIndex = 11;
            HourlyRateNumeric.ThousandsSeparator = true;
            // 
            // invoiceNumberLabel
            // 
            invoiceNumberLabel.AutoSize = true;
            invoiceNumberLabel.Location = new Point(580, 278);
            invoiceNumberLabel.Name = "invoiceNumberLabel";
            invoiceNumberLabel.Size = new Size(220, 20);
            invoiceNumberLabel.TabIndex = 11;
            invoiceNumberLabel.Text = "Invoice number";
            // 
            // InvoiceNumberNumeric
            // 
            InvoiceNumberNumeric.Location = new Point(580, 301);
            InvoiceNumberNumeric.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            InvoiceNumberNumeric.Name = "InvoiceNumberNumeric";
            InvoiceNumberNumeric.Size = new Size(100, 27);
            InvoiceNumberNumeric.TabIndex = 14;
            // 
            // hourlyRateLabel
            // 
            hourlyRateLabel.AutoSize = true;
            hourlyRateLabel.Location = new Point(11, 278);
            hourlyRateLabel.Name = "hourlyRateLabel";
            hourlyRateLabel.Size = new Size(125, 20);
            hourlyRateLabel.TabIndex = 11;
            hourlyRateLabel.Text = "Hourly rate (ZAR)";
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
            dataDirectoryLabel.Size = new Size(350, 20);
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
            SpesnetWorkTaskColumn.Width = 400;
            // 
            // CommentPrefixColumn
            // 
            CommentPrefixColumn.HeaderText = "Comment Prefix";
            CommentPrefixColumn.MinimumWidth = 6;
            CommentPrefixColumn.Name = "CommentPrefixColumn";
            CommentPrefixColumn.Width = 140;
            // 
            // SyncForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1550, 713);
            Controls.Add(mainTabControl);
            Controls.Add(topPanel);
            MinimumSize = new Size(1550, 760);
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
            ((System.ComponentModel.ISupportInitialize)BillingCycleStartDayNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)HourlyRateNumeric).EndInit();
            ((System.ComponentModel.ISupportInitialize)InvoiceNumberNumeric).EndInit();
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
        private Label hourlyRateLabel;
        private NumericUpDown HourlyRateNumeric;
        private Label invoiceNumberLabel;
        private NumericUpDown InvoiceNumberNumeric;
        private Label billingCycleStartDayLabel;
        private NumericUpDown BillingCycleStartDayNumeric;
        private Label billingCycleNoteLabel;
        private Label dailySyncTimeLabel;
        private DateTimePicker DailySyncTimePicker;
        private Label billingReportDirectoryLabel;
        private TextBox BillingReportDirectoryTextBox;
        private Button BrowseBillingReportDirectoryButton;
        private Label invoiceTemplateLabel;
        private TextBox InvoiceTemplateTextBox;
        private Button BrowseInvoiceTemplateButton;
        private Button SaveSettingsButton;
        private Button CreateTogglReportButton;
        private CheckBox RunAtStartupCheckBox;
        private DataGridViewComboBoxColumn StatusColumn;
        private DataGridViewComboBoxColumn TogglClientColumn;
        private DataGridViewComboBoxColumn TogglProjectColumn;
        private DataGridViewComboBoxColumn SpesnetProjectColumn;
        private DataGridViewComboBoxColumn SpesnetClientColumn;
        private DataGridViewComboBoxColumn SpesnetWorkTaskColumn;
        private DataGridViewTextBoxColumn CommentPrefixColumn;
    }
}
