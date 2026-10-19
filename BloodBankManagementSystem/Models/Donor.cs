namespace BloodBankManagementSystem.Models;

/// <summary>
/// Represents a registered blood donor.
/// </summary>
public class Donor
{
    public int DonorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? City { get; set; }
    public int BloodGroupId { get; set; }
    public string BloodGroupName { get; set; } = string.Empty;
    public DateTime? LastDonationDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string FormattedLastDonation => LastDonationDate?.ToString("yyyy-MM-dd") ?? "Never";
}
