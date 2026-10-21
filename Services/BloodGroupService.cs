using BloodBankManagementSystem.Data;
using BloodBankManagementSystem.Models;
using Microsoft.Data.Sqlite;

namespace BloodBankManagementSystem.Services;

/// <summary>
/// Provides retrieval operations for the eight standard ABO/Rh blood groups.
/// </summary>
public class BloodGroupService
{
    /// <summary>
    /// Fetches all 8 blood groups ordered by BloodGroupId.
    /// </summary>
    public List<BloodGroup> GetAllBloodGroups()
    {
        var list = new List<BloodGroup>();

        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT BloodGroupId, GroupName FROM BloodGroups ORDER BY BloodGroupId ASC;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new BloodGroup
            {
                BloodGroupId = reader.GetInt32(0),
                GroupName = reader.GetString(1)
            });
        }

        return list;
    }

    /// <summary>
    /// Finds a blood group by its unique ID.
    /// </summary>
    public BloodGroup? GetById(int id)
    {
        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT BloodGroupId, GroupName FROM BloodGroups WHERE BloodGroupId = @id;";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new BloodGroup
            {
                BloodGroupId = reader.GetInt32(0),
                GroupName = reader.GetString(1)
            };
        }

        return null;
    }
}
