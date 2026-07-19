using System.Text.Json.Serialization;

namespace SpesnetTogglSync.Models;

public class TogglMe
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("default_workspace_id")]
    public long DefaultWorkspaceId { get; set; }
}

public class TogglClient
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class TogglProject
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("client_id")]
    public long? ClientId { get; set; }
}

public class TogglTimeEntry
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("workspace_id")]
    public long WorkspaceId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("start")]
    public string Start { get; set; } = string.Empty;

    [JsonPropertyName("stop")]
    public string? Stop { get; set; }

    [JsonPropertyName("duration")]
    public long Duration { get; set; }

    [JsonPropertyName("client_id")]
    public long? ClientId { get; set; }

    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("project_id")]
    public long? ProjectId { get; set; }

    [JsonPropertyName("project_name")]
    public string? ProjectName { get; set; }

    public DateTime StartUtc => DateTime.Parse(Start, null, System.Globalization.DateTimeStyles.RoundtripKind);
}
