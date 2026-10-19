namespace BloodBankManagementSystem.Models;

/// <summary>
/// Represents one of the eight standard ABO/Rh blood groups.
/// </summary>
public class BloodGroup
{
    public int BloodGroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;

    public override string ToString() => GroupName;
}
