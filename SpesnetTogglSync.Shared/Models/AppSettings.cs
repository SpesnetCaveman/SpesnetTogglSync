namespace SpesnetTogglSync.Models;

public class AppSettings
{
    public string TogglApiToken { get; set; } = string.Empty;
    public long TogglWorkspaceId { get; set; }
    public string SpesnetUsername { get; set; } = string.Empty;
    public string SpesnetPassword { get; set; } = string.Empty;
    public string SpesnetDomain { get; set; } = "https://gateway_internal.evolvemed.co.za/api-evolveTimekeepingAPI/";
    public bool UseMockSpesnet { get; set; } = true;
    public SpesnetReferenceCache? SpesnetReferenceCache { get; set; }
}

public class SpesnetReferenceCache
{
    public int EmployeeId { get; set; }
    public List<SpesnetProject> Projects { get; set; } = [];
    public List<SpesnetWorkTask> WorkTasks { get; set; } = [];
    public Dictionary<int, List<SpesnetClient>> ClientsByProject { get; set; } = [];
}
