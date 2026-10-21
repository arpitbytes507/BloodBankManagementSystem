namespace BloodBankManagementSystem.Models;

/// <summary>
/// Represents the inventory status of a specific blood group.
/// </summary>
public class BloodInventory
{
    public int InventoryId { get; set; }
    public int BloodGroupId { get; set; }
    public string BloodGroupName { get; set; } = string.Empty;
    public int UnitsAvailable { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Helper status for college presentation: Normal, Low, or Critical stock levels.
    /// </summary>
    public string StockStatus => UnitsAvailable switch
    {
        <= 2 => "Critical",
        <= 5 => "Low Stock",
        _ => "Adequate"
    };
}
