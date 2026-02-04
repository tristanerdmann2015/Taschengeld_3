using SQLite;

namespace Taschengeld_3.Models;

public class TimeEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int TaskId { get; set; }
    [Ignore]
    public TaskItem? Task { get; set; }
    public DateTime EntryDate { get; set; }
    public string StartTimeString { get; set; } = "12:00:00";  // Store TimeSpan as string
    public double DurationInHours { get; set; } = 0;
    public int Count { get; set; } = 0;
    public double TotalPrice { get; set; }  // Changed from decimal to double
    public string Notes { get; set; } = string.Empty;

    // Property for convenience in code
    [Ignore]
    public TimeSpan StartTime
    {
        get => TimeSpan.TryParse(StartTimeString, out var ts) ? ts : new TimeSpan(12, 0, 0);
        set => StartTimeString = value.ToString(@"hh\:mm\:ss");
    }

    public void CalculatePrice()
    {
        if (Task == null) return;

        TotalPrice = Task.BillingType == BillingType.PerHour
            ? DurationInHours * Task.Price
            : Count * Task.Price;
    }

    public string GetDisplayTime()
    {
        return StartTime.ToString(@"hh\:mm");
    }

    public string GetDisplayDuration()
    {
        if (Task?.BillingType == BillingType.PerHour)
        {
            return $"{DurationInHours:F2} h";
        }
        else
        {
            return $"{Count} x";
        }
    }
}

