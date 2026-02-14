#if ANDROID
using Android.Graphics.Pdf;
using Taschengeld_3.Models;
using Paint = Android.Graphics.Paint;
using Color = Android.Graphics.Color;
using Typeface = Android.Graphics.Typeface;
using TypefaceStyle = Android.Graphics.TypefaceStyle;

namespace Taschengeld_3.Services;

/// <summary>
/// Android-spezifischer PDF-Generator mit nativer PdfDocument API
/// </summary>
public class AndroidPdfGenerator
{
    private const int PageWidth = 595;  // A4 in points (72 dpi)
    private const int PageHeight = 842;
    private const int Margin = 40;
    private const int LineHeight = 20;

    public async Task<string> GeneratePdfAsync(CostSummary summary, List<TimeEntry> entries)
    {
        var fileName = $"Taschengeld_{summary.Period.Replace(" ", "_").Replace("/", "-")}.pdf";
        var filePath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);

        await Task.Run(() =>
        {
            using var document = new PdfDocument();
            var pageInfo = new PdfDocument.PageInfo.Builder(PageWidth, PageHeight, 1).Create();
            var page = document.StartPage(pageInfo);
            var canvas = page.Canvas;

            float y = Margin;

            // Farben
            var purpleColor = Color.Rgb(81, 43, 212);
            var greenColor = Color.Rgb(76, 175, 80);
            var grayColor = Color.Rgb(100, 100, 100);
            var blackColor = Color.Black;

            // Paint-Objekte
            var titlePaint = new Paint { TextSize = 24, Color = purpleColor };
            titlePaint.SetTypeface(Typeface.Create(Typeface.Default, TypefaceStyle.Bold));

            var headerPaint = new Paint { TextSize = 14, Color = purpleColor };
            headerPaint.SetTypeface(Typeface.Create(Typeface.Default, TypefaceStyle.Bold));

            var normalPaint = new Paint { TextSize = 12, Color = blackColor };
            var smallPaint = new Paint { TextSize = 10, Color = grayColor };
            var pricePaint = new Paint { TextSize = 12, Color = greenColor };
            pricePaint.SetTypeface(Typeface.Create(Typeface.Default, TypefaceStyle.Bold));

            var linePaint = new Paint { Color = purpleColor, StrokeWidth = 2 };

            // Titel
            canvas.DrawText("Taschengeld Abrechnung", Margin, y + 24, titlePaint);
            y += 35;

            // Linie unter Titel
            canvas.DrawLine(Margin, y, PageWidth - Margin, y, linePaint);
            y += 20;

            // Zeitraum
            canvas.DrawText($"Zeitraum: {summary.Period}", Margin, y, normalPaint);
            y += 25;

            // Zusammenfassung Box
            var boxPaint = new Paint { Color = Color.Rgb(240, 240, 255) };
            canvas.DrawRect(Margin, y, PageWidth - Margin, y + 60, boxPaint);
            
            y += 25;
            canvas.DrawText("Gesamtverdienst:", Margin + 10, y, headerPaint);
            canvas.DrawText($"{summary.TotalCost:C}", PageWidth - Margin - 100, y, pricePaint);
            y += 20;
            canvas.DrawText($"Anzahl Eintraege: {summary.EntryCount}", Margin + 10, y, normalPaint);
            y += 30;

            // Aufgaben-Uebersicht
            if (summary.TaskCosts != null && summary.TaskCosts.Count > 0)
            {
                y += 10;
                canvas.DrawText("Aufgaben-Uebersicht", Margin, y, headerPaint);
                y += 5;
                canvas.DrawLine(Margin, y, PageWidth - Margin, y, linePaint);
                y += 15;

                // Tabellenkopf
                canvas.DrawText("Aufgabe", Margin, y, normalPaint);
                canvas.DrawText("Anzahl", Margin + 300, y, normalPaint);
                canvas.DrawText("Verdienst", Margin + 400, y, normalPaint);
                y += 15;

                foreach (var taskCost in summary.TaskCosts)
                {
                    // Pruefen ob neue Seite noetig
                    if (y > PageHeight - Margin - 50)
                    {
                        document.FinishPage(page);
                        pageInfo = new PdfDocument.PageInfo.Builder(PageWidth, PageHeight, document.Pages.Count + 1).Create();
                        page = document.StartPage(pageInfo);
                        canvas = page.Canvas;
                        y = Margin;
                    }

                    canvas.DrawText(TruncateText(taskCost.TaskName, 40), Margin, y, normalPaint);
                    canvas.DrawText($"{taskCost.Count}x", Margin + 300, y, normalPaint);
                    canvas.DrawText($"{taskCost.Cost:C}", Margin + 400, y, pricePaint);
                    y += LineHeight;
                }
            }

            // Detaillierte Eintraege
            if (entries != null && entries.Count > 0)
            {
                y += 20;
                
                // Pruefen ob neue Seite noetig
                if (y > PageHeight - Margin - 100)
                {
                    document.FinishPage(page);
                    pageInfo = new PdfDocument.PageInfo.Builder(PageWidth, PageHeight, document.Pages.Count + 1).Create();
                    page = document.StartPage(pageInfo);
                    canvas = page.Canvas;
                    y = Margin;
                }

                canvas.DrawText("Detaillierte Eintraege", Margin, y, headerPaint);
                y += 5;
                canvas.DrawLine(Margin, y, PageWidth - Margin, y, linePaint);
                y += 15;

                // Tabellenkopf
                canvas.DrawText("Datum", Margin, y, smallPaint);
                canvas.DrawText("Zeit", Margin + 70, y, smallPaint);
                canvas.DrawText("Aufgabe", Margin + 120, y, smallPaint);
                canvas.DrawText("Dauer", Margin + 320, y, smallPaint);
                canvas.DrawText("Preis", Margin + 400, y, smallPaint);
                y += 15;

                foreach (var entry in entries.OrderBy(e => e.EntryDate).ThenBy(e => e.StartTime))
                {
                    // Pruefen ob neue Seite noetig
                    if (y > PageHeight - Margin - 30)
                    {
                        document.FinishPage(page);
                        pageInfo = new PdfDocument.PageInfo.Builder(PageWidth, PageHeight, document.Pages.Count + 1).Create();
                        page = document.StartPage(pageInfo);
                        canvas = page.Canvas;
                        y = Margin;
                    }

                    var taskName = entry.Task?.Name ?? "Unbekannt";
                    var durationInfo = entry.Task?.BillingType == BillingType.PerHour
                        ? $"{entry.DurationInHours:F1}h"
                        : $"{entry.Count}x";

                    canvas.DrawText(entry.EntryDate.ToString("dd.MM.yy"), Margin, y, normalPaint);
                    canvas.DrawText(entry.StartTime.ToString(@"hh\:mm"), Margin + 70, y, normalPaint);
                    canvas.DrawText(TruncateText(taskName, 25), Margin + 120, y, normalPaint);
                    canvas.DrawText(durationInfo, Margin + 320, y, normalPaint);
                    canvas.DrawText($"{entry.TotalPrice:C}", Margin + 400, y, pricePaint);
                    y += LineHeight;
                }
            }

            // Footer
            y = PageHeight - Margin;
            canvas.DrawLine(Margin, y - 15, PageWidth - Margin, y - 15, new Paint { Color = grayColor, StrokeWidth = 1 });
            canvas.DrawText($"Erstellt am {DateTime.Now:dd.MM.yyyy HH:mm} - Taschengeld App", Margin, y, smallPaint);

            document.FinishPage(page);

            // Speichern
            using var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create);
            document.WriteTo(stream);
        });

        return filePath;
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
    }
}
#endif
