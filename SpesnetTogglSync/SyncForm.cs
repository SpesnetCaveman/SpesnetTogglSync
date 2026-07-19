using SpesnetTogglSync.Models;
using SpesnetTogglSync.Services;
using SpesnetTogglSync.SpesnetApi;
using SpesnetTogglSync.TogglApi;

namespace SpesnetTogglSync;

public partial class SyncForm : Form
{
    private static readonly string[] StatusDisplayValues =
    [
        nameof(EntryMappingStatus.Active),
        nameof(EntryMappingStatus.Ignore),
        nameof(EntryMappingStatus.New)
    ];

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
    }

    private void SaveSettingsFromUi()
    {
        _settings.TogglApiToken = TogglApiTokenTextBox.Text.Trim();
        _settings.SpesnetUsername = SpesnetUsernameTextBox.Text.Trim();
        _settings.SpesnetPassword = SpesnetPasswordTextBox.Text;
        _settings.SpesnetDomain = SpesnetDomainTextBox.Text.Trim();
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
            EnsureEntryMappingsFromToggl();
            RefreshMappingGridSources();
            LoadMappingsIntoUi();
        }
        finally
        {
            if (disposeClient)
            {
                togglClient.Dispose();
            }
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

    /// <summary>
    /// One-time: convert legacy SelectedClients checkboxes into Active/Ignore status,
    /// then ensure every live Toggl client+project has a mapping row.
    /// </summary>
    private void EnsureEntryMappingsFromToggl()
    {
        var userMappings = GetCurrentUserMappings();
        MigrateSelectedClientsToStatus(userMappings);

        var existingByKey = userMappings.EntryMappings
            .ToDictionary(MappingKey, StringComparer.OrdinalIgnoreCase);

        var next = new List<EntryMapping>();

        // Preserve client-level Ignore rows first.
        foreach (var mapping in userMappings.EntryMappings.Where(m => m.IsClientLevelIgnore))
        {
            next.Add(mapping);
        }

        var ignoredClientIds = next
            .Where(m => m.TogglClientId > 0)
            .Select(m => m.TogglClientId)
            .ToHashSet();
        var ignoredClientNames = next
            .Select(m => m.TogglClientName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var client in _togglClients)
        {
            if (ignoredClientIds.Contains(client.Id) || ignoredClientNames.Contains(client.Name))
            {
                continue;
            }

            var projects = _togglProjects.Where(p => p.ClientId == client.Id).ToList();
            foreach (var project in projects)
            {
                var key = MappingKey(client.Id, client.Name, project.Id, project.Name);
                if (existingByKey.TryGetValue(key, out var existing))
                {
                    existing.TogglClientId = client.Id;
                    existing.TogglClientName = client.Name;
                    existing.TogglProjectId = project.Id;
                    existing.TogglProjectName = project.Name;
                    next.Add(existing);
                }
                else
                {
                    next.Add(new EntryMapping
                    {
                        Status = EntryMappingStatus.New,
                        TogglClientId = client.Id,
                        TogglClientName = client.Name,
                        TogglProjectId = project.Id,
                        TogglProjectName = project.Name
                    });
                }
            }
        }

        // Keep orphan rows (removed from Toggl) so the user can Ignore/delete them explicitly.
        var keptKeys = next.Select(MappingKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var orphan in userMappings.EntryMappings)
        {
            var key = MappingKey(orphan);
            if (keptKeys.Contains(key) || orphan.IsClientLevelIgnore)
            {
                continue;
            }

            next.Add(orphan);
            keptKeys.Add(key);
        }

        userMappings.EntryMappings = next
            .OrderBy(m => m.TogglClientName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.IsClientLevelIgnore ? string.Empty : m.TogglProjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void MigrateSelectedClientsToStatus(UserMappings userMappings)
    {
        if (userMappings.SelectedClients.Count == 0)
        {
            return;
        }

        var selectedIds = userMappings.SelectedClients
            .Where(c => c.IsSelected)
            .Select(c => c.ClientId)
            .ToHashSet();

        foreach (var mapping in userMappings.EntryMappings)
        {
            if (mapping.Status != EntryMappingStatus.Active)
            {
                continue;
            }

            if (selectedIds.Count > 0 && !selectedIds.Contains(mapping.TogglClientId))
            {
                mapping.Status = EntryMappingStatus.Ignore;
            }
        }

        userMappings.SelectedClients = [];
    }

    private static string MappingKey(EntryMapping mapping) =>
        MappingKey(mapping.TogglClientId, mapping.TogglClientName, mapping.TogglProjectId, mapping.TogglProjectName);

    private static string MappingKey(long clientId, string clientName, long projectId, string projectName)
    {
        var clientPart = clientId > 0 ? $"id:{clientId}" : $"name:{clientName}";
        var projectPart = projectId > 0
            ? $"id:{projectId}"
            : string.IsNullOrWhiteSpace(projectName) ? "project:" : $"name:{projectName}";
        return $"{clientPart}|{projectPart}";
    }

    private void LoadMappingsIntoUi()
    {
        var userMappings = GetCurrentUserMappings();
        MappingGrid.Rows.Clear();
        foreach (var mapping in userMappings.EntryMappings)
        {
            MappingGrid.Rows.Add(
                mapping.Status.ToString(),
                mapping.TogglClientName,
                string.IsNullOrWhiteSpace(mapping.TogglProjectName) ? string.Empty : mapping.TogglProjectName,
                mapping.SpesnetProjectId > 0 ? mapping.SpesnetProjectId : DBNull.Value,
                mapping.SpesnetClientId > 0 ? mapping.SpesnetClientId : DBNull.Value,
                mapping.SpesnetWorkTaskId > 0 ? mapping.SpesnetWorkTaskId : DBNull.Value);
            MappingGrid.Rows[^1].Tag = mapping;
        }

        RefreshMappingGridSources();
    }

    private void SaveMappingsFromUi()
    {
        var userMappings = GetCurrentUserMappings();
        userMappings.SelectedClients = [];
        userMappings.EntryMappings = [];

        foreach (DataGridViewRow row in MappingGrid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var status = ParseStatus(row.Cells[StatusColumn.Index].Value);
            var togglClientName = Convert.ToString(row.Cells[TogglClientColumn.Index].Value) ?? string.Empty;
            var togglProjectName = Convert.ToString(row.Cells[TogglProjectColumn.Index].Value) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(togglClientName))
            {
                continue;
            }

            var isClientLevelIgnore = status == EntryMappingStatus.Ignore &&
                                      string.IsNullOrWhiteSpace(togglProjectName);
            if (!isClientLevelIgnore && string.IsNullOrWhiteSpace(togglProjectName))
            {
                // Incomplete non-ignore rows are dropped; New client+project rows are auto-populated.
                continue;
            }

            var togglClient = _togglClients.FirstOrDefault(c =>
                string.Equals(c.Name, togglClientName, StringComparison.OrdinalIgnoreCase));
            var togglProject = isClientLevelIgnore
                ? null
                : _togglProjects.FirstOrDefault(p =>
                    string.Equals(p.Name, togglProjectName, StringComparison.OrdinalIgnoreCase) &&
                    (togglClient == null || p.ClientId == togglClient.Id));

            var projectId = GetSelectedId(row.Cells[SpesnetProjectColumn.Index].Value);
            var clientId = GetSelectedId(row.Cells[SpesnetClientColumn.Index].Value);
            var workTaskId = GetSelectedId(row.Cells[SpesnetWorkTaskColumn.Index].Value);

            userMappings.EntryMappings.Add(new EntryMapping
            {
                Status = status,
                TogglClientId = togglClient?.Id ?? 0,
                TogglClientName = togglClientName,
                TogglProjectId = togglProject?.Id ?? 0,
                TogglProjectName = isClientLevelIgnore ? string.Empty : togglProjectName,
                SpesnetProjectId = projectId,
                SpesnetProjectName = projectId > 0 ? GetProjectName(projectId) : string.Empty,
                SpesnetClientId = clientId,
                SpesnetClientName = projectId > 0 && clientId > 0 ? GetClientName(projectId, clientId) : string.Empty,
                SpesnetWorkTaskId = workTaskId,
                SpesnetWorkTaskName = workTaskId > 0 ? GetWorkTaskName(workTaskId) : string.Empty
            });
        }

        // If a client-level Ignore exists, drop project-specific rows for that client.
        var ignoredClientIds = userMappings.EntryMappings
            .Where(m => m.IsClientLevelIgnore && m.TogglClientId > 0)
            .Select(m => m.TogglClientId)
            .ToHashSet();
        var ignoredClientNames = userMappings.EntryMappings
            .Where(m => m.IsClientLevelIgnore)
            .Select(m => m.TogglClientName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (ignoredClientIds.Count > 0 || ignoredClientNames.Count > 0)
        {
            userMappings.EntryMappings = userMappings.EntryMappings
                .Where(m =>
                    m.IsClientLevelIgnore ||
                    (!ignoredClientIds.Contains(m.TogglClientId) &&
                     !ignoredClientNames.Contains(m.TogglClientName)))
                .ToList();
        }

        if (_togglClients.Count > 0)
        {
            EnsureEntryMappingsFromToggl();
        }
    }

    private static EntryMappingStatus ParseStatus(object? value)
    {
        var text = Convert.ToString(value);
        if (Enum.TryParse<EntryMappingStatus>(text, ignoreCase: true, out var status))
        {
            return status;
        }

        return EntryMappingStatus.New;
    }

    private void RefreshMappingGridSources()
    {
        var togglClientNames = _togglClients.Select(c => c.Name).Distinct().OrderBy(n => n).ToArray();
        var projectItems = _referenceCache.Projects
            .Select(p => new ComboItem(p.Id, p.ProjName))
            .OrderBy(p => p.Name)
            .ToArray();
        var workTaskItems = _referenceCache.WorkTasks
            .Select(w => new ComboItem(w.Id, $"{w.Description} ({w.Code})"))
            .OrderBy(w => w.Name)
            .ToArray();

        SetComboColumnDataSource(StatusColumn, StatusDisplayValues);
        SetComboColumnDataSource(TogglClientColumn, togglClientNames);
        SetComboColumnDataSource(SpesnetProjectColumn, projectItems);
        SetComboColumnDataSource(SpesnetWorkTaskColumn, workTaskItems);

        foreach (DataGridViewRow row in MappingGrid.Rows)
        {
            if (!row.IsNewRow)
            {
                UpdateTogglProjectComboForRow(row);
                UpdateSpesnetClientComboForRow(row);
            }
        }
    }

    private void SetComboColumnDataSource(DataGridViewComboBoxColumn column, object dataSource)
    {
        column.DataSource = null;
        column.DataSource = dataSource;
        if (dataSource is ComboItem[])
        {
            column.DisplayMember = nameof(ComboItem.Name);
            column.ValueMember = nameof(ComboItem.Id);
        }
    }

    private void InitializeMappingGrids()
    {
        MappingGrid.AutoGenerateColumns = false;
        MappingGrid.Columns.Add(StatusColumn);
        MappingGrid.Columns.Add(TogglClientColumn);
        MappingGrid.Columns.Add(TogglProjectColumn);
        MappingGrid.Columns.Add(SpesnetProjectColumn);
        MappingGrid.Columns.Add(SpesnetClientColumn);
        MappingGrid.Columns.Add(SpesnetWorkTaskColumn);

        StatusColumn.DataSource = StatusDisplayValues;

        MappingGrid.EditingControlShowing += MappingGrid_EditingControlShowing;
        MappingGrid.CellValueChanged += MappingGrid_CellValueChanged;
        MappingGrid.CurrentCellDirtyStateChanged += MappingGrid_CurrentCellDirtyStateChanged;
        MappingGrid.DataError += MappingGrid_DataError;
        MappingGrid.DefaultValuesNeeded += MappingGrid_DefaultValuesNeeded;
    }

    private static void MappingGrid_DefaultValuesNeeded(object? sender, DataGridViewRowEventArgs e)
    {
        e.Row.Cells["StatusColumn"].Value = nameof(EntryMappingStatus.New);
    }

    private static void MappingGrid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        // Combo cells briefly hold values that aren't in the filtered Items list while
        // per-row DataSources are swapped; suppress the default error dialog.
        e.ThrowException = false;
    }

    private void MappingGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (sender is DataGridView grid && grid.IsCurrentCellDirty)
        {
            grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void MappingGrid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (e.Control is not ComboBox comboBox)
        {
            return;
        }

        comboBox.SelectedIndexChanged -= TogglClientCombo_SelectedIndexChanged;
        comboBox.SelectedIndexChanged -= SpesnetProjectCombo_SelectedIndexChanged;

        if (MappingGrid.CurrentCell?.OwningColumn == TogglClientColumn)
        {
            comboBox.SelectedIndexChanged += TogglClientCombo_SelectedIndexChanged;
        }
        else if (MappingGrid.CurrentCell?.OwningColumn == TogglProjectColumn && MappingGrid.CurrentRow != null)
        {
            UpdateTogglProjectComboForRow(MappingGrid.CurrentRow);
            SyncEditingComboWithCell(comboBox, (DataGridViewComboBoxCell)MappingGrid.CurrentCell);
        }
        else if (MappingGrid.CurrentCell?.OwningColumn == SpesnetProjectColumn)
        {
            comboBox.SelectedIndexChanged += SpesnetProjectCombo_SelectedIndexChanged;
        }
        else if (MappingGrid.CurrentCell?.OwningColumn == SpesnetClientColumn && MappingGrid.CurrentRow != null)
        {
            UpdateSpesnetClientComboForRow(MappingGrid.CurrentRow);
            SyncEditingComboWithCell(comboBox, (DataGridViewComboBoxCell)MappingGrid.CurrentCell);
        }
    }

    private static void SyncEditingComboWithCell(ComboBox editingCombo, DataGridViewComboBoxCell cell)
    {
        editingCombo.DataSource = null;
        editingCombo.DisplayMember = cell.DisplayMember;
        editingCombo.ValueMember = cell.ValueMember;
        editingCombo.DataSource = cell.DataSource;

        if (!string.IsNullOrEmpty(cell.ValueMember))
        {
            editingCombo.SelectedValue = cell.Value!;
        }
        else
        {
            editingCombo.SelectedItem = cell.Value;
        }
    }

    private void TogglClientCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (MappingGrid.CurrentRow == null)
        {
            return;
        }

        UpdateTogglProjectComboForRow(MappingGrid.CurrentRow);
    }

    private void SpesnetProjectCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (MappingGrid.CurrentRow == null)
        {
            return;
        }

        UpdateSpesnetClientComboForRow(MappingGrid.CurrentRow);
    }

    private void MappingGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
        {
            return;
        }

        if (e.ColumnIndex == TogglClientColumn.Index)
        {
            UpdateTogglProjectComboForRow(MappingGrid.Rows[e.RowIndex]);
        }
        else if (e.ColumnIndex == SpesnetProjectColumn.Index)
        {
            UpdateSpesnetClientComboForRow(MappingGrid.Rows[e.RowIndex]);
        }
    }

    private void UpdateTogglProjectComboForRow(DataGridViewRow row)
    {
        var togglClientName = Convert.ToString(row.Cells[TogglClientColumn.Index].Value) ?? string.Empty;
        var togglClient = _togglClients.FirstOrDefault(c =>
            string.Equals(c.Name, togglClientName, StringComparison.OrdinalIgnoreCase));

        // Leading empty option allows client-level Ignore (no project).
        var projectNames = togglClient == null
            ? [string.Empty]
            : new[] { string.Empty }
                .Concat(_togglProjects
                    .Where(p => p.ClientId == togglClient.Id)
                    .Select(p => p.Name)
                    .Distinct()
                    .OrderBy(n => n))
                .ToArray();

        var projectCell = (DataGridViewComboBoxCell)row.Cells[TogglProjectColumn.Index];
        var current = Convert.ToString(projectCell.Value) ?? string.Empty;

        projectCell.Value = null;
        projectCell.DataSource = null;
        projectCell.DataSource = projectNames;

        var match = projectNames.FirstOrDefault(n =>
            string.Equals(n, current, StringComparison.OrdinalIgnoreCase));
        projectCell.Value = match ?? string.Empty;
    }

    private void UpdateSpesnetClientComboForRow(DataGridViewRow row)
    {
        var projectId = GetSelectedId(row.Cells[SpesnetProjectColumn.Index].Value);
        var clients = projectId > 0 && _referenceCache.ClientsByProject.TryGetValue(projectId, out var projectClients)
            ? projectClients.Select(c => new ComboItem(c.Id, c.Name)).ToArray()
            : Array.Empty<ComboItem>();

        var clientCell = (DataGridViewComboBoxCell)row.Cells[SpesnetClientColumn.Index];
        var currentId = GetSelectedId(clientCell.Value);

        clientCell.Value = null;
        clientCell.DataSource = null;
        clientCell.DisplayMember = nameof(ComboItem.Name);
        clientCell.ValueMember = nameof(ComboItem.Id);
        clientCell.DataSource = clients;

        if (clients.Length == 0)
        {
            return;
        }

        var match = clients.FirstOrDefault(c => c.Id == currentId);
        clientCell.Value = match?.Id ?? (object?)null;
    }

    private async void RefreshTogglButton_Click(object sender, EventArgs e)
    {
        try
        {
            SetUiEnabled(false);
            SaveMappingsFromUi();
            await RefreshTogglClientsAsync();
            _logger.Info("Toggl clients and projects refreshed; mapping rows updated.");
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
            _logger.Error(ex.Message);
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

        var unresolved = SyncService.FindUnresolvedMappings(GetCurrentUserMappings());
        if (unresolved.Count > 0)
        {
            MessageBox.Show(
                this,
                $"Resolve {unresolved.Count} mapping(s) still set to New before syncing. Set each to Active (with Spesnet fields) or Ignore. " +
                "A client-only row with Status=Ignore covers every project for that client.",
                "Unresolved Mappings",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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
