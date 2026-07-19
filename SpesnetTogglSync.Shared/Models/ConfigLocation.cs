namespace SpesnetTogglSync.Models;

/// <summary>
/// Bootstrap pointer kept next to the executable. Points at the directory that holds
/// appsettings.json, mappings.json, syncstate.json, and logs/.
/// </summary>
public class ConfigLocation
{
    public string DataDirectory { get; set; } = string.Empty;
}
