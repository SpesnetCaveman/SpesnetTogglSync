using SpesnetTogglSync.Models;

namespace SpesnetTogglSync.Services;

/// <summary>
/// Assigns one invoice number per billing period. Regenerating that period reuses it.
/// </summary>
internal static class InvoiceNumbers
{
    public static string PeriodKey(BillingPeriod period) =>
        period.Start.ToString("yyyy-MM-dd") + "_" + period.End.ToString("yyyy-MM-dd");

    /// <summary>Latest invoice number to show in Settings. Falls back to the ledger when the setting is unset.</summary>
    public static int Current(AppSettings settings, InvoiceNumberLedger ledger)
    {
        if (settings.InvoiceNumber > 0)
        {
            return settings.InvoiceNumber;
        }

        return LastIssued(ledger);
    }

    public static int Resolve(ConfigService config, AppSettings settings, string templatePath, BillingPeriod period)
    {
        var ledger = config.LoadInvoiceLedger();
        var key = PeriodKey(period);
        if (ledger.Periods.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var highest = LastIssued(ledger);
        var assigned = settings.InvoiceNumber;
        if (assigned <= 0)
        {
            assigned = highest > 0 ? highest + 1 : NextFromTemplate(templatePath, period);
        }
        else if (assigned <= highest)
        {
            assigned = highest + 1;
        }

        ledger.Periods[key] = assigned;
        ledger.LastNumber = Math.Max(ledger.LastNumber, assigned);
        config.SaveInvoiceLedger(ledger);

        settings.InvoiceNumber = assigned;
        config.SaveSettings(settings);
        return assigned;
    }

    private static int LastIssued(InvoiceNumberLedger ledger)
    {
        var last = ledger.LastNumber;
        if (ledger.Periods.Count > 0)
        {
            last = Math.Max(last, ledger.Periods.Values.Max());
        }

        return Math.Max(last, 0);
    }

    private static int NextFromTemplate(string templatePath, BillingPeriod period)
    {
        var templateInvoice = InvoiceDocument.ReadTemplateInvoice(templatePath);
        return templateInvoice.Date == period.End
            ? templateInvoice.Number
            : templateInvoice.Number + 1;
    }
}
