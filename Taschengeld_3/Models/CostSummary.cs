namespace Taschengeld_3.Models;

public class CostSummary
{
    public string Period { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public int EntryCount { get; set; }
    public List<TaskCostSummary> TaskCosts { get; set; } = new();
}

public class TaskCostSummary
{
    public string TaskName { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public int Count { get; set; }
}
