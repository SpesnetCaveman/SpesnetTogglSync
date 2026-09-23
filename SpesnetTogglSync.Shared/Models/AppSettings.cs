namespace SpesnetTogglSync.Models;

public class AppSettings
{
    public string TogglApiToken { get; set; } = string.Empty;
    public long TogglWorkspaceId { get; set; }
    public string SpesnetUsername { get; set; } = string.Empty;
    public string SpesnetPassword { get; set; } = string.Empty;
    public string SpesnetDomain { get; set; } = "https://gateway_internal.evolvemed.co.za/api-evolveTimekeepingAPI/";
    public bool UseMockSpesnet { get; set; } = true;

    /// <summary>ZAR per hour. Sync notifications and billing reports multiply tracked hours by this.</summary>
    public decimal HourlyRate { get; set; }

    /// <summary>
    /// Day of the month a billing cycle starts (1–31). Shorter months use the last day.
    /// The cycle runs until the day before the next start. 20 means the 20th through the 19th.
    /// </summary>
    public int BillingCycleStartDay { get; set; } = 20;

    /// <summary>
    /// South African time (HH:mm) after which the daily sync runs as soon as this PC is on.
    /// If the PC is off at that time, the sync runs on the next launch that day.
    /// </summary>
    public string DailySyncTime { get; set; } = "07:00";

    /// <summary>
    /// Folder for billing-cycle Toggl reports and invoices ({folder}/yyyy-MM/).
    /// Empty saves them in the data directory.
    /// </summary>
    public string BillingReportDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Latest invoice number. A new billing period uses the next number and updates this.
    /// Creating the same period again does not change it. 0 means it has not been set.
    /// </summary>
    public int InvoiceNumber { get; set; }

    /// <summary>
    /// Word file copied once into the data directory as invoice-template.docx.
    /// After that copy exists, this path is not read again.
    /// </summary>
    public string InvoiceTemplatePath { get; set; } = string.Empty;

    /// <summary>
    /// When null, the app registers itself to start with Windows.
    /// Set false to leave the notification-area schedule off after login.
    /// </summary>
    public bool? RunAtStartup { get; set; }

    public SpesnetReferenceCache? SpesnetReferenceCache { get; set; }

    public bool IsRunAtStartupEnabled() => RunAtStartup ?? true;
}

public class SpesnetReferenceCache
{
    public int EmployeeId { get; set; }
    public List<SpesnetProject> Projects { get; set; } = [];
    public List<SpesnetWorkTask> WorkTasks { get; set; } = [];
    public Dictionary<int, List<SpesnetClient>> ClientsByProject { get; set; } = [];
}
