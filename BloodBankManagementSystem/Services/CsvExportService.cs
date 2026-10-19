using System.Text;
using BloodBankManagementSystem.Models;

namespace BloodBankManagementSystem.Services;

/// <summary>
/// Exports donors, blood inventory, and blood requests into standard RFC-4180 CSV files.
/// </summary>
public static class CsvExportService
{
    /// <summary>
    /// Exports the donors list to a CSV file.
    /// </summary>
    public static void ExportDonorsToCsv(string filePath, IEnumerable<Donor> donors)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DonorId,FullName,Age,Gender,Phone,Email,City,BloodGroup,LastDonationDate,CreatedAt");

        foreach (var d in donors)
        {
            sb.AppendLine(string.Join(",",
                d.DonorId,
                EscapeCsv(d.FullName),
                d.Age,
                EscapeCsv(d.Gender),
                EscapeCsv(d.Phone),
                EscapeCsv(d.Email ?? string.Empty),
                EscapeCsv(d.City ?? string.Empty),
                EscapeCsv(d.BloodGroupName),
                EscapeCsv(d.FormattedLastDonation),
                EscapeCsv(d.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
            ));
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Exports blood inventory levels to a CSV file.
    /// </summary>
    public static void ExportInventoryToCsv(string filePath, IEnumerable<BloodInventory> inventory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("InventoryId,BloodGroupId,BloodGroup,UnitsAvailable,StockStatus,LastUpdatedAt");

        foreach (var i in inventory)
        {
            sb.AppendLine(string.Join(",",
                i.InventoryId,
                i.BloodGroupId,
                EscapeCsv(i.BloodGroupName),
                i.UnitsAvailable,
                EscapeCsv(i.StockStatus),
                EscapeCsv(i.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
            ));
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Exports blood requests to a CSV file.
    /// </summary>
    public static void ExportRequestsToCsv(string filePath, IEnumerable<BloodRequest> requests)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RequestId,PatientName,HospitalName,ContactNumber,BloodGroup,UnitsRequired,RequestDate,Status,Notes");

        foreach (var r in requests)
        {
            sb.AppendLine(string.Join(",",
                r.RequestId,
                EscapeCsv(r.PatientName),
                EscapeCsv(r.HospitalName),
                EscapeCsv(r.ContactNumber),
                EscapeCsv(r.BloodGroupName),
                r.UnitsRequired,
                EscapeCsv(r.RequestDate.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(r.Status),
                EscapeCsv(r.Notes ?? string.Empty)
            ));
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Escapes CSV values according to RFC 4180 standard.
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return $"\"{value}\"";
    }
}
