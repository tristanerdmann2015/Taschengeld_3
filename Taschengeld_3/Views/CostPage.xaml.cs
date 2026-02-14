using System.Text;
using Taschengeld_3.Models;
using Taschengeld_3.Services;
using Taschengeld_3.ViewModels;

namespace Taschengeld_3.Views;

public partial class CostPage : ContentPage
{
    private CostViewModel? _viewModel;
    private SoundService? _soundService;
    private DatabaseService? _databaseService;
    private PdfService? _pdfService;

    public CostPage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<CostViewModel>();
        _soundService = ServiceHelper.GetService<SoundService>();
        _databaseService = ServiceHelper.GetService<DatabaseService>();
        _pdfService = ServiceHelper.GetService<PdfService>();
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await _viewModel.LoadData();
                    ViewModePicker.SelectedIndex = 1; // Monthly by default
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Fehler", ex.Message, "OK");
                }
            });
        }
    }

    private async void OnLoadCosts(object sender, EventArgs e)
    {
        if (_viewModel != null && ViewModePicker.SelectedIndex >= 0)
        {
            await _soundService!.PlayClickSound();
            _viewModel.ViewMode = (CostViewModel.CostViewMode)ViewModePicker.SelectedIndex;
            await _viewModel.LoadCostSummaries();
        }
    }

    private async void OnShareEntry(object sender, EventArgs e)
    {
        try
        {
            await _soundService!.PlayShareSound();
            
            CostSummary? summary = null;
            
            if (sender is Button button)
            {
                summary = button.CommandParameter as CostSummary;
            }
            
            if (summary != null)
            {
                // Generiere PDF und teile
                await GenerateAndSharePdf(summary);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnShareEntry Error: {ex.Message}");
            await DisplayAlertAsync("Fehler", $"Fehler beim Teilen: {ex.Message}", "OK");
        }
    }

    private async Task GenerateAndSharePdf(CostSummary summary)
    {
        try
        {
            // Hole die detaillierten TimeEntries fuer den Zeitraum
            var (startDate, endDate) = ParsePeriod(summary.Period);
            var entries = await _databaseService!.GetTimeEntriesByDate(startDate, endDate);
            
            string filePath;
            
#if ANDROID
            // Verwende nativen Android PDF-Generator
            var pdfGenerator = new AndroidPdfGenerator();
            filePath = await pdfGenerator.GeneratePdfAsync(summary, entries);
            System.Diagnostics.Debug.WriteLine($"PDF erstellt: {filePath}");
#else
            // Fallback fuer andere Plattformen: HTML
            var htmlContent = _pdfService!.GenerateHtmlReport(summary, entries);
            var fileName = $"Taschengeld_{summary.Period.Replace(" ", "_").Replace("/", "-")}.html";
            filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
#endif
            
            // Teile die Datei
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Taschengeld Abrechnung - {summary.Period}",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GenerateAndSharePdf Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            
            // Fallback: Teile als Text
            await ShareAsText(summary);
        }
    }

    private (DateTime startDate, DateTime endDate) ParsePeriod(string period)
    {
        try
        {
            // Format: "Januar 2026" oder "KW 5 2026" oder "Q1 2026"
            var parts = period.Split(' ');
            
            if (period.StartsWith("KW"))
            {
                // Wochenformat: "KW 5 2026"
                var week = int.Parse(parts[1]);
                var year = int.Parse(parts[2]);
                var firstDayOfYear = new DateTime(year, 1, 1);
                var startDate = firstDayOfYear.AddDays((week - 1) * 7 - (int)firstDayOfYear.DayOfWeek + 1);
                return (startDate, startDate.AddDays(6));
            }
            else if (period.StartsWith("Q"))
            {
                // Quartalsformat: "Q1 2026"
                var quarter = int.Parse(parts[0].Substring(1));
                var year = int.Parse(parts[1]);
                var startMonth = (quarter - 1) * 3 + 1;
                return (new DateTime(year, startMonth, 1), new DateTime(year, startMonth + 2, DateTime.DaysInMonth(year, startMonth + 2)));
            }
            else
            {
                // Monatsformat: "Januar 2026"
                var monthNames = new[] { "Januar", "Februar", "März", "April", "Mai", "Juni", 
                                          "Juli", "August", "September", "Oktober", "November", "Dezember" };
                var month = Array.IndexOf(monthNames, parts[0]) + 1;
                if (month == 0) month = 1;
                var year = int.Parse(parts[1]);
                return (new DateTime(year, month, 1), new DateTime(year, month, DateTime.DaysInMonth(year, month)));
            }
        }
        catch
        {
            // Fallback: Aktueller Monat
            var now = DateTime.Now;
            return (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)));
        }
    }

    private string GenerateHtmlReport(CostSummary summary, List<TimeEntry> entries)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='de'>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        sb.AppendLine($"<title>Taschengeld Abrechnung - {summary.Period}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; padding: 20px; max-width: 800px; margin: 0 auto; }");
        sb.AppendLine("h1 { color: #512BD4; border-bottom: 2px solid #512BD4; padding-bottom: 10px; }");
        sb.AppendLine("h2 { color: #333; margin-top: 30px; }");
        sb.AppendLine(".summary { background: #f5f5f5; padding: 15px; border-radius: 8px; margin: 20px 0; }");
        sb.AppendLine(".summary-row { display: flex; justify-content: space-between; padding: 5px 0; }");
        sb.AppendLine(".total { font-size: 24px; color: #4CAF50; font-weight: bold; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        sb.AppendLine("th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("th { background: #512BD4; color: white; }");
        sb.AppendLine("tr:nth-child(even) { background: #f9f9f9; }");
        sb.AppendLine(".price { text-align: right; font-weight: bold; }");
        sb.AppendLine(".footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        
        // Header
        sb.AppendLine($"<h1>📊 Taschengeld Abrechnung</h1>");
        sb.AppendLine($"<h2>Zeitraum: {summary.Period}</h2>");
        
        // Zusammenfassung
        sb.AppendLine("<div class='summary'>");
        sb.AppendLine($"<div class='summary-row'><span>Gesamtverdienst:</span><span class='total'>{summary.TotalCost:C}</span></div>");
        sb.AppendLine($"<div class='summary-row'><span>Anzahl Einträge:</span><span>{summary.EntryCount}</span></div>");
        sb.AppendLine("</div>");
        
        // Aufgaben-Übersicht
        if (summary.TaskCosts != null && summary.TaskCosts.Count > 0)
        {
            sb.AppendLine("<h2>📋 Aufgaben-Übersicht</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Aufgabe</th><th>Anzahl</th><th class='price'>Verdienst</th></tr>");
            foreach (var taskCost in summary.TaskCosts)
            {
                sb.AppendLine($"<tr><td>{taskCost.TaskName}</td><td>{taskCost.Count}x</td><td class='price'>{taskCost.Cost:C}</td></tr>");
            }
            sb.AppendLine("</table>");
        }
        
        // Detaillierte Einträge
        if (entries != null && entries.Count > 0)
        {
            sb.AppendLine("<h2>📝 Detaillierte Einträge</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Datum</th><th>Uhrzeit</th><th>Aufgabe</th><th>Dauer/Anzahl</th><th class='price'>Preis</th></tr>");
            
            foreach (var entry in entries.OrderBy(e => e.EntryDate).ThenBy(e => e.StartTime))
            {
                var taskName = entry.Task?.Name ?? "Unbekannt";
                var durationInfo = entry.Task?.BillingType == BillingType.PerHour 
                    ? $"{entry.DurationInHours:F1}h" 
                    : $"{entry.Count}x";
                
                sb.AppendLine($"<tr>");
                sb.AppendLine($"<td>{entry.EntryDate:dd.MM.yyyy}</td>");
                sb.AppendLine($"<td>{entry.StartTime:hh\\:mm}</td>");
                sb.AppendLine($"<td>{taskName}</td>");
                sb.AppendLine($"<td>{durationInfo}</td>");
                sb.AppendLine($"<td class='price'>{entry.TotalPrice:C}</td>");
                sb.AppendLine($"</tr>");
            }
            sb.AppendLine("</table>");
        }
        
        // Footer
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine($"<p>Erstellt am: {DateTime.Now:dd.MM.yyyy HH:mm}</p>");
        sb.AppendLine("<p>Generiert mit Taschengeld App</p>");
        sb.AppendLine("</div>");
        
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }

    private async Task ShareAsText(CostSummary summary)
    {
        var shareText = new StringBuilder();
        shareText.AppendLine($"📊 Taschengeld Übersicht - {summary.Period}");
        shareText.AppendLine();
        shareText.AppendLine($"💰 Gesamtverdienst: {summary.TotalCost:C}");
        shareText.AppendLine($"📝 Anzahl Einträge: {summary.EntryCount}");
        shareText.AppendLine();
        
        if (summary.TaskCosts != null && summary.TaskCosts.Count > 0)
        {
            shareText.AppendLine("Aufgaben:");
            foreach (var taskCost in summary.TaskCosts)
            {
                shareText.AppendLine($"  • {taskCost.TaskName}: {taskCost.Cost:C}");
            }
        }
        
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = shareText.ToString(),
            Title = $"Taschengeld - {summary.Period}"
        });
    }
}

