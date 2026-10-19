using BloodBankManagementSystem.Data;
using BloodBankManagementSystem.Helpers;
using BloodBankManagementSystem.Models;
using Microsoft.Data.Sqlite;

namespace BloodBankManagementSystem.Services;

/// <summary>
/// Manages blood stock inventory operations for all eight blood groups.
/// Ensures non-negative stock constraints and supports prototype demonstration adjustments.
/// </summary>
public class InventoryService
{
    /// <summary>
    /// Retrieves current inventory for all eight blood groups.
    /// </summary>
    public List<BloodInventory> GetAllInventory()
    {
        var list = new List<BloodInventory>();

        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT bi.InventoryId, bi.BloodGroupId, bg.GroupName, bi.UnitsAvailable, bi.UpdatedAt
            FROM BloodInventory bi
            INNER JOIN BloodGroups bg ON bi.BloodGroupId = bg.BloodGroupId
            ORDER BY bi.BloodGroupId ASC;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var inv = new BloodInventory
            {
                InventoryId = reader.GetInt32(0),
                BloodGroupId = reader.GetInt32(1),
                BloodGroupName = reader.GetString(2),
                UnitsAvailable = reader.GetInt32(3)
            };

            if (DateTime.TryParse(reader.GetString(4), out var dt))
            {
                inv.UpdatedAt = dt;
            }

            list.Add(inv);
        }

        return list;
    }

    /// <summary>
    /// Retrieves inventory for a specific blood group.
    /// </summary>
    public BloodInventory? GetByBloodGroupId(int bloodGroupId)
    {
        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT bi.InventoryId, bi.BloodGroupId, bg.GroupName, bi.UnitsAvailable, bi.UpdatedAt
            FROM BloodInventory bi
            INNER JOIN BloodGroups bg ON bi.BloodGroupId = bg.BloodGroupId
            WHERE bi.BloodGroupId = @bgId;";
        cmd.Parameters.AddWithValue("@bgId", bloodGroupId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var inv = new BloodInventory
            {
                InventoryId = reader.GetInt32(0),
                BloodGroupId = reader.GetInt32(1),
                BloodGroupName = reader.GetString(2),
                UnitsAvailable = reader.GetInt32(3)
            };

            if (DateTime.TryParse(reader.GetString(4), out var dt))
            {
                inv.UpdatedAt = dt;
            }

            return inv;
        }

        return null;
    }

    /// <summary>
    /// Adjusts blood units for a blood group.
    /// Used for manual prototype adjustments (e.g. donation drives or lab testing).
    /// Prevents stock from becoming negative.
    /// </summary>
    public void AdjustStock(int bloodGroupId, int unitsAdjustment, string reason = "Demonstration Stock Adjustment")
    {
        using var connection = DatabaseHelper.GetOpenConnection();
        using var tx = connection.BeginTransaction();

        try
        {
            // 1. Fetch current units
            using var selectCmd = connection.CreateCommand();
            selectCmd.Transaction = tx;
            selectCmd.CommandText = "SELECT UnitsAvailable FROM BloodInventory WHERE BloodGroupId = @bgId;";
            selectCmd.Parameters.AddWithValue("@bgId", bloodGroupId);

            var result = selectCmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                throw new KeyNotFoundException($"Blood group ID {bloodGroupId} not found in inventory.");
            }

            int currentUnits = Convert.ToInt32(result);

            // 2. Validate non-negative outcome
            if (!ValidationHelper.ValidateStockAdjustment(currentUnits, unitsAdjustment, out var error))
            {
                throw new InvalidOperationException(error);
            }

            int newUnits = currentUnits + unitsAdjustment;

            // 3. Update stock
            using var updateCmd = connection.CreateCommand();
            updateCmd.Transaction = tx;
            updateCmd.CommandText = @"
                UPDATE BloodInventory
                SET UnitsAvailable = @newUnits,
                    UpdatedAt = @now
                WHERE BloodGroupId = @bgId;";
            updateCmd.Parameters.AddWithValue("@newUnits", newUnits);
            updateCmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            updateCmd.Parameters.AddWithValue("@bgId", bloodGroupId);

            updateCmd.ExecuteNonQuery();

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
