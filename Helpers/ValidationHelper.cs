using BloodBankManagementSystem.Models;

namespace BloodBankManagementSystem.Helpers;

/// <summary>
/// Provides input validation logic and beginner-friendly error messages
/// for donors, inventory adjustments, and blood requests.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates donor registration and update fields.
    /// </summary>
    public static bool ValidateDonor(Donor donor, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(donor.FullName))
        {
            errorMessage = "Donor full name is required.";
            return false;
        }

        if (donor.Age < 18)
        {
            errorMessage = "Donor must be at least 18 years old to be eligible to donate blood.";
            return false;
        }

        if (donor.Age > 65)
        {
            errorMessage = "Standard blood donor age limit is 65 years.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(donor.Phone))
        {
            errorMessage = "Donor contact phone number is required.";
            return false;
        }

        if (donor.BloodGroupId <= 0)
        {
            errorMessage = "Please select a valid blood group.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(donor.Gender))
        {
            errorMessage = "Please select donor gender.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// Validates blood requisition fields.
    /// </summary>
    public static bool ValidateRequest(BloodRequest request, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(request.PatientName))
        {
            errorMessage = "Patient name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.HospitalName))
        {
            errorMessage = "Hospital / Clinic name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.ContactNumber))
        {
            errorMessage = "Emergency contact number is required.";
            return false;
        }

        if (request.BloodGroupId <= 0)
        {
            errorMessage = "Please select the requested blood group.";
            return false;
        }

        if (request.UnitsRequired <= 0)
        {
            errorMessage = "Units required must be at least 1 unit.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// Validates stock adjustment operation.
    /// </summary>
    public static bool ValidateStockAdjustment(int currentUnits, int adjustment, out string errorMessage)
    {
        if (adjustment == 0)
        {
            errorMessage = "Stock adjustment quantity cannot be zero.";
            return false;
        }

        if (currentUnits + adjustment < 0)
        {
            errorMessage = $"Cannot deduct {Math.Abs(adjustment)} units. Available stock is only {currentUnits} units. Stock cannot become negative.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
