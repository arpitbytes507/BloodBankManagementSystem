using BloodBankManagementSystem.Data;
using BloodBankManagementSystem.Helpers;
using BloodBankManagementSystem.Models;
using Microsoft.Data.Sqlite;

namespace BloodBankManagementSystem.Services;

/// <summary>
/// Manages blood requisitions from patients and hospitals.
/// Implements atomic transactional fulfillment with stock verification and rollback.
/// </summary>
public class RequestService
{
    /// <summary>
    /// Retrieves blood requests filtered by status and/or blood group.
    /// </summary>
    public List<BloodRequest> GetAllRequests(string? statusFilter = null, int? bloodGroupId = null)
    {
        var requests = new List<BloodRequest>();

        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();

        var query = @"
            SELECT r.RequestId, r.PatientName, r.HospitalName, r.ContactNumber,
                   r.BloodGroupId, bg.GroupName, r.UnitsRequired, r.RequestDate,
                   r.Status, r.Notes
            FROM BloodRequests r
            INNER JOIN BloodGroups bg ON r.BloodGroupId = bg.BloodGroupId
            WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            query += " AND r.Status = @status";
            cmd.Parameters.AddWithValue("@status", statusFilter);
        }

        if (bloodGroupId.HasValue && bloodGroupId.Value > 0)
        {
            query += " AND r.BloodGroupId = @bgId";
            cmd.Parameters.AddWithValue("@bgId", bloodGroupId.Value);
        }

        query += " ORDER BY r.RequestId DESC;";
        cmd.CommandText = query;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            requests.Add(MapRequest(reader));
        }

        return requests;
    }

    /// <summary>
    /// Retrieves a single blood request by ID.
    /// </summary>
    public BloodRequest? GetRequestById(int requestId)
    {
        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT r.RequestId, r.PatientName, r.HospitalName, r.ContactNumber,
                   r.BloodGroupId, bg.GroupName, r.UnitsRequired, r.RequestDate,
                   r.Status, r.Notes
            FROM BloodRequests r
            INNER JOIN BloodGroups bg ON r.BloodGroupId = bg.BloodGroupId
            WHERE r.RequestId = @id;";
        cmd.Parameters.AddWithValue("@id", requestId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapRequest(reader);
        }

        return null;
    }

    /// <summary>
    /// Registers a new blood requisition.
    /// </summary>
    public void AddRequest(BloodRequest request)
    {
        if (!ValidationHelper.ValidateRequest(request, out var error))
        {
            throw new ArgumentException(error);
        }

        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO BloodRequests (PatientName, HospitalName, ContactNumber, BloodGroupId, UnitsRequired, RequestDate, Status, Notes)
            VALUES (@patient, @hospital, @contact, @bgId, @units, @reqDate, 'Pending', @notes);";

        cmd.Parameters.AddWithValue("@patient", request.PatientName.Trim());
        cmd.Parameters.AddWithValue("@hospital", request.HospitalName.Trim());
        cmd.Parameters.AddWithValue("@contact", request.ContactNumber.Trim());
        cmd.Parameters.AddWithValue("@bgId", request.BloodGroupId);
        cmd.Parameters.AddWithValue("@units", request.UnitsRequired);
        cmd.Parameters.AddWithValue("@reqDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@notes", (object?)request.Notes?.Trim() ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Fulfills a pending request using an atomic SQLite transaction.
    /// Checks stock sufficiency, deducts blood units, and marks request as Fulfilled.
    /// Rolls back completely if stock is insufficient or if errors occur.
    /// </summary>
    public (bool Success, string Message) FulfillRequest(int requestId)
    {
        using var connection = DatabaseHelper.GetOpenConnection();
        using var tx = connection.BeginTransaction();

        try
        {
            // 1. Fetch current request state
            using var fetchCmd = connection.CreateCommand();
            fetchCmd.Transaction = tx;
            fetchCmd.CommandText = @"
                SELECT r.RequestId, r.BloodGroupId, bg.GroupName, r.UnitsRequired, r.Status
                FROM BloodRequests r
                INNER JOIN BloodGroups bg ON r.BloodGroupId = bg.BloodGroupId
                WHERE r.RequestId = @id;";
            fetchCmd.Parameters.AddWithValue("@id", requestId);

            int bloodGroupId = 0;
            string bloodGroupName = string.Empty;
            int unitsRequired = 0;
            string currentStatus = string.Empty;

            using (var reader = fetchCmd.ExecuteReader())
            {
                if (!reader.Read())
                {
                    tx.Rollback();
                    return (false, $"Request #{requestId} was not found.");
                }

                bloodGroupId = reader.GetInt32(1);
                bloodGroupName = reader.GetString(2);
                unitsRequired = reader.GetInt32(3);
                currentStatus = reader.GetString(4);
            }

            // 2. Prevent duplicate fulfillment or modifying non-pending requests
            if (!string.Equals(currentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                tx.Rollback();
                return (false, $"Cannot fulfill request #{requestId}. Only 'Pending' requests can be fulfilled (Current Status: {currentStatus}).");
            }

            // 3. Check inventory stock
            using var stockCmd = connection.CreateCommand();
            stockCmd.Transaction = tx;
            stockCmd.CommandText = "SELECT UnitsAvailable FROM BloodInventory WHERE BloodGroupId = @bgId;";
            stockCmd.Parameters.AddWithValue("@bgId", bloodGroupId);

            var stockObj = stockCmd.ExecuteScalar();
            if (stockObj == null || stockObj == DBNull.Value)
            {
                tx.Rollback();
                return (false, $"Inventory record for blood group {bloodGroupName} was not found.");
            }

            int availableUnits = Convert.ToInt32(stockObj);

            if (availableUnits < unitsRequired)
            {
                tx.Rollback();
                return (false, $"Insufficient inventory for {bloodGroupName}!\nAvailable: {availableUnits} units\nRequired: {unitsRequired} units\nShortfall: {unitsRequired - availableUnits} units.\nPlease replenish stock before fulfilling.");
            }

            // 4. Deduct inventory units atomically
            using var deductCmd = connection.CreateCommand();
            deductCmd.Transaction = tx;
            deductCmd.CommandText = @"
                UPDATE BloodInventory
                SET UnitsAvailable = UnitsAvailable - @units,
                    UpdatedAt = @now
                WHERE BloodGroupId = @bgId;";
            deductCmd.Parameters.AddWithValue("@units", unitsRequired);
            deductCmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            deductCmd.Parameters.AddWithValue("@bgId", bloodGroupId);

            int inventoryUpdated = deductCmd.ExecuteNonQuery();
            if (inventoryUpdated == 0)
            {
                tx.Rollback();
                return (false, "Failed to update blood inventory record.");
            }

            // 5. Update request status to 'Fulfilled'
            using var updateReqCmd = connection.CreateCommand();
            updateReqCmd.Transaction = tx;
            updateReqCmd.CommandText = @"
                UPDATE BloodRequests
                SET Status = 'Fulfilled'
                WHERE RequestId = @id;";
            updateReqCmd.Parameters.AddWithValue("@id", requestId);

            int requestUpdated = updateReqCmd.ExecuteNonQuery();
            if (requestUpdated == 0)
            {
                tx.Rollback();
                return (false, "Failed to update request status.");
            }

            // 6. Commit the transaction
            tx.Commit();

            return (true, $"Success! Request #{requestId} fulfilled.\n{unitsRequired} unit(s) of {bloodGroupName} deducted.\nRemaining stock: {availableUnits - unitsRequired} units.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, $"Database transaction failed and was rolled back: {ex.Message}");
        }
    }

    /// <summary>
    /// Rejects a pending request with an optional reason.
    /// Only pending requests may be rejected.
    /// </summary>
    public (bool Success, string Message) RejectRequest(int requestId, string? reason = null)
    {
        using var connection = DatabaseHelper.GetOpenConnection();

        // Check current status
        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT Status, Notes FROM BloodRequests WHERE RequestId = @id;";
        checkCmd.Parameters.AddWithValue("@id", requestId);

        string status;
        string? existingNotes = null;

        using (var reader = checkCmd.ExecuteReader())
        {
            if (!reader.Read())
            {
                return (false, $"Request #{requestId} not found.");
            }

            status = reader.GetString(0);
            if (!reader.IsDBNull(1)) existingNotes = reader.GetString(1);
        }

        if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"Cannot reject request #{requestId}. Current status is already '{status}'.");
        }

        string updatedNotes = string.IsNullOrWhiteSpace(reason)
            ? existingNotes ?? string.Empty
            : (string.IsNullOrWhiteSpace(existingNotes) ? $"Rejection reason: {reason}" : $"{existingNotes} | Rejection reason: {reason}");

        using var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = @"
            UPDATE BloodRequests
            SET Status = 'Rejected',
                Notes = @notes
            WHERE RequestId = @id;";
        updateCmd.Parameters.AddWithValue("@notes", updatedNotes);
        updateCmd.Parameters.AddWithValue("@id", requestId);

        updateCmd.ExecuteNonQuery();

        return (true, $"Request #{requestId} has been marked as Rejected.");
    }

    /// <summary>
    /// Deletes a request record.
    /// </summary>
    public void DeleteRequest(int requestId)
    {
        using var connection = DatabaseHelper.GetOpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM BloodRequests WHERE RequestId = @id;";
        cmd.Parameters.AddWithValue("@id", requestId);

        int rows = cmd.ExecuteNonQuery();
        if (rows == 0)
        {
            throw new KeyNotFoundException($"Request #{requestId} not found.");
        }
    }

    private static BloodRequest MapRequest(SqliteDataReader reader)
    {
        var req = new BloodRequest
        {
            RequestId = reader.GetInt32(0),
            PatientName = reader.GetString(1),
            HospitalName = reader.GetString(2),
            ContactNumber = reader.GetString(3),
            BloodGroupId = reader.GetInt32(4),
            BloodGroupName = reader.GetString(5),
            UnitsRequired = reader.GetInt32(6),
            Status = reader.GetString(8),
            Notes = reader.IsDBNull(9) ? null : reader.GetString(9)
        };

        if (DateTime.TryParse(reader.GetString(7), out var dt))
        {
            req.RequestDate = dt;
        }

        return req;
    }
}
