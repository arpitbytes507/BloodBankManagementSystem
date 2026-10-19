namespace BloodBankManagementSystem.Models;

/// <summary>
/// Aggregated metrics displayed on the administrative dashboard.
/// </summary>
public class DashboardStats
{
    public int TotalDonors { get; set; }
    public int TotalAvailableUnits { get; set; }
    public int PendingRequests { get; set; }
    public int FulfilledRequests { get; set; }
    public int RejectedRequests { get; set; }
    public List<BloodInventory> InventorySummary { get; set; } = new();
}
