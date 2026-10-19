using BloodBankManagementSystem.Data;
using BloodBankManagementSystem.Models;
using BloodBankManagementSystem.Services;

namespace BloodBankManagementSystem.Tests;

/// <summary>
/// Comprehensive verification test suite for Blood Bank Management System.
/// Validates SQLite schema, foreign-key enforcement, CRUD operations,
/// input validations, atomic transactions, inventory constraints, and CSV export.
/// </summary>
internal static class Program
{
    private static int _passCount = 0;
    private static int _failCount = 0;

    private static void Main()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("===================================================================");
        Console.WriteLine("  Blood Bank Management System - Automated Verification Suite");
        Console.WriteLine("===================================================================");
        Console.ResetColor();

        string testDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_bloodbank.db");
        if (File.Exists(testDbPath)) File.Delete(testDbPath);
        DatabaseHelper.DatabasePath = testDbPath;

        RunTest("1. Database Schema & 8 Blood Groups Seeding", TestDatabaseInitialization);
        RunTest("2. Foreign-Key Pragma & Non-Negative Inventory Constraint", TestDatabaseConstraints);
        RunTest("3. Donor Validation Rules (Age >= 18, Required Fields)", TestDonorValidations);
        RunTest("4. Donor CRUD Operations & Search/Filter", TestDonorCrudAndSearch);
        RunTest("5. Blood Inventory Stock Adjustment & Negative Prevention", TestInventoryOperations);
        RunTest("6. Atomic Transaction Fulfillment with Inventory Deduction", TestTransactionalFulfillmentSuccess);
        RunTest("7. Transaction Rollback on Insufficient Stock", TestTransactionalFulfillmentRollback);
        RunTest("8. Duplicate Fulfillment Prevention", TestPreventDuplicateFulfillment);
        RunTest("9. Blood Requisition Rejection Workflow", TestRequisitionRejection);
        RunTest("10. Dashboard Aggregated Statistics Calculation", TestDashboardMetrics);
        RunTest("11. Standard RFC-4180 CSV Data Export", TestCsvExports);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("===================================================================");
        Console.ResetColor();

        if (_failCount == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ALL TESTS PASSED! ({_passCount}/{_passCount} assertions succeeded)");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  TESTS FAILED: {_failCount} failed, {_passCount} passed");
            Console.ResetColor();
            Environment.Exit(1);
        }

        // Clean up test database
        try
        {
            if (File.Exists(testDbPath)) File.Delete(testDbPath);
        }
        catch { /* ignored */ }
    }

    private static void RunTest(string testName, Action testAction)
    {
        Console.Write($"Running {testName} ... ");
        try
        {
            testAction();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[PASS]");
            Console.ResetColor();
            _passCount++;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[FAIL]");
            Console.WriteLine($"   Error: {ex.Message}");
            Console.ResetColor();
            _failCount++;
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"Assertion failed: {message}");
    }

    private static void TestDatabaseInitialization()
    {
        DatabaseHelper.InitializeDatabase();

        var bgService = new BloodGroupService();
        var groups = bgService.GetAllBloodGroups();

        Assert(groups.Count == 8, $"Expected 8 blood groups, got {groups.Count}");

        string[] expected = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        for (int i = 0; i < expected.Length; i++)
        {
            Assert(groups[i].GroupName == expected[i], $"Group {i} expected {expected[i]} but got {groups[i].GroupName}");
        }

        var invService = new InventoryService();
        var invList = invService.GetAllInventory();
        Assert(invList.Count == 8, $"Expected 8 inventory records, got {invList.Count}");
        Assert(invList.All(x => x.UnitsAvailable == 0), "Initial inventory must be 0 units for all groups");
    }

    private static void TestDatabaseConstraints()
    {
        using var conn = DatabaseHelper.GetOpenConnection();

        // 1. Check foreign key pragma
        using var pragmaCmd = conn.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys;";
        var pragma = Convert.ToInt32(pragmaCmd.ExecuteScalar());
        Assert(pragma == 1, "Foreign keys must be enabled (PRAGMA foreign_keys = 1)");

        // 2. Check foreign key violation throws
        bool fkThrew = false;
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Donors (FullName, Age, Gender, Phone, BloodGroupId, CreatedAt) VALUES ('Orphan', 25, 'Male', '123', 999, '2024-01-01');";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            fkThrew = true;
        }
        Assert(fkThrew, "Inserting donor with invalid BloodGroupId must violate Foreign Key");

        // 3. Check CHECK(UnitsAvailable >= 0) constraint
        bool checkThrew = false;
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE BloodInventory SET UnitsAvailable = -5 WHERE BloodGroupId = 1;";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            checkThrew = true;
        }
        Assert(checkThrew, "Database CHECK constraint must prevent negative UnitsAvailable");
    }

    private static void TestDonorValidations()
    {
        var donorService = new DonorService();

        // Under 18
        bool under18Threw = false;
        try
        {
            donorService.AddDonor(new Donor { FullName = "Minor", Age = 17, Gender = "Male", Phone = "123", BloodGroupId = 1 });
        }
        catch (ArgumentException)
        {
            under18Threw = true;
        }
        Assert(under18Threw, "Donor under 18 must be rejected");

        // Empty Name
        bool emptyNameThrew = false;
        try
        {
            donorService.AddDonor(new Donor { FullName = "   ", Age = 25, Gender = "Male", Phone = "123", BloodGroupId = 1 });
        }
        catch (ArgumentException)
        {
            emptyNameThrew = true;
        }
        Assert(emptyNameThrew, "Empty donor name must be rejected");

        // Empty Phone
        bool emptyPhoneThrew = false;
        try
        {
            donorService.AddDonor(new Donor { FullName = "No Phone", Age = 25, Gender = "Male", Phone = "", BloodGroupId = 1 });
        }
        catch (ArgumentException)
        {
            emptyPhoneThrew = true;
        }
        Assert(emptyPhoneThrew, "Empty donor phone must be rejected");
    }

    private static void TestDonorCrudAndSearch()
    {
        var donorService = new DonorService();

        // Add
        var donor = new Donor
        {
            FullName = "Arthur Pendelton",
            Age = 32,
            Gender = "Male",
            Phone = "555-4321",
            Email = "arthur@example.com",
            City = "Camelot",
            BloodGroupId = 1, // A+
            LastDonationDate = new DateTime(2024, 6, 1)
        };
        donorService.AddDonor(donor);

        // Retrieve and Search
        var searchByName = donorService.GetAllDonors("Pendelton", null);
        Assert(searchByName.Count == 1, "Search by name failed");
        var created = searchByName[0];
        Assert(created.FullName == "Arthur Pendelton", "Full name mismatch");
        Assert(created.BloodGroupName == "A+", "BloodGroupName mismatch");

        // Search by Phone
        var searchByPhone = donorService.GetAllDonors("4321", null);
        Assert(searchByPhone.Count == 1, "Search by phone failed");

        // Filter by Blood Group
        var filterByBg = donorService.GetAllDonors(null, 1);
        Assert(filterByBg.Any(d => d.DonorId == created.DonorId), "Filter by BloodGroupId failed");

        // Update
        created.FullName = "Arthur King";
        created.Age = 35;
        donorService.UpdateDonor(created);

        var updated = donorService.GetDonorById(created.DonorId);
        Assert(updated != null && updated.FullName == "Arthur King" && updated.Age == 35, "Donor update failed");

        // Delete
        donorService.DeleteDonor(created.DonorId);
        var deleted = donorService.GetDonorById(created.DonorId);
        Assert(deleted == null, "Donor delete failed");
    }

    private static void TestInventoryOperations()
    {
        var invService = new InventoryService();

        // 1. Add 10 units to O+ (Group ID 7)
        invService.AdjustStock(7, 10, "Donation drive intake");
        var invOPlus = invService.GetByBloodGroupId(7);
        Assert(invOPlus != null && invOPlus.UnitsAvailable == 10, $"Expected 10 units of O+, got {invOPlus?.UnitsAvailable}");
        Assert(invOPlus!.StockStatus == "Adequate", "Status should be Adequate for 10 units");

        // 2. Prevent deducting 15 units when only 10 available
        bool prevented = false;
        try
        {
            invService.AdjustStock(7, -15, "Excessive deduction");
        }
        catch (InvalidOperationException)
        {
            prevented = true;
        }
        Assert(prevented, "Stock deduction exceeding available units must throw InvalidOperationException");

        // Check stock was not altered
        invOPlus = invService.GetByBloodGroupId(7);
        Assert(invOPlus!.UnitsAvailable == 10, "Stock should remain 10 after failed deduction");
    }

    private static void TestTransactionalFulfillmentSuccess()
    {
        var invService = new InventoryService();
        var reqService = new RequestService();

        // Prepare inventory: B+ (Group ID 3) has 8 units
        invService.AdjustStock(3, 8, "Stocking B+ for testing");

        // Create requisition: 3 units of B+
        var req = new BloodRequest
        {
            PatientName = "Bruce Wayne",
            HospitalName = "Gotham General Hospital",
            ContactNumber = "555-BAT1",
            BloodGroupId = 3,
            UnitsRequired = 3,
            Notes = "Emergency trauma surgery"
        };
        reqService.AddRequest(req);

        var allRequests = reqService.GetAllRequests("Pending", 3);
        Assert(allRequests.Count > 0, "Request should be saved in Pending status");
        int reqId = allRequests[0].RequestId;

        // Fulfill request via transaction
        var (success, msg) = reqService.FulfillRequest(reqId);
        Assert(success, $"FulfillRequest should succeed: {msg}");

        // Verify request is Fulfilled
        var fulfilledReq = reqService.GetRequestById(reqId);
        Assert(fulfilledReq != null && fulfilledReq.Status == "Fulfilled", "Request status must be Fulfilled");

        // Verify inventory deducted: 8 - 3 = 5 units
        var invBPlus = invService.GetByBloodGroupId(3);
        Assert(invBPlus != null && invBPlus.UnitsAvailable == 5, $"Expected 5 units remaining, got {invBPlus?.UnitsAvailable}");
    }

    private static void TestTransactionalFulfillmentRollback()
    {
        var invService = new InventoryService();
        var reqService = new RequestService();

        // Current stock of B+ is 5 units. Requisition asks for 10 units.
        var req = new BloodRequest
        {
            PatientName = "Clark Kent",
            HospitalName = "Metropolis Medical",
            ContactNumber = "555-SPMN",
            BloodGroupId = 3,
            UnitsRequired = 10,
            Notes = "High volume requirement exceeding stock"
        };
        reqService.AddRequest(req);

        var allRequests = reqService.GetAllRequests("Pending", 3);
        int reqId = allRequests.First(r => r.PatientName == "Clark Kent").RequestId;

        // Fulfill should fail due to insufficient stock
        var (success, msg) = reqService.FulfillRequest(reqId);
        Assert(!success, "FulfillRequest must fail when required units exceed stock");
        Assert(msg.Contains("Insufficient inventory"), $"Message should state insufficient inventory: {msg}");

        // Verify transaction rolled back: stock unchanged at 5 units
        var invBPlus = invService.GetByBloodGroupId(3);
        Assert(invBPlus!.UnitsAvailable == 5, $"Stock must remain 5 after rollback, got {invBPlus.UnitsAvailable}");

        // Verify request remains Pending
        var pendingReq = reqService.GetRequestById(reqId);
        Assert(pendingReq!.Status == "Pending", "Request status must remain Pending after rollback");
    }

    private static void TestPreventDuplicateFulfillment()
    {
        var reqService = new RequestService();

        // Find fulfilled request
        var fulfilledList = reqService.GetAllRequests("Fulfilled", null);
        Assert(fulfilledList.Count > 0, "Expected at least one fulfilled request from previous tests");
        int reqId = fulfilledList[0].RequestId;

        // Attempting to fulfill again
        var (success, msg) = reqService.FulfillRequest(reqId);
        Assert(!success, "Fulfilling an already fulfilled request must fail");
        Assert(msg.Contains("Only 'Pending' requests can be fulfilled"), $"Expected pending message, got: {msg}");
    }

    private static void TestRequisitionRejection()
    {
        var reqService = new RequestService();

        var req = new BloodRequest
        {
            PatientName = "Diana Prince",
            HospitalName = "Themyscira Clinic",
            ContactNumber = "555-WW01",
            BloodGroupId = 5, // AB+
            UnitsRequired = 2,
            Notes = "Requisition to be rejected"
        };
        reqService.AddRequest(req);

        var pendingList = reqService.GetAllRequests("Pending", 5);
        int reqId = pendingList.First(r => r.PatientName == "Diana Prince").RequestId;

        var (success, msg) = reqService.RejectRequest(reqId, "Incomplete medical chart");
        Assert(success, $"RejectRequest should succeed: {msg}");

        var rejected = reqService.GetRequestById(reqId);
        Assert(rejected != null && rejected.Status == "Rejected", "Status must be Rejected");
        Assert(rejected!.Notes?.Contains("Incomplete medical chart") == true, "Rejection reason must be appended to notes");

        // Attempt to fulfill a rejected request must fail
        var (fulfillSuccess, _) = reqService.FulfillRequest(reqId);
        Assert(!fulfillSuccess, "Fulfilling a rejected request must fail");
    }

    private static void TestDashboardMetrics()
    {
        var dbService = new DashboardService();
        var stats = dbService.GetDashboardStats();

        Assert(stats.InventorySummary.Count == 8, "InventorySummary must have 8 groups");
        Assert(stats.TotalAvailableUnits >= 0, "TotalAvailableUnits must be >= 0");
        Assert(stats.FulfilledRequests >= 1, "FulfilledRequests must be at least 1");
        Assert(stats.RejectedRequests >= 1, "RejectedRequests must be at least 1");
    }

    private static void TestCsvExports()
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        string donorsCsv = Path.Combine(dir, "test_donors.csv");
        string invCsv = Path.Combine(dir, "test_inventory.csv");
        string reqCsv = Path.Combine(dir, "test_requests.csv");

        var donorService = new DonorService();
        donorService.AddDonor(new Donor { FullName = "Sample, CSV Donor", Age = 40, Gender = "Female", Phone = "555-CSV1", BloodGroupId = 2 });

        var donors = donorService.GetAllDonors();
        CsvExportService.ExportDonorsToCsv(donorsCsv, donors);
        Assert(File.Exists(donorsCsv), "Donors CSV must be generated");
        string donorContent = File.ReadAllText(donorsCsv);
        Assert(donorContent.Contains("\"Sample, CSV Donor\""), "CSV must escape commas with quotes");

        var inv = new InventoryService().GetAllInventory();
        CsvExportService.ExportInventoryToCsv(invCsv, inv);
        Assert(File.Exists(invCsv), "Inventory CSV must be generated");

        var reqs = new RequestService().GetAllRequests();
        CsvExportService.ExportRequestsToCsv(reqCsv, reqs);
        Assert(File.Exists(reqCsv), "Requests CSV must be generated");

        // Clean up CSV test files
        File.Delete(donorsCsv);
        File.Delete(invCsv);
        File.Delete(reqCsv);
    }
}
