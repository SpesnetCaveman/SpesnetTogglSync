namespace SpesnetTogglSync.Models;

/// <summary>
/// Invoice numbers already issued. Each billing period keeps the number it was first given.
/// </summary>
public class InvoiceNumberLedger
{
    /// <summary>Highest invoice number issued, including the number read from the Word template.</summary>
    public int LastNumber { get; set; }

    /// <summary>Period key (yyyy-MM-dd_yyyy-MM-dd) to the invoice number used for that period.</summary>
    public Dictionary<string, int> Periods { get; set; } = [];
}
