using BloodBankManagementSystem.Data;
using BloodBankManagementSystem.Models;

namespace BloodBankManagementSystem.Services;

/// <summary>
/// Aggregates database counts and inventory statistics for the administrative dashboard.
/// </summary>
public class DashboardService
{
    private readonly InventoryService _inventoryService = new();

    public DashboardStats GetDashboardStats()
    {
        var stats = new DashboardStats();

        using var connection = DatabaseHelper.GetOpenConnection();

        // 1. Total Registered Donors
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Donors;";
            stats.TotalDonors = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        // 2. Total Available Blood Units
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COALESCE(SUM(UnitsAvailable), 0) FROM BloodInventory;";
            stats.TotalAvailableUnits = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        // 3. Pending Requests
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM BloodRequests WHERE Status = 'Pending';";
            stats.PendingRequests = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        // 4. Fulfilled Requests
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM BloodRequests WHERE Status = 'Fulfilled';";
            stats.FulfilledRequests = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        // 5. Rejected Requests
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM BloodRequests WHERE Status = 'Rejected';";
            stats.RejectedRequests = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        // 6. Detailed Inventory Summary
        stats.InventorySummary = _inventoryService.GetAllInventory();

        return stats;
    }
}
