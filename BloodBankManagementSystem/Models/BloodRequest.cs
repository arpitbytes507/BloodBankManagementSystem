namespace BloodBankManagementSystem.Models;

/// <summary>
/// Represents a blood requisition from a hospital or patient.
/// </summary>
public class BloodRequest
{
    public int RequestId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public int BloodGroupId { get; set; }
    public string BloodGroupName { get; set; } = string.Empty;
    public int UnitsRequired { get; set; }
    public DateTime RequestDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Pending"; // "Pending", "Fulfilled", "Rejected"
    public string? Notes { get; set; }
}
