using SpesnetTogglSync.Models;
using SpesnetTogglSync.Services;

namespace SpesnetTogglSync;

public partial class SyncForm : Form
{
    private readonly ConfigService _configService = new();
    private readonly FileLogger _logger = new();
    private AppSettings _settings = new();
    private SyncState _syncState = new();
    private MappingsFile _mappingsFile = new();
    private SpesnetReferenceCache _referenceCache = new();
    private long _togglUserId;
    private long _workspaceId;
    private List<TogglClient> _togglClients = [];
    private List<TogglProject> _togglProjects = [];
    private CancellationTokenSource? _syncCancellation;

    public SyncForm()
    {
        InitializeComponent();
        InitializeMappingGrids();
        _logger.LogWritten += (_, line) => AppendLogLine(line);
    }

    private async void SyncForm_Load(object sender, EventArgs e)
    {
        _settings = _configService.LoadSettings();
        _syncState = _configService.LoadSyncState();
        _mappingsFile = _configService.LoadMappings();
        _referenceCache = _settings.SpesnetReferenceCache ?? new SpesnetReferenceCache();

        UseMockSpesnetCheckBox.Checked = _settings.UseMockSpesnet;
        LoadSettingsIntoUi();

        var watermark = _syncState.LastSyncedStartTime?.ToLocalTime()
            ?? DateTime.Now.AddDays(-30);
        StartSyncDateTimeControl.Value = watermark;

        LogTextBox.Text = _logger.ReadTodayLog();
        await InitializeTogglContextAsync();
        await EnsureReferenceDataLoadedAsync();
        LoadMappingsIntoUi();
    }

    private async Task EnsureReferenceDataLoadedAsync()
    {
        if (_referenceCache.Projects.Count > 0 && _referenceCache.WorkTasks.Count > 0)
        {
            return;
        }

        try
        {
            using var spesnetClient = CreateSpesnetClient();
            _referenceCache = await spesnetClient.RefreshReferenceDataAsync();
            _settings.SpesnetReferenceCache = _referenceCache;
            _configService.SaveSettings(_settings);
            _logger.Info("Spesnet reference data loaded automatically on startup.");
        }
        catch (Exception ex)
        {
            _logger.Warn($"Could not auto-load Spesnet reference data: {ex.Message}");
        }
    }

    private void SyncForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        SaveMappingsFromUi();
        _configService.SaveMappings(_mappingsFile);
        _syncState.LastSyncedStartTime = StartSyncDateTimeControl.Value.ToUniversalTime();
        _configService.SaveSyncState(_syncState);
    }

    private void LoadSettingsIntoUi()
    {
        TogglApiTokenTextBox.Text = _settings.TogglApiToken;
        SpesnetUsernameTextBox.Text = _settings.SpesnetUsername;
        SpesnetPasswordTextBox.Text = _settings.SpesnetPassword;
        SpesnetDomainTextBox.Text = _settings.SpesnetDomain;
        AspNetUserIdTextBox.Text = _settings.AspNetUserId;
    }

    private void SaveSettingsFromUi()
    {
        _settings.TogglApiToken = TogglApiTokenTextBox.Text.Trim();
        _settings.SpesnetUsername = SpesnetUsernameTextBox.Text.Trim();
        _settings.SpesnetPassword = SpesnetPasswordTextBox.Text;
        _settings.SpesnetDomain = SpesnetDomainTextBox.Text.Trim();
        _settings.AspNetUserId = AspNetUserIdTextBox.Text.Trim();
        _settings.UseMockSpesnet = UseMockSpesnetCheckBox.Checked;
        _settings.SpesnetReferenceCache = _referenceCache;
        _configService.SaveSettings(_settings);
    }

    private async Task InitializeTogglContextAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.TogglApiToken))
        {
            StatusLabel.Text = "Configure Toggl API token in Settings.";
            return;
        }

        try
        {
            using var togglClient = CreateTogglClient();
            var me = await togglClient.GetMeAsync();
            _togglUserId = me.Id;
            _workspaceId = _settings.TogglWorkspaceId > 0 ? _settings.TogglWorkspaceId : me.DefaultWorkspaceId;
            if (_settings.TogglWorkspaceId != _workspaceId)
            {
                _settings.TogglWorkspaceId = _workspaceId;
                _configService.SaveSettings(_settings);
            }

            await RefreshTogglClientsAsync(togglClient);
            StatusLabel.Text = $"Ready. Toggl user {_togglUserId}, workspace {_workspaceId}.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Failed to connect to Toggl.";
            _logger.Error(ex.Message);
            MessageBox.Show(this, ex.Message, "Toggl Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RefreshTogglClientsAsync(ITogglClient? togglClient = null)
    {
        var disposeClient = togglClient == null;
        togglClient ??= CreateTogglClient();
        try
        {
            _togglClients = (await togglClient.GetClientsAsync(_workspaceId)).OrderBy(c => c.Name).ToList();
            _togglProjects = (await togglClient.GetProjectsAsync(_workspaceId)).OrderBy(p => p.Name).ToList();
            PopulateTogglClientCheckList();
            RefreshMappingGridSources();
        }
        finally
        {
            if (disposeClient && togglClient is TogglApiClient concrete)
            {
                concrete.Dispose();
            }
        }
    }

    private void PopulateTogglClientCheckList()
    {
        var userMappings = GetCurrentUserMappings();
        TogglClientsCheckedListBox.Items.Clear();

        foreach (var client in _togglClients)
        {
            var existing = userMappings.SelectedClients.FirstOrDefault(c => c.ClientId == client.Id);
            var isChecked = existing?.IsSelected ?? false;
            TogglClientsCheckedListBox.Items.Add(client, isChecked);
        }
    }

    private UserMappings GetCurrentUserMappings()
    {
        if (_togglUserId == 0)
        {
            _togglUserId = 1;
        }

        return _configService.GetOrCreateUserMappings(_mappingsFile, _togglUserId);
    }

    private void LoadMappingsIntoUi()
    {
        var userMappings = GetCurrentUserMappings();
        ClientMappingGrid.Rows.Clear();
        foreach (var mapping in userMappings.ClientMappings)
        {
            ClientMappingGrid.Rows.Add(mapping.TogglClientName, mapping.SpesnetProjectId, mapping.SpesnetClientId);
            var row = ClientMappingGrid.Rows[^1];
            row.Tag = mapping;
        }

        ProjectMappingGrid.Rows.Clear();
        foreach (var mapping in userMappings.ProjectMappings)
        {
            ProjectMappingGrid.Rows.Add(mapping.TogglProjectName, mapping.SpesnetWorkTaskId);
            ProjectMappingGrid.Rows[^1].Tag = mapping;
        }

        RefreshMappingGridSources();
    }

    private void SaveMappingsFromUi()
    {
        var userMappings = GetCurrentUserMappings();

        userMappings.SelectedClients = [];
        foreach (TogglClient client in TogglClientsCheckedListBox.CheckedItems)
        {
            userMappings.SelectedClients.Add(new SelectedTogglClient
            {
                ClientId = client.Id,
                ClientName = client.Name,
                IsSelected = true
            });
        }

        userMappings.ClientMappings = [];
        foreach (DataGridViewRow row in ClientMappingGrid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var togglClientName = Convert.ToString(row.Cells[TogglClientColumn.Index].Value) ?? string.Empty;
            var togglClient = _togglClients.FirstOrDefault(c =>
                string.Equals(c.Name, togglClientName, StringComparison.OrdinalIgnoreCase));
            var projectId = GetSelectedId(row.Cells[SpesnetProjectColumn.Index].Value);
            var clientId = GetSelectedId(row.Cells[SpesnetClientColumn.Index].Value);
            if (string.IsNullOrWhiteSpace(togglClientName) || projectId == 0 || clientId == 0)
            {
                continue;
            }

            userMappings.ClientMappings.Add(new ClientMapping
            {
                TogglClientId = togglClient?.Id ?? 0,
                TogglClientName = togglClientName,
                SpesnetProjectId = projectId,
                SpesnetProjectName = GetProjectName(projectId),
                SpesnetClientId = clientId,
                SpesnetClientName = GetClientName(projectId, clientId)
            });
        }

        userMappings.ProjectMappings = [];
        foreach (DataGridViewRow row in ProjectMappingGrid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var togglProjectName = Convert.ToString(row.Cells[TogglProjectColumn.Index].Value) ?? string.Empty;
            var togglProject = _togglProjects.FirstOrDefault(p =>
                string.Equals(p.Name, togglProjectName, StringComparison.OrdinalIgnoreCase));
            var workTaskId = GetSelectedId(row.Cells[SpesnetWorkTaskColumn.Index].Value);
            if (string.IsNullOrWhiteSpace(togglProjectName) || workTaskId == 0)
            {
                continue;
            }

            userMappings.ProjectMappings.Add(new ProjectMapping
            {
                TogglProjectId = togglProject?.Id ?? 0,
                TogglProjectName = togglProjectName,
                SpesnetWorkTaskId = workTaskId,
                SpesnetWorkTaskName = GetWorkTaskName(workTaskId)
            });
        }
    }

    private void RefreshMappingGridSources()
    {
        var togglClientNames = _togglClients.Select(c => c.Name).Distinct().OrderBy(n => n).ToArray();
        var togglProjectNames = _togglProjects.Select(p => p.Name).Distinct().OrderBy(n => n).ToArray();
        var projectItems = _referenceCache.Projects
            .Select(p => new ComboItem(p.Id, p.ProjName))
            .OrderBy(p => p.Name)
            .ToArray();
        var workTaskItems = _referenceCache.WorkTasks
            .Select(w => new ComboItem(w.Id, $"{w.Description} ({w.Code})"))
            .OrderBy(w => w.Name)
            .ToArray();

        SetComboColumnDataSource(TogglClientColumn, togglClientNames);
        SetComboColumnDataSource(TogglProjectColumn, togglProjectNames);
        SetComboColumnDataSource(SpesnetProjectColumn, projectItems);
        SetComboColumnDataSource(SpesnetWorkTaskColumn, workTaskItems);

        foreach (DataGridViewRow row in ClientMappingGrid.Rows)
        {
            if (!row.IsNewRow)
            {
                UpdateClientComboForRow(row);
            }
        }
    }

    private void SetComboColumnDataSource(DataGridViewComboBoxColumn column, object dataSource)
    {
        column.DataSource = null;
        column.DataSource = dataSource;
        if (dataSource is ComboItem[] comboItems)
        {
            column.DisplayMember = nameof(ComboItem.Name);
            column.ValueMember = nameof(ComboItem.Id);
        }
    }

    private void InitializeMappingGrids()
    {
        ClientMappingGrid.AutoGenerateColumns = false;
        ClientMappingGrid.Columns.Add(TogglClientColumn);
        ClientMappingGrid.Columns.Add(SpesnetProjectColumn);
        ClientMappingGrid.Columns.Add(SpesnetClientColumn);

        ProjectMappingGrid.AutoGenerateColumns = false;
        ProjectMappingGrid.Columns.Add(TogglProjectColumn);
        ProjectMappingGrid.Columns.Add(SpesnetWorkTaskColumn);

        ClientMappingGrid.EditingControlShowing += ClientMappingGrid_EditingControlShowing;
        ClientMappingGrid.CellValueChanged += ClientMappingGrid_CellValueChanged;
        ClientMappingGrid.CurrentCellDirtyStateChanged += MappingGrid_CurrentCellDirtyStateChanged;
    }

    private void MappingGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (sender is DataGridView grid && grid.IsCurrentCellDirty)
        {
            grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void ClientMappingGrid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (ClientMappingGrid.CurrentCell?.OwningColumn != SpesnetProjectColumn)
        {
            return;
        }

        if (e.Control is ComboBox comboBox)
        {
            comboBox.SelectedIndexChanged -= SpesnetProjectCombo_SelectedIndexChanged;
            comboBox.SelectedIndexChanged += SpesnetProjectCombo_SelectedIndexChanged;
        }
    }

    private void SpesnetProjectCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (ClientMappingGrid.CurrentRow == null)
        {
            return;
        }

        UpdateClientComboForRow(ClientMappingGrid.CurrentRow);
    }

    private void ClientMappingGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != SpesnetProjectColumn.Index)
        {
            return;
        }

        UpdateClientComboForRow(ClientMappingGrid.Rows[e.RowIndex]);
    }

    private void UpdateClientComboForRow(DataGridViewRow row)
    {
        var projectId = GetSelectedId(row.Cells[SpesnetProjectColumn.Index].Value);
        var clients = projectId > 0 && _referenceCache.ClientsByProject.TryGetValue(projectId, out var projectClients)
            ? projectClients.Select(c => new ComboItem(c.Id, c.Name)).ToArray()
            : Array.Empty<ComboItem>();

        var clientCell = (DataGridViewComboBoxCell)row.Cells[SpesnetClientColumn.Index];
        clientCell.DataSource = clients;
        clientCell.DisplayMember = nameof(ComboItem.Name);
        clientCell.ValueMember = nameof(ComboItem.Id);

        if (clients.Length == 0)
        {
            clientCell.Value = null;
            return;
        }

        var currentId = GetSelectedId(clientCell.Value);
        if (clients.All(c => c.Id != currentId))
        {
            clientCell.Value = clients[0].Id;
        }
    }

    private async void RefreshTogglButton_Click(object sender, EventArgs e)
    {
        try
        {
            SetUiEnabled(false);
            await RefreshTogglClientsAsync();
            _logger.Info("Toggl clients and projects refreshed.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Toggl Refresh Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetUiEnabled(true);
        }
    }

    private async void RefreshSpesnetButton_Click(object sender, EventArgs e)
    {
        try
        {
            SetUiEnabled(false);
            SaveSettingsFromUi();
            using var spesnetClient = CreateSpesnetClient();
            _referenceCache = await spesnetClient.RefreshReferenceDataAsync();
            _settings.SpesnetReferenceCache = _referenceCache;
            _configService.SaveSettings(_settings);
            RefreshMappingGridSources();
            _logger.Info("Spesnet reference data refreshed.");
            StatusLabel.Text = $"Spesnet reference loaded: {_referenceCache.Projects.Count} projects, {_referenceCache.WorkTasks.Count} work tasks.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Spesnet Refresh Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetUiEnabled(true);
        }
    }

    private void SaveSettingsButton_Click(object sender, EventArgs e)
    {
        SaveSettingsFromUi();
        _logger.Info("Settings saved.");
        StatusLabel.Text = "Settings saved.";
    }

    private void UseMockSpesnetCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        _settings.UseMockSpesnet = UseMockSpesnetCheckBox.Checked;
        _configService.SaveSettings(_settings);
    }

    private async void StartSyncButton_Click(object sender, EventArgs e)
    {
        if (_syncCancellation != null)
        {
            return;
        }

        SaveSettingsFromUi();
        SaveMappingsFromUi();
        _configService.SaveMappings(_mappingsFile);

        if (string.IsNullOrWhiteSpace(_settings.TogglApiToken))
        {
            MessageBox.Show(this, "Configure your Toggl API token on the Settings tab.", "Missing Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_referenceCache.Projects.Count == 0 || _referenceCache.WorkTasks.Count == 0)
        {
            MessageBox.Show(this, "Refresh Spesnet reference data before syncing.", "Missing Reference Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (TogglClientsCheckedListBox.CheckedItems.Count == 0)
        {
            MessageBox.Show(this, "Select at least one Toggl client to sync on the Toggl Clients tab.", "No Clients Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _syncCancellation = new CancellationTokenSource();
        try
        {
            SetUiEnabled(false);
            StatusLabel.Text = "Sync in progress...";

            using var togglClient = CreateTogglClient();
            using var spesnetClient = CreateSpesnetClient();
            var syncService = new SyncService(togglClient, spesnetClient, _configService, _logger);
            syncService.Progress += SyncService_Progress;

            var watermark = StartSyncDateTimeControl.Value.ToUniversalTime();
            var result = await syncService.SyncAsync(
                watermark,
                GetCurrentUserMappings(),
                _referenceCache,
                _syncCancellation.Token);

            if (result.LastSyncedStartTime.HasValue)
            {
                StartSyncDateTimeControl.Value = result.LastSyncedStartTime.Value.ToLocalTime();
                _syncState.LastSyncedStartTime = result.LastSyncedStartTime;
                _configService.SaveSyncState(_syncState);
            }

            StatusLabel.Text = result.Message;
            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, "Sync Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show(this, result.Message, "Sync Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            StatusLabel.Text = "Sync failed.";
            MessageBox.Show(this, ex.Message, "Sync Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _syncCancellation?.Dispose();
            _syncCancellation = null;
            SetUiEnabled(true);
        }
    }

    private void SyncService_Progress(object? sender, SyncProgressEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SyncService_Progress(sender, e));
            return;
        }

        StatusLabel.Text = e.Message;
        if (e.UpdatedWatermark.HasValue)
        {
            StartSyncDateTimeControl.Value = e.UpdatedWatermark.Value.ToLocalTime();
        }
    }

    private TogglApiClient CreateTogglClient() => new(_settings.TogglApiToken, _logger);

    private ISpesnetTimekeepingClient CreateSpesnetClient() =>
        _settings.UseMockSpesnet
            ? new MockSpesnetTimekeepingClient(_logger)
            : new SpesnetTimekeepingClient(_settings, _logger);

    private void AppendLogLine(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLogLine(line));
            return;
        }

        LogTextBox.AppendText(line + Environment.NewLine);
    }

    private void SetUiEnabled(bool enabled)
    {
        StartSyncButton.Enabled = enabled;
        RefreshTogglButton.Enabled = enabled;
        RefreshSpesnetButton.Enabled = enabled;
        SaveSettingsButton.Enabled = enabled;
    }

    private static int GetSelectedId(object? value)
    {
        return value switch
        {
            ComboItem item => item.Id,
            int id => id,
            long longId => (int)longId,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 0
        };
    }

    private string GetProjectName(int projectId) =>
        _referenceCache.Projects.FirstOrDefault(p => p.Id == projectId)?.ProjName ?? string.Empty;

    private string GetClientName(int projectId, int clientId) =>
        _referenceCache.ClientsByProject.TryGetValue(projectId, out var clients)
            ? clients.FirstOrDefault(c => c.Id == clientId)?.Name ?? string.Empty
            : string.Empty;

    private string GetWorkTaskName(int workTaskId) =>
        _referenceCache.WorkTasks.FirstOrDefault(w => w.Id == workTaskId)?.Description ?? string.Empty;

    private sealed class ComboItem(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
    }
}
