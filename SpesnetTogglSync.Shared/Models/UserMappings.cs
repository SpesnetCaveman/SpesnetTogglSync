namespace SpesnetTogglSync.Models;

public class MappingsFile
{
    public Dictionary<string, UserMappings> Users { get; set; } = [];
}

public class UserMappings
{
    public long TogglUserId { get; set; }
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
/// One row maps a Toggl client + project pair to the full Spesnet destination.
/// </summary>
public class EntryMapping
{
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
}
