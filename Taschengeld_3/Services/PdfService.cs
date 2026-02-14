using System.Text;
using Taschengeld_3.Models;

namespace Taschengeld_3.Services;

/// <summary>
/// Service zur Berichts-Generierung fuer Taschengeld-Abrechnungen
/// Verwendet HTML fuer maximale Kompatibilitaet auf allen Plattformen
/// </summary>
public class PdfService
{
    public PdfService()
    {
    }

    /// <summary>
    /// Generiert einen HTML-Bericht fuer eine Kostenuebersicht
    /// HTML wird von allen Geraeten unterstuetzt und kann als PDF gedruckt werden
    /// </summary>
    public string GenerateHtmlReport(CostSummary summary, List<TimeEntry> entries)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='de'>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        sb.AppendLine($"<title>Taschengeld Abrechnung - {summary.Period}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("* { box-sizing: border-box; }");
        sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif; padding: 20px; max-width: 800px; margin: 0 auto; background: #f5f5f5; }");
        sb.AppendLine(".container { background: white; padding: 30px; border-radius: 12px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
        sb.AppendLine("h1 { color: #512BD4; border-bottom: 3px solid #512BD4; padding-bottom: 15px; margin-bottom: 20px; font-size: 28px; }");
        sb.AppendLine("h2 { color: #333; margin-top: 30px; font-size: 20px; display: flex; align-items: center; gap: 10px; }");
        sb.AppendLine(".summary { background: linear-gradient(135deg, #512BD4, #7B1FA2); padding: 25px; border-radius: 12px; margin: 25px 0; color: white; }");
        sb.AppendLine(".summary-row { display: flex; justify-content: space-between; padding: 8px 0; align-items: center; }");
        sb.AppendLine(".total { font-size: 32px; font-weight: bold; }");
        sb.AppendLine(".total-label { font-size: 18px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
        sb.AppendLine("th, td { padding: 12px 10px; text-align: left; border-bottom: 1px solid #e0e0e0; }");
        sb.AppendLine("th { background: #512BD4; color: white; font-weight: 600; }");
        sb.AppendLine("tr:nth-child(even) { background: #f9f9f9; }");
        sb.AppendLine("tr:hover { background: #f0f0f0; }");
        sb.AppendLine(".price { text-align: right; font-weight: bold; color: #4CAF50; }");
        sb.AppendLine(".footer { margin-top: 40px; padding-top: 20px; border-top: 2px solid #e0e0e0; color: #666; font-size: 12px; text-align: center; }");
        sb.AppendLine(".coin { font-size: 40px; }");
        sb.AppendLine("@media print { body { background: white; } .container { box-shadow: none; } }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class='container'>");
        
        // Header
        sb.AppendLine($"<h1><span class='coin'>??</span> Taschengeld Abrechnung</h1>");
        sb.AppendLine($"<p style='font-size: 18px; color: #666;'>Zeitraum: <strong>{summary.Period}</strong></p>");
        
        // Zusammenfassung
        sb.AppendLine("<div class='summary'>");
        sb.AppendLine($"<div class='summary-row'><span class='total-label'>Gesamtverdienst:</span><span class='total'>{summary.TotalCost:C}</span></div>");
        sb.AppendLine($"<div class='summary-row'><span>Anzahl Eintraege:</span><span style='font-size: 20px;'>{summary.EntryCount}</span></div>");
        sb.AppendLine("</div>");
        
        // Aufgaben-Uebersicht
        if (summary.TaskCosts != null && summary.TaskCosts.Count > 0)
        {
            sb.AppendLine("<h2>?? Aufgaben-Uebersicht</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Aufgabe</th><th style='text-align:center;'>Anzahl</th><th style='text-align:right;'>Verdienst</th></tr>");
            foreach (var taskCost in summary.TaskCosts)
            {
                sb.AppendLine($"<tr><td>{taskCost.TaskName}</td><td style='text-align:center;'>{taskCost.Count}x</td><td class='price'>{taskCost.Cost:C}</td></tr>");
            }
            sb.AppendLine("</table>");
        }
        
        // Detaillierte Eintraege
        if (entries != null && entries.Count > 0)
        {
            sb.AppendLine("<h2>?? Detaillierte Eintraege</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Datum</th><th>Uhrzeit</th><th>Aufgabe</th><th>Dauer/Anzahl</th><th style='text-align:right;'>Preis</th></tr>");
            
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
        sb.AppendLine("<p>Generiert mit <strong>Taschengeld App</strong> ??</p>");
        sb.AppendLine("</div>");
        
        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }
}
