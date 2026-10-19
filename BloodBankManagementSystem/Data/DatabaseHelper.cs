using Microsoft.Data.Sqlite;

namespace BloodBankManagementSystem.Data;

/// <summary>
/// Manages SQLite database connectivity, schema creation, foreign-key enforcement,
/// and initial seed data for the Blood Bank Management System.
/// </summary>
public static class DatabaseHelper
{
    private static string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bloodbank.db");
    private static string _connectionString = $"Data Source={_dbPath};";

    /// <summary>
    /// Gets or sets the database file path.
    /// Allows test suites to use an isolated temporary database.
    /// </summary>
    public static string DatabasePath
    {
        get => _dbPath;
        set
        {
            _dbPath = value;
            _connectionString = $"Data Source={_dbPath};";
        }
    }

    /// <summary>
    /// Gets the SQLite connection string.
    /// </summary>
    public static string ConnectionString => _connectionString;

    /// <summary>
    /// Creates and opens a new SQLite connection with foreign keys enabled.
    /// In SQLite, foreign-key enforcement must be activated on every connection.
    /// </summary>
    public static SqliteConnection GetOpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
        pragmaCmd.ExecuteNonQuery();

        return connection;
    }

    /// <summary>
    /// Initializes tables, constraints, indexes, and initial seeds.
    /// Safe to call multiple times on application startup.
    /// </summary>
    public static void InitializeDatabase()
    {
        using var connection = GetOpenConnection();

        // 1. Create BloodGroups Table
        const string createBloodGroupsTable = @"
            CREATE TABLE IF NOT EXISTS BloodGroups (
                BloodGroupId INTEGER PRIMARY KEY AUTOINCREMENT,
                GroupName TEXT NOT NULL UNIQUE
            );";

        // 2. Create BloodInventory Table
        const string createInventoryTable = @"
            CREATE TABLE IF NOT EXISTS BloodInventory (
                InventoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                BloodGroupId INTEGER NOT NULL UNIQUE,
                UnitsAvailable INTEGER NOT NULL DEFAULT 0 CHECK(UnitsAvailable >= 0),
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (BloodGroupId) REFERENCES BloodGroups(BloodGroupId) ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS IX_BloodInventory_BloodGroupId ON BloodInventory(BloodGroupId);";

        // 3. Create Donors Table
        const string createDonorsTable = @"
            CREATE TABLE IF NOT EXISTS Donors (
                DonorId INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                Age INTEGER NOT NULL CHECK(Age >= 18),
                Gender TEXT NOT NULL,
                Phone TEXT NOT NULL,
                Email TEXT,
                City TEXT,
                BloodGroupId INTEGER NOT NULL,
                LastDonationDate TEXT,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (BloodGroupId) REFERENCES BloodGroups(BloodGroupId) ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS IX_Donors_BloodGroupId ON Donors(BloodGroupId);
            CREATE INDEX IF NOT EXISTS IX_Donors_Phone ON Donors(Phone);
            CREATE INDEX IF NOT EXISTS IX_Donors_FullName ON Donors(FullName);";

        // 4. Create BloodRequests Table
        const string createRequestsTable = @"
            CREATE TABLE IF NOT EXISTS BloodRequests (
                RequestId INTEGER PRIMARY KEY AUTOINCREMENT,
                PatientName TEXT NOT NULL,
                HospitalName TEXT NOT NULL,
                ContactNumber TEXT NOT NULL,
                BloodGroupId INTEGER NOT NULL,
                UnitsRequired INTEGER NOT NULL CHECK(UnitsRequired > 0),
                RequestDate TEXT NOT NULL,
                Status TEXT NOT NULL DEFAULT 'Pending' CHECK(Status IN ('Pending', 'Fulfilled', 'Rejected')),
                Notes TEXT,
                FOREIGN KEY (BloodGroupId) REFERENCES BloodGroups(BloodGroupId) ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS IX_BloodRequests_BloodGroupId ON BloodRequests(BloodGroupId);
            CREATE INDEX IF NOT EXISTS IX_BloodRequests_Status ON BloodRequests(Status);";

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = createBloodGroupsTable + createInventoryTable + createDonorsTable + createRequestsTable;
            cmd.ExecuteNonQuery();
        }

        // 5. Seed the 8 ABO/Rh Blood Groups and 0-unit Inventory records
        SeedBloodGroupsAndInventory(connection);
    }

    /// <summary>
    /// Seeds the eight standard ABO/Rh blood groups and ensures each group
    /// has a corresponding BloodInventory entry initialized without duplicates.
    /// </summary>
    private static void SeedBloodGroupsAndInventory(SqliteConnection connection)
    {
        string[] standardGroups = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

        foreach (var group in standardGroups)
        {
            // Seed BloodGroup if not existing
            using var insertGroupCmd = connection.CreateCommand();
            insertGroupCmd.CommandText = @"
                INSERT OR IGNORE INTO BloodGroups (GroupName)
                VALUES (@groupName);";
            insertGroupCmd.Parameters.AddWithValue("@groupName", group);
            insertGroupCmd.ExecuteNonQuery();

            // Retrieve BloodGroupId
            using var getIdCmd = connection.CreateCommand();
            getIdCmd.CommandText = "SELECT BloodGroupId FROM BloodGroups WHERE GroupName = @groupName;";
            getIdCmd.Parameters.AddWithValue("@groupName", group);
            var idObj = getIdCmd.ExecuteScalar();
            if (idObj == null || idObj == DBNull.Value) continue;

            int groupId = Convert.ToInt32(idObj);

            // Ensure BloodInventory record exists with default 0 units
            using var insertInventoryCmd = connection.CreateCommand();
            insertInventoryCmd.CommandText = @"
                INSERT OR IGNORE INTO BloodInventory (BloodGroupId, UnitsAvailable, UpdatedAt)
                VALUES (@groupId, 0, @updatedAt);";
            insertInventoryCmd.Parameters.AddWithValue("@groupId", groupId);
            insertInventoryCmd.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            insertInventoryCmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Seeds fictitious demonstration records for an academic presentation if no donors exist.
    /// Does not contain real patient or donor personal info.
    /// </summary>
    public static void SeedSampleDemoDataIfEmpty()
    {
        using var connection = GetOpenConnection();

        // Check if Donors already exist
        using var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM Donors;";
        int donorCount = Convert.ToInt32(countCmd.ExecuteScalar());
        if (donorCount > 0) return; // Already populated

        // Provide starter stock for demo testing
        using var stockCmd = connection.CreateCommand();
        stockCmd.CommandText = @"
            UPDATE BloodInventory SET UnitsAvailable = 15, UpdatedAt = datetime('now') WHERE BloodGroupId IN (1, 3, 7); -- A+, B+, O+
            UPDATE BloodInventory SET UnitsAvailable = 8,  UpdatedAt = datetime('now') WHERE BloodGroupId IN (5);       -- AB+
            UPDATE BloodInventory SET UnitsAvailable = 4,  UpdatedAt = datetime('now') WHERE BloodGroupId IN (2, 4);    -- A-, B-
            UPDATE BloodInventory SET UnitsAvailable = 2,  UpdatedAt = datetime('now') WHERE BloodGroupId IN (6, 8);    -- AB-, O- (Low/Critical)
        ";
        stockCmd.ExecuteNonQuery();

        // Sample Donors
        var sampleDonors = new[]
        {
            ("John Doe", 28, "Male", "555-0101", "johndoe@example.com", "Springfield", 1, "2024-08-15"),
            ("Jane Smith", 34, "Female", "555-0102", "janesmith@example.com", "Metropolis", 7, "2024-09-10"),
            ("Alex Johnson", 22, "Other", "555-0103", "alex.j@example.com", "Gotham", 3, "2024-07-20"),
            ("Emily Davis", 45, "Female", "555-0104", "emily.d@example.com", "Star City", 5, "2024-05-12"),
            ("Michael Brown", 31, "Male", "555-0105", "mbrown@example.com", "Central City", 8, "2024-09-01"),
            ("Sarah Wilson", 26, "Female", "555-0106", "sarah.w@example.com", "Springfield", 2, "2024-08-28")
        };

        foreach (var (name, age, gender, phone, email, city, groupId, lastDonation) in sampleDonors)
        {
            using var insertDonorCmd = connection.CreateCommand();
            insertDonorCmd.CommandText = @"
                INSERT INTO Donors (FullName, Age, Gender, Phone, Email, City, BloodGroupId, LastDonationDate, CreatedAt)
                VALUES (@name, @age, @gender, @phone, @email, @city, @groupId, @lastDonation, datetime('now'));";
            insertDonorCmd.Parameters.AddWithValue("@name", name);
            insertDonorCmd.Parameters.AddWithValue("@age", age);
            insertDonorCmd.Parameters.AddWithValue("@gender", gender);
            insertDonorCmd.Parameters.AddWithValue("@phone", phone);
            insertDonorCmd.Parameters.AddWithValue("@email", email);
            insertDonorCmd.Parameters.AddWithValue("@city", city);
            insertDonorCmd.Parameters.AddWithValue("@groupId", groupId);
            insertDonorCmd.Parameters.AddWithValue("@lastDonation", lastDonation);
            insertDonorCmd.ExecuteNonQuery();
        }

        // Sample Requests
        var sampleRequests = new[]
        {
            ("Robert Taylor", "City General Hospital", "555-0201", 1, 2, "Pending", "Emergency surgery requirement"),
            ("Maria Garcia", "Memorial Hospital", "555-0202", 7, 3, "Pending", "ICU patient transfusion"),
            ("David Miller", "St. Jude Medical Center", "555-0203", 3, 1, "Fulfilled", "Routine surgical preparation"),
            ("Alice Cooper", "County Health Clinic", "555-0204", 8, 4, "Pending", "Trauma center urgent demand")
        };

        foreach (var (pName, hosp, contact, groupId, units, status, notes) in sampleRequests)
        {
            using var reqCmd = connection.CreateCommand();
            reqCmd.CommandText = @"
                INSERT INTO BloodRequests (PatientName, HospitalName, ContactNumber, BloodGroupId, UnitsRequired, RequestDate, Status, Notes)
                VALUES (@pName, @hosp, @contact, @groupId, @units, datetime('now'), @status, @notes);";
            reqCmd.Parameters.AddWithValue("@pName", pName);
            reqCmd.Parameters.AddWithValue("@hosp", hosp);
            reqCmd.Parameters.AddWithValue("@contact", contact);
            reqCmd.Parameters.AddWithValue("@groupId", groupId);
            reqCmd.Parameters.AddWithValue("@units", units);
            reqCmd.Parameters.AddWithValue("@status", status);
            reqCmd.Parameters.AddWithValue("@notes", notes);
            reqCmd.ExecuteNonQuery();
        }
    }
}
