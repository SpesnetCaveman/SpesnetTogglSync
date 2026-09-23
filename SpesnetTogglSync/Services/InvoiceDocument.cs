using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;

namespace SpesnetTogglSync.Services;

/// <summary>
/// Fills the app's invoice template and exports a PDF. The template file itself is not changed.
/// Replacement uses the existing labels (INVOICE:, Date:, Consultation services, hours, rate, amount, total).
/// </summary>
internal readonly record struct TemplateInvoice(int Number, DateOnly Date);

internal static class InvoiceDocument
{
    private const int WdAlertsNone = 0;
    private const int WdDoNotSaveChanges = 0;
    private const int WdExportFormatPdf = 17;
    private const int WdFormatXmlDocument = 12;
    private const int WdCharacter = 1;

    /// <summary>
    /// Copies <paramref name="importFrom"/> to <paramref name="appCopyPath"/> when the app copy is missing.
    /// An existing app copy is left as-is, so later edits to the original file are ignored.
    /// </summary>
    public static void EnsureAppCopy(string appCopyPath, string? importFrom)
    {
        if (File.Exists(appCopyPath))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(importFrom) || !File.Exists(importFrom))
        {
            return;
        }

        Import(importFrom, appCopyPath);
    }

    /// <summary>Replaces the app's template with <paramref name="sourcePath"/>.</summary>
    public static void ReplaceAppCopy(string sourcePath, string appCopyPath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Invoice template was not found.", sourcePath);
        }

        Import(sourcePath, appCopyPath);
    }

    public static TemplateInvoice ReadTemplateInvoice(string templatePath)
    {
        var invoice = new TemplateInvoice(0, default);
        RunSta(() =>
        {
            WithWord(templatePath, readOnly: true, document =>
            {
                string text = document.Content.Text;
                var numberMatch = Regex.Match(text, @"INVOICE:\s*(\d+)");
                var dateMatch = Regex.Match(text, @"Date:\s*(\d{4}-\d{2}-\d{2})");
                if (!numberMatch.Success || !int.TryParse(numberMatch.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                {
                    throw new InvalidOperationException("The invoice template does not contain an invoice number.");
                }

                if (!dateMatch.Success || !DateOnly.TryParseExact(dateMatch.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    throw new InvalidOperationException("The invoice template does not contain an invoice date.");
                }

                invoice = new TemplateInvoice(number, date);
            });
        });
        return invoice;
    }

    public static void CreatePdf(
        string templatePath,
        string pdfPath,
        int invoiceNumber,
        BillingPeriod period,
        decimal hours,
        decimal hourlyRate)
    {
        var numberText = invoiceNumber.ToString("000", CultureInfo.InvariantCulture);
        var invoiceDate = period.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var description =
            "Consultation services " +
            period.Start.ToString("d MMMM yyyy", CultureInfo.InvariantCulture) +
            " \u2013 " +
            period.End.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var hoursText = hours.ToString("0.00", CultureInfo.InvariantCulture);
        var rateText = hourlyRate == decimal.Truncate(hourlyRate)
            ? decimal.Truncate(hourlyRate).ToString("0", CultureInfo.InvariantCulture)
            : hourlyRate.ToString("0.00", CultureInfo.InvariantCulture);
        var money = FormatRand(decimal.Round(hours * hourlyRate, 2, MidpointRounding.AwayFromZero));

        RunSta(() =>
        {
            var tempDoc = Path.Combine(Path.GetTempPath(), "spesnet-invoice-" + Guid.NewGuid().ToString("N") + ".docx");
            dynamic? word = null;
            dynamic? document = null;
            try
            {
                word = CreateWord();
                dynamic source = word.Documents.Open(templatePath, false, true, false);
                source.SaveAs2(tempDoc, WdFormatXmlDocument);
                source.Close(WdDoNotSaveChanges);
                document = word.Documents.Open(tempDoc, false, false, false);
                Unlock(document);
                ReplaceParagraphToken(
                    document,
                    "INVOICE:",
                    @"\d+",
                    numberText,
                    "invoice number");
                ReplaceParagraphToken(
                    document,
                    "Date:",
                    @"\d{4}-\d{2}-\d{2}",
                    invoiceDate,
                    "invoice date");
                FillAmounts(document, description, hoursText, rateText, money);
                if (File.Exists(pdfPath))
                {
                    File.Delete(pdfPath);
                }

                document.ExportAsFixedFormat(pdfPath, WdExportFormatPdf);
            }
            finally
            {
                if (document != null)
                {
                    try
                    {
                        document.Close(WdDoNotSaveChanges);
                    }
                    catch
                    {
                        // The document may already be closed after export.
                    }
                }

                if (word != null)
                {
                    try
                    {
                        word.Quit(WdDoNotSaveChanges);
                    }
                    catch
                    {
                        // Word already quit.
                    }
                }

                TryDelete(tempDoc);
            }
        });
    }

    public static string FormatRand(decimal amount)
    {
        var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        format.NumberGroupSeparator = "\u00A0";
        format.NumberDecimalSeparator = ".";
        format.NumberGroupSizes = [3];
        return "R " + amount.ToString("#,##0.00", format);
    }

    private static void FillAmounts(dynamic document, string description, string hours, string rate, string money)
    {
        var filledLine = false;
        var filledTotal = false;
        int tableCount = document.Tables.Count;
        for (var tableIndex = 1; tableIndex <= tableCount; tableIndex++)
        {
            dynamic table = document.Tables[tableIndex];
            var headers = ReadHeaders(table);
            int rowCount = table.Rows.Count;
            for (var rowIndex = 1; rowIndex <= rowCount; rowIndex++)
            {
                dynamic row = table.Rows[rowIndex];
                var kind = RowKind(row);
                if (kind == "line")
                {
                    SetLabeledCell(row, "Consultation services", description);
                    SetHeaderOrPosition(table, row, headers, "HOURS", hours, 2);
                    SetHeaderOrPosition(table, row, headers, "RATE", rate, 3);
                    SetHeaderOrPosition(table, row, headers, "AMOUNT", money, 4);
                    filledLine = true;
                }
                else if (kind == "total")
                {
                    if (!SetHeaderCell(table, row, headers, "AMOUNT", money))
                    {
                        SetLastMoneyCell(row, money);
                    }

                    filledTotal = true;
                }
            }
        }

        if (!filledLine || !filledTotal)
        {
            throw new InvalidOperationException("The invoice template is missing the hours line or the total.");
        }
    }

    private static Dictionary<string, int> ReadHeaders(dynamic table)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int rowCount = table.Rows.Count;
        for (var rowIndex = 1; rowIndex <= rowCount; rowIndex++)
        {
            ForEachCell((object)table.Rows[rowIndex], (cell, _) =>
            {
                var text = CellText(cell);
                if (text.Equals("DESCRIPTION", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("HOURS", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("RATE", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("AMOUNT", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        headers[text] = (int)cell.ColumnIndex;
                    }
                    catch
                    {
                        // Merged cells do not always expose a column index.
                    }
                }
            });
        }

        return headers;
    }

    private static string? RowKind(dynamic row)
    {
        string? kind = null;
        ForEachCell((object)row, (cell, _) =>
        {
            if (kind != null)
            {
                return;
            }

            var text = CellText(cell);
            if (text.StartsWith("Consultation services", StringComparison.OrdinalIgnoreCase))
            {
                kind = "line";
            }
            else if (text.Equals("TOTAL", StringComparison.OrdinalIgnoreCase))
            {
                kind = "total";
            }
        });

        return kind;
    }

    private static void SetLabeledCell(dynamic row, string prefix, string value)
    {
        ForEachCell((object)row, (cell, _) =>
        {
            if (CellText(cell).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                SetCellText(cell, value);
            }
        });
    }

    private static void SetHeaderOrPosition(
        dynamic table,
        dynamic row,
        Dictionary<string, int> headers,
        string header,
        string value,
        int position)
    {
        if (SetHeaderCell(table, row, headers, header, value))
        {
            return;
        }

        ForEachCell((object)row, (cell, index) =>
        {
            if (index == position)
            {
                SetCellText(cell, value);
            }
        });
    }

    private static bool SetHeaderCell(
        dynamic table,
        dynamic row,
        Dictionary<string, int> headers,
        string header,
        string value)
    {
        if (!headers.TryGetValue(header, out var column))
        {
            return false;
        }

        try
        {
            SetCellText(table.Cell((int)row.Index, column), value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void SetLastMoneyCell(dynamic row, string money)
    {
        dynamic? target = null;
        ForEachCell((object)row, (cell, _) =>
        {
            var text = CellText(cell);
            if (text.StartsWith("R", StringComparison.Ordinal) || text.Contains('.', StringComparison.Ordinal))
            {
                target = cell;
            }
        });

        if (target == null)
        {
            throw new InvalidOperationException("The invoice template total row has no amount.");
        }

        SetCellText(target, money);
    }

    private static void Unlock(dynamic document)
    {
        int protection = -1;
        try
        {
            protection = (int)document.ProtectionType;
        }
        catch
        {
            protection = -1;
        }

        if (protection != -1)
        {
            try
            {
                document.Unprotect("");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Invoice template protection {protection} could not be removed. {ex.GetType().Name}");
            }
        }

        try
        {
            int controls = document.ContentControls.Count;
            for (var index = 1; index <= controls; index++)
            {
                dynamic control = document.ContentControls[index];
                control.LockContents = false;
                control.LockContentControl = false;
            }
        }
        catch
        {
            // No content controls to unlock.
        }
    }

    private static void ReplaceParagraphToken(
        dynamic document,
        string paragraphPrefix,
        string tokenPattern,
        string replacement,
        string label)
    {
        int count = document.Paragraphs.Count;
        for (var index = 1; index <= count; index++)
        {
            dynamic paragraph = document.Paragraphs[index];
            string raw = (string?)paragraph.Range.Text ?? "";
            string text = raw.TrimEnd('\r', '\a');
            int prefixAt = text.IndexOf(paragraphPrefix, StringComparison.Ordinal);
            if (prefixAt < 0)
            {
                continue;
            }

            var token = Regex.Match(text.Substring(prefixAt), tokenPattern);
            if (!token.Success)
            {
                continue;
            }

            dynamic find = paragraph.Range.Find;
            find.ClearFormatting();
            find.Replacement.ClearFormatting();
            find.Text = token.Value;
            find.Replacement.Text = replacement;
            find.Forward = true;
            find.Wrap = 0;
            find.Format = false;
            find.MatchCase = false;
            find.MatchWholeWord = false;
            find.MatchWildcards = false;
            bool replaced = find.Execute(
                token.Value,
                false,
                false,
                false,
                false,
                false,
                true,
                0,
                false,
                replacement,
                1);
            if (!replaced)
            {
                continue;
            }

            return;
        }

        throw new InvalidOperationException($"The invoice template is missing the {label}.");
    }

    private static void ForEachCell(object rowObject, Action<dynamic, int> action)
    {
        dynamic row = rowObject;
        int count = row.Cells.Count;
        for (var index = 1; index <= count; index++)
        {
            action(row.Cells[index], index);
        }
    }

    private static string CellText(dynamic cell)
    {
        string text = cell.Range.Text;
        return text.Trim().TrimEnd('\r', '\a', '\n');
    }

    private static void SetCellText(dynamic cell, string value)
    {
        dynamic range = cell.Range;
        range.MoveEnd(WdCharacter, -1);
        range.Text = value.Replace('\u00A0', '|');
        if (!value.Contains('\u00A0'))
        {
            return;
        }

        dynamic find = cell.Range.Find;
        find.ClearFormatting();
        find.Replacement.ClearFormatting();
        find.Text = "|";
        find.Replacement.Text = "\u00A0";
        find.Forward = true;
        find.Wrap = 0;
        find.Format = false;
        find.MatchWildcards = false;
        find.Execute("|", false, false, false, false, false, true, 0, false, "\u00A0", 2);
    }

    private static dynamic CreateWord()
    {
        var wordType = Type.GetTypeFromProgID("Word.Application");
        if (wordType == null)
        {
            throw new InvalidOperationException("Microsoft Word is required to create the invoice PDF.");
        }

        dynamic word = Activator.CreateInstance(wordType)
            ?? throw new InvalidOperationException("Microsoft Word is required to create the invoice PDF.");
        word.Visible = false;
        word.DisplayAlerts = WdAlertsNone;
        return word;
    }

    private static void WithWord(string templatePath, bool readOnly, Action<dynamic> action)
    {
        dynamic word = CreateWord();
        dynamic? document = null;
        try
        {
            document = word.Documents.Open(templatePath, false, readOnly, false);
            action(document);
        }
        finally
        {
            if (document != null)
            {
                try
                {
                    document.Close(WdDoNotSaveChanges);
                }
                catch
                {
                    // The document may already be closed.
                }
            }

            try
            {
                word.Quit(WdDoNotSaveChanges);
            }
            catch
            {
                // Word already quit.
            }
        }
    }

    private static void RunSta(Action action)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            action();
            return;
        }

        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        })
        {
            IsBackground = true,
            Name = "Invoice Word"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

    private static void Import(string sourcePath, string appCopyPath)
    {
        var source = Path.GetFullPath(sourcePath);
        var destination = Path.GetFullPath(appCopyPath);
        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var staging = destination + ".importing";
        TryDelete(staging);
        try
        {
            CopyFile(source, staging);
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            File.Move(staging, destination);
        }
        finally
        {
            TryDelete(staging);
        }
    }

    private static void CopyFile(string source, string destination)
    {
        try
        {
            File.Copy(source, destination, overwrite: true);
        }
        catch (IOException)
        {
            CopyWithWord(source, destination);
        }
        catch (UnauthorizedAccessException)
        {
            CopyWithWord(source, destination);
        }
    }

    private static void CopyWithWord(string source, string destination)
    {
        RunSta(() =>
        {
            dynamic word = CreateWord();
            dynamic? document = null;
            try
            {
                document = word.Documents.Open(source, false, true, false);
                TryDelete(destination);
                document.SaveAs2(destination, WdFormatXmlDocument);
            }
            finally
            {
                if (document != null)
                {
                    try
                    {
                        document.Close(WdDoNotSaveChanges);
                    }
                    catch
                    {
                        // The document may already be closed.
                    }
                }

                try
                {
                    word.Quit(WdDoNotSaveChanges);
                }
                catch
                {
                    // Word already quit.
                }
            }
        });
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // A leftover temp copy is harmless.
        }
    }
}
