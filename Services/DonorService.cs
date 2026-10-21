using BloodBankManagementSystem.Data;
using BloodBankManagementSystem.Helpers;
using BloodBankManagementSystem.Models;
using Microsoft.Data.Sqlite;

namespace BloodBankManagementSystem.Services;

/// <summary>
/// Handles all business logic and parameterized database operations for blood donors.
/// </summary>
public class DonorService
{
    /// <summary>
    /// Retrieves donors with optional text search (by name or phone) and blood group filter.
    /// </summary>
    public List<Donor> GetAllDonors(string? searchTerm = null, int? bloodGroupId = null)
    {
        var donors = new List<Donor>();

        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();

        var query = @"
            SELECT d.DonorId, d.FullName, d.Age, d.Gender, d.Phone, d.Email, d.City,
                   d.BloodGroupId, bg.GroupName, d.LastDonationDate, d.CreatedAt
            FROM Donors d
            INNER JOIN BloodGroups bg ON d.BloodGroupId = bg.BloodGroupId
            WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query += " AND (d.FullName LIKE @search OR d.Phone LIKE @search OR d.City LIKE @search)";
            cmd.Parameters.AddWithValue("@search", $"%{searchTerm.Trim()}%");
        }

        if (bloodGroupId.HasValue && bloodGroupId.Value > 0)
        {
            query += " AND d.BloodGroupId = @bgId";
            cmd.Parameters.AddWithValue("@bgId", bloodGroupId.Value);
        }

        query += " ORDER BY d.DonorId DESC;";
        cmd.CommandText = query;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            donors.Add(MapDonor(reader));
        }

        return donors;
    }

    /// <summary>
    /// Retrieves a single donor by ID.
    /// </summary>
    public Donor? GetDonorById(int donorId)
    {
        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT d.DonorId, d.FullName, d.Age, d.Gender, d.Phone, d.Email, d.City,
                   d.BloodGroupId, bg.GroupName, d.LastDonationDate, d.CreatedAt
            FROM Donors d
            INNER JOIN BloodGroups bg ON d.BloodGroupId = bg.BloodGroupId
            WHERE d.DonorId = @id;";
        cmd.Parameters.AddWithValue("@id", donorId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapDonor(reader);
        }

        return null;
    }

    /// <summary>
    /// Registers a new donor after validating business rules.
    /// </summary>
    public void AddDonor(Donor donor)
    {
        if (!ValidationHelper.ValidateDonor(donor, out var error))
        {
            throw new ArgumentException(error);
        }

        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Donors (FullName, Age, Gender, Phone, Email, City, BloodGroupId, LastDonationDate, CreatedAt)
            VALUES (@name, @age, @gender, @phone, @email, @city, @bgId, @lastDate, @createdAt);";

        cmd.Parameters.AddWithValue("@name", donor.FullName.Trim());
        cmd.Parameters.AddWithValue("@age", donor.Age);
        cmd.Parameters.AddWithValue("@gender", donor.Gender);
        cmd.Parameters.AddWithValue("@phone", donor.Phone.Trim());
        cmd.Parameters.AddWithValue("@email", (object?)donor.Email?.Trim() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@city", (object?)donor.City?.Trim() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@bgId", donor.BloodGroupId);
        cmd.Parameters.AddWithValue("@lastDate", donor.LastDonationDate.HasValue
            ? donor.LastDonationDate.Value.ToString("yyyy-MM-dd")
            : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Updates an existing donor's information.
    /// </summary>
    public void UpdateDonor(Donor donor)
    {
        if (donor.DonorId <= 0)
        {
            throw new ArgumentException("Invalid Donor ID.");
        }

        if (!ValidationHelper.ValidateDonor(donor, out var error))
        {
            throw new ArgumentException(error);
        }

        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE Donors
            SET FullName = @name,
                Age = @age,
                Gender = @gender,
                Phone = @phone,
                Email = @email,
                City = @city,
                BloodGroupId = @bgId,
                LastDonationDate = @lastDate
            WHERE DonorId = @id;";

        cmd.Parameters.AddWithValue("@id", donor.DonorId);
        cmd.Parameters.AddWithValue("@name", donor.FullName.Trim());
        cmd.Parameters.AddWithValue("@age", donor.Age);
        cmd.Parameters.AddWithValue("@gender", donor.Gender);
        cmd.Parameters.AddWithValue("@phone", donor.Phone.Trim());
        cmd.Parameters.AddWithValue("@email", (object?)donor.Email?.Trim() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@city", (object?)donor.City?.Trim() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@bgId", donor.BloodGroupId);
        cmd.Parameters.AddWithValue("@lastDate", donor.LastDonationDate.HasValue
            ? donor.LastDonationDate.Value.ToString("yyyy-MM-dd")
            : (object)DBNull.Value);

        var rowsAffected = cmd.ExecuteNonQuery();
        if (rowsAffected == 0)
        {
            throw new KeyNotFoundException($"Donor with ID {donor.DonorId} was not found.");
        }
    }

    /// <summary>
    /// Deletes a donor by ID.
    /// </summary>
    public void DeleteDonor(int donorId)
    {
        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Donors WHERE DonorId = @id;";
        cmd.Parameters.AddWithValue("@id", donorId);

        var rowsAffected = cmd.ExecuteNonQuery();
        if (rowsAffected == 0)
        {
            throw new KeyNotFoundException($"Donor with ID {donorId} was not found.");
        }
    }

    private static Donor MapDonor(SqliteDataReader reader)
    {
        var donor = new Donor
        {
            DonorId = reader.GetInt32(0),
            FullName = reader.GetString(1),
            Age = reader.GetInt32(2),
            Gender = reader.GetString(3),
            Phone = reader.GetString(4),
            Email = reader.IsDBNull(5) ? null : reader.GetString(5),
            City = reader.IsDBNull(6) ? null : reader.GetString(6),
            BloodGroupId = reader.GetInt32(7),
            BloodGroupName = reader.GetString(8)
        };

        if (!reader.IsDBNull(9))
        {
            if (DateTime.TryParse(reader.GetString(9), out var d))
                donor.LastDonationDate = d;
        }

        if (!reader.IsDBNull(10))
        {
            if (DateTime.TryParse(reader.GetString(10), out var c))
                donor.CreatedAt = c;
        }

        return donor;
    }
}
