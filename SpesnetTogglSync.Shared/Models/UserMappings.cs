namespace SpesnetTogglSync.Models;

public class MappingsFile
{
    public Dictionary<string, UserMappings> Users { get; set; } = [];
}

public class UserMappings
{
    public long TogglUserId { get; set; }

    /// <summary>
    /// Legacy client checkbox selection. Kept for one-time migration from older mappings.json;
    /// cleared after migrate and no longer used by sync/UI.
    /// </summary>
    public List<SelectedTogglClient> SelectedClients { get; set; } = [];

    public List<EntryMapping> EntryMappings { get; set; } = [];
}

public class SelectedTogglClient
{
    public long ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

/// <summary>
/// How an entry mapping participates in sync.
/// Default <see cref="Active"/> preserves older mappings.json that omit status.
/// </summary>
public enum EntryMappingStatus
{
    Active = 0,
    Ignore = 1,
    New = 2
}

/// <summary>
/// One row maps a Toggl client + project pair to the full Spesnet destination.
/// A client-only row (empty project) with status Ignore skips every project for that client.
/// </summary>
public class EntryMapping
{
    public EntryMappingStatus Status { get; set; } = EntryMappingStatus.Active;
    public long TogglClientId { get; set; }
    public string TogglClientName { get; set; } = string.Empty;
    public long TogglProjectId { get; set; }
    public string TogglProjectName { get; set; } = string.Empty;
    public int SpesnetProjectId { get; set; }
    public string SpesnetProjectName { get; set; } = string.Empty;
    public int SpesnetClientId { get; set; }
    public string SpesnetClientName { get; set; } = string.Empty;
    public int SpesnetWorkTaskId { get; set; }
    public string SpesnetWorkTaskName { get; set; } = string.Empty;

    public bool IsClientLevelIgnore =>
        Status == EntryMappingStatus.Ignore &&
        TogglProjectId == 0 &&
        string.IsNullOrWhiteSpace(TogglProjectName);

    public bool HasSpesnetDestination =>
        SpesnetProjectId > 0 && SpesnetClientId > 0 && SpesnetWorkTaskId > 0;
}
