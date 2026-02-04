using SQLite;

namespace Taschengeld_3.Models;

public class TaskItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }  // Changed from decimal to double (SQLite-net doesn't support decimal)
    public int BillingTypeValue { get; set; } = (int)BillingType.PerHour;  // Store as int for SQLite
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
    
    // Property for convenience in code
    [Ignore]
    public BillingType BillingType
    {
        get => (BillingType)BillingTypeValue;
        set => BillingTypeValue = (int)value;
    }
}

public enum BillingType
{
    PerHour = 0,
    PerCount = 1
}
