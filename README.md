# Blood Bank Management System (BBMS)

An academic desktop application built with **C#**, **Windows Forms (.NET 8)**, and **SQLite (`Microsoft.Data.Sqlite`)**. Designed for college mini-projects, lab demonstrations, and viva evaluations.

---

## 1. Project Overview

The **Blood Bank Management System** provides a blood bank administrator with tools to:
- Register and manage eligible blood donors (ages 18–65).
- Monitor real-time blood stock across all **eight standard ABO/Rh blood groups** (`A+`, `A-`, `B+`, `B-`, `AB+`, `AB-`, `O+`, `O-`).
- Process patient and hospital transfusion requisitions.
- Atomically check stock and deduct units using **database transactions**, preventing negative stock or duplicate fulfillments.
- Search, filter, and export records to **RFC-4180 CSV** files.
- View a central **administrative dashboard** with key operational metrics and low-stock alerts.

---

## 2. Technology Stack & Prerequisites

| Component | Specification |
| :--- | :--- |
| **Language** | C# 12 |
| **Application Type** | Windows Forms Desktop Application |
| **Target Framework** | `.NET 8` for Windows (`net8.0-windows`) |
| **Cross-Platform Compilation** | `<EnableWindowsTargeting>true</EnableWindowsTargeting>` |
| **Database** | SQLite 3 (`bloodbank.db`) |
| **Data Access Library** | `Microsoft.Data.Sqlite` (100% Parameterized Queries) |
| **Architecture** | Multi-Tier (Presentation, Services, Data Access, Models) |

---

## 3. Architecture & Project Structure

The project strictly separates user interface, business logic, and database access:

```
BloodBankManagementSystem/
├── BloodBankManagementSystem.sln             # Visual Studio Solution File
├── BloodBankManagementSystem/                 # Windows Forms Desktop Application
│   ├── BloodBankManagementSystem.csproj      # .NET 8 Windows Forms project file
│   ├── Program.cs                            # Application entry point & DB initialization
│   ├── Models/                               # Data entity models
│   │   ├── BloodGroup.cs                     # ABO/Rh blood group model
│   │   ├── Donor.cs                          # Registered donor model
│   │   ├── BloodInventory.cs                 # Blood group stock model
│   │   ├── BloodRequest.cs                   # Hospital/patient requisition model
│   │   └── DashboardStats.cs                 # Aggregated dashboard metrics model
│   ├── Data/                                 # Data Access Layer
│   │   └── DatabaseHelper.cs                 # Connection handling, schema creation, seeding & PRAGMA
│   ├── Services/                             # Business Logic Layer
│   │   ├── BloodGroupService.cs              # Blood group queries
│   │   ├── DonorService.cs                   # Donor CRUD, search, and filtering
│   │   ├── InventoryService.cs               # Inventory retrieval & demonstration stock adjustments
│   │   ├── RequestService.cs                 # Transactional fulfillment, rejection, and queries
│   │   ├── DashboardService.cs               # Summary metric calculations
│   │   └── CsvExportService.cs               # RFC-4180 CSV export generation
│   ├── Forms/                                # Presentation Layer (Windows Forms)
│   │   ├── MainForm.cs                       # Primary application window with sidebar navigation
│   │   ├── DonorDialog.cs                    # Modal dialog for Add / Edit Donor
│   │   ├── StockAdjustmentDialog.cs          # Modal dialog for Demonstration Stock Adjustments
│   │   └── RequestDialog.cs                  # Modal dialog for New Blood Requisitions
│   └── Helpers/                              # UI theme & validation helpers
│       ├── UITheme.cs                        # Burgundy color palette, fonts, DataGridView styling
│       └── ValidationHelper.cs               # Donor, request, and stock validation rules
└── BloodBankManagementSystem.Tests/          # Automated Test Suite (runs on Linux & Windows)
    ├── BloodBankManagementSystem.Tests.csproj# Console test project
    └── Program.cs                            # 11 automated verification tests
```

---

## 4. Database Schema & Relational Design

The system initializes a local SQLite database named `bloodbank.db` on first launch.

### Database Tables

```sql
-- 1. BloodGroups Table
CREATE TABLE IF NOT EXISTS BloodGroups (
    BloodGroupId INTEGER PRIMARY KEY AUTOINCREMENT,
    GroupName TEXT NOT NULL UNIQUE
);

-- 2. BloodInventory Table
CREATE TABLE IF NOT EXISTS BloodInventory (
    InventoryId INTEGER PRIMARY KEY AUTOINCREMENT,
    BloodGroupId INTEGER NOT NULL UNIQUE,
    UnitsAvailable INTEGER NOT NULL DEFAULT 0 CHECK(UnitsAvailable >= 0),
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (BloodGroupId) REFERENCES BloodGroups(BloodGroupId) ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS IX_BloodInventory_BloodGroupId ON BloodInventory(BloodGroupId);

-- 3. Donors Table
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
CREATE INDEX IF NOT EXISTS IX_Donors_FullName ON Donors(FullName);

-- 4. BloodRequests Table
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
CREATE INDEX IF NOT EXISTS IX_BloodRequests_Status ON BloodRequests(Status);
```

### Automatic Seeding
- On initialization, the 8 standard ABO/Rh groups (`A+`, `A-`, `B+`, `B-`, `AB+`, `AB-`, `O+`, `O-`) are seeded.
- Each blood group is paired with an initial inventory record of `0` units.
- A **"Load Demo Data"** button is available on the Dashboard to load sample records (without real personal info) for presentations.

---

## 5. Key Features & Business Rules

### 1. Dashboard
- **Real-time KPI Cards**: Total Registered Donors, Total Available Units, Pending Requests, Fulfilled Requests.
- **Stock Alert Indicator**: Visual warning when blood groups drop below safe thresholds (Critical: $\le 2$ units, Low: $\le 5$ units).
- **Inventory Overview Table**: Shows available units and status for all 8 blood groups at a glance.

### 2. Donor Management
- **Add / Edit / Delete**: Modal dialog with full validation.
- **Validation Rules**:
  - Full Name and Phone Number are mandatory.
  - Age must be between **18 and 65 years**.
  - Blood group selection is mandatory.
- **Search & Filters**: Search donors in real-time by Name, Phone, or City; filter by Blood Group.
- **Safe Deletion**: Requires administrator confirmation before deleting any donor.

### 3. Blood Inventory (Demonstration Module)
- Clearly labeled as an **Academic Demonstration Inventory Adjustment** module.
- Allows administrators to simulate blood donation drives, laboratory testing, or hospital stock receipts.
- **Non-Negative Guard**: Prevents adjustments that would reduce inventory below 0 units.
- Quick `+1` and `+5` unit shortcut buttons for rapid testing during viva evaluations.

### 4. Transactional Requisition Fulfillment
- Hospital requests can be fulfilled only if they are currently **Pending**.
- Fulfillment executes inside an **atomic SQLite transaction**:
  1. Verifies request status is `Pending`.
  2. Queries available stock for the requested blood group.
  3. Checks if `UnitsAvailable >= UnitsRequired`.
  4. If sufficient, deducts units from `BloodInventory` and marks request status as `Fulfilled`.
  5. Commits transaction.
  6. If stock is insufficient or any query fails, the transaction **rolls back completely**, preventing partial updates or negative stock.

### 5. Reports & CSV Export
- Generates RFC-4180 compliant CSV exports for:
  - **Donor Registry** (`Donors_Export_*.csv`)
  - **Blood Inventory Snapshot** (`BloodInventory_Export_*.csv`)
  - **Requisition History** (`BloodRequests_Export_*.csv`)

---

## 6. How to Build & Run

### A. On Windows (Desktop GUI Execution)

1. **Option 1: Visual Studio 2022**
   - Double-click `BloodBankManagementSystem.sln` to open the solution in Visual Studio 2022 (v17.8 or newer with `.NET desktop development` workload).
   - Press <kbd>F5</kbd> or click the green **Start** button to build and run the GUI application.

2. **Option 2: .NET CLI (Command Prompt / PowerShell)**
   ```bash
   # Clone the repository (if not already local)
   git clone https://github.com/<your-username>/BloodBankManagementSystem.git
   cd BloodBankManagementSystem

   # Build the solution
   dotnet build

   # Run the Windows Forms application
   dotnet run --project BloodBankManagementSystem/BloodBankManagementSystem.csproj
   ```

---

### B. On Linux / GitHub Codespaces (Headless Environment)

> [!NOTE]
> **Environment Note**: Windows Forms applications rely on the Windows Graphics Device Interface (GDI+) and the Windows Desktop runtime (`Microsoft.WindowsDesktop.App`), which are native to the Windows operating system. Therefore, GUI execution cannot run directly inside a headless Linux container (such as GitHub Codespaces).
> 
> However, thanks to `<EnableWindowsTargeting>true</EnableWindowsTargeting>`, the code **compiles cleanly** on Linux with the .NET 8 SDK (`dotnet build`), and the included **automated verification suite** can be executed directly on Linux to test all database operations and transactions.

#### 1. Compile the Project on Linux
```bash
dotnet build
```
*Expected result: `Build succeeded. 0 Error(s)`*

#### 2. Run the Automated Verification Suite on Linux
```bash
dotnet exec --roll-forward Major BloodBankManagementSystem.Tests/bin/Debug/net8.0/BloodBankManagementSystem.Tests.dll
```

#### Test Suite Output:
```text
===================================================================
  Blood Bank Management System - Automated Verification Suite
===================================================================
Running 1. Database Schema & 8 Blood Groups Seeding ... [PASS]
Running 2. Foreign-Key Pragma & Non-Negative Inventory Constraint ... [PASS]
Running 3. Donor Validation Rules (Age >= 18, Required Fields) ... [PASS]
Running 4. Donor CRUD Operations & Search/Filter ... [PASS]
Running 5. Blood Inventory Stock Adjustment & Negative Prevention ... [PASS]
Running 6. Atomic Transaction Fulfillment with Inventory Deduction ... [PASS]
Running 7. Transaction Rollback on Insufficient Stock ... [PASS]
Running 8. Duplicate Fulfillment Prevention ... [PASS]
Running 9. Blood Requisition Rejection Workflow ... [PASS]
Running 10. Dashboard Aggregated Statistics Calculation ... [PASS]
Running 11. Standard RFC-4180 CSV Data Export ... [PASS]

===================================================================
  ALL TESTS PASSED! (11/11 assertions succeeded)
```

---

## 7. Step-by-Step Test Scenarios for Evaluation

| Step | Action | Expected Result |
| :---: | :--- | :--- |
| **1** | Launch application on Windows. | The dashboard loads with 8 blood groups initialized at 0 units. |
| **2** | Click **"🧪 Load Demo Data"** on Dashboard. | Loads starter records. Dashboard KPI cards update immediately. |
| **3** | Go to **Donor Management** $\rightarrow$ Click **"➕ Add Donor"**. | Modal opens. Enter Age = 16 $\rightarrow$ click Save $\rightarrow$ error displays: *Donor must be at least 18 years old*. |
| **4** | Enter valid donor (Age = 25, Name = "Alice Green", Blood Group = "O+") $\rightarrow$ Save. | Donor is registered and appears in table. Dashboard donor count increments. |
| **5** | Go to **Blood Inventory** $\rightarrow$ Select `O+` $\rightarrow$ Click **"+5 Quick Units"**. | Available stock increments from 0 to 5. Status updates from "Critical" to "Low Stock". |
| **6** | Go to **Blood Requests** $\rightarrow$ Click **"➕ New Request"**. | Create request for 8 units of `O+`. Status is set to **Pending**. |
| **7** | Select the pending request $\rightarrow$ Click **"✅ Fulfill Request"**. | Prompt warns: *Insufficient inventory (Available: 5, Required: 8)*. Transaction rolls back. Stock remains 5. Request remains Pending. |
| **8** | Return to **Blood Inventory** $\rightarrow$ Add 10 units to `O+` (Total = 15). | Stock is replenished to 15 units. |
| **9** | Return to **Blood Requests** $\rightarrow$ Click **"✅ Fulfill Request"**. | Transaction succeeds! 8 units deducted ($15 - 8 = 7$). Request status updates to **Fulfilled**. |
| **10** | Attempt to click **"✅ Fulfill Request"** on the same request again. | Button is disabled (or displays notice that only Pending requests can be fulfilled). |
| **11** | Go to **Reports & CSV** $\rightarrow$ Click **"Export Donors CSV"**. | File save dialog opens. Exported CSV matches RFC-4180 formatting with headers. |

---

## 8. College Viva & Technical Questions FAQ

**Q1: Why did you use SQLite and `Microsoft.Data.Sqlite`?**
> *Answer:* SQLite is a lightweight, serverless, self-contained relational database that requires no server installation or background services. It is ideal for standalone desktop applications. `Microsoft.Data.Sqlite` is the official modern ADO.NET provider maintained by Microsoft for .NET 8.

**Q2: How do you prevent SQL Injection?**
> *Answer:* Every SQL command uses parameterized queries (`cmd.Parameters.AddWithValue(...)` or `SqliteParameter`). Dynamic SQL string concatenation is avoided.

**Q3: How are foreign keys enforced in SQLite?**
> *Answer:* In SQLite, foreign-key enforcement is disabled by default for backward compatibility. In our application, `DatabaseHelper.GetOpenConnection()` executes `PRAGMA foreign_keys = ON;` on every opened connection.

**Q4: How does the application prevent negative inventory during request fulfillment?**
> *Answer:* Fulfillment uses an atomic database transaction (`connection.BeginTransaction()`). It queries the current available units, verifies that `UnitsAvailable >= UnitsRequired`, updates the inventory table, and sets the request status to 'Fulfilled'. If stock is insufficient or an error occurs, `tx.Rollback()` is called, ensuring database consistency.

**Q5: What architecture was followed?**
> *Answer:* A Multi-Tier (N-Tier) architecture:
> - **Models**: Pure entity classes (`Donor`, `BloodRequest`, etc.).
> - **Data Access Layer**: `DatabaseHelper` managing connections and SQL schema.
> - **Service Layer**: Business rules, validations, queries, and transactions.
> - **Presentation Layer**: Windows Forms (`MainForm`, `DonorDialog`, etc.) and `UITheme`.

---

## 9. License & Academic Disclaimer

This project is developed as an educational mini-project for academic demonstration purposes. All sample names, contact numbers, and clinical details in demo scripts are entirely fictitious.