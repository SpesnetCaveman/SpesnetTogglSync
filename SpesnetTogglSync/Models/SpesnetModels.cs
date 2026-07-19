using System.Text.Json.Serialization;

namespace SpesnetTogglSync.Models;

public class SpesnetLoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("rememberme")]
    public bool RememberMe { get; set; }
}

public class SpesnetUserInfo
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("aspNetUserId")]
    public string AspNetUserId { get; set; } = string.Empty;
}

public class SpesnetEmployeeResponse
{
    [JsonPropertyName("currentUser")]
    public SpesnetEmployee? CurrentUser { get; set; }
}

public class SpesnetEmployee
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("surname")]
    public string Surname { get; set; } = string.Empty;

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;
}

public class SpesnetProject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("projname")]
    public string ProjName { get; set; } = string.Empty;
}

public class SpesnetWorkTask
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("descript")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}

public class SpesnetClient
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}

public class SpesnetClientsByProjectResponse
{
    [JsonPropertyName("defaultProjectID")]
    public int DefaultProjectId { get; set; }

    [JsonPropertyName("clients")]
    public List<SpesnetClient> Clients { get; set; } = [];
}

public class SpesnetWorkDoneEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    [JsonPropertyName("employeeid")]
    public int EmployeeId { get; set; }

    [JsonPropertyName("normalhours")]
    public double NormalHours { get; set; }

    [JsonPropertyName("overtimehours")]
    public double OvertimeHours { get; set; }

    [JsonPropertyName("projectid")]
    public int ProjectId { get; set; }

    [JsonPropertyName("clientid")]
    public int ClientId { get; set; }

    [JsonPropertyName("txdatetime")]
    public string TxDateTime { get; set; } = string.Empty;

    [JsonPropertyName("worktaskid")]
    public int WorkTaskId { get; set; }
}

public class SpesnetSaveWorkRequest
{
    [JsonPropertyName("workDoneList")]
    public List<SpesnetWorkDoneEntry> WorkDoneList { get; set; } = [];

    [JsonPropertyName("accessKey")]
    public string AccessKey { get; set; } = string.Empty;
}

public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SyncedCount { get; set; }
    public int SkippedCount { get; set; }
    public DateTime? LastSyncedStartTime { get; set; }
}

public class SyncProgressEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public DateTime? UpdatedWatermark { get; set; }
}
