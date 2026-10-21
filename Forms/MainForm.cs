using System.Drawing;
using System.Windows.Forms;
using BloodBankManagementSystem.Data;
using BloodBankManagementSystem.Helpers;
using BloodBankManagementSystem.Models;
using BloodBankManagementSystem.Services;

namespace BloodBankManagementSystem.Forms;

/// <summary>
/// Primary application window for the Blood Bank Management System.
/// Features a modern responsive layout with sidebar navigation, metric dashboard,
/// donor management, inventory tracking, transactional requisition fulfillment, and CSV reporting.
/// </summary>
public partial class MainForm : Form
{
    // Services
    private readonly BloodGroupService _bloodGroupService = new();
    private readonly DonorService _donorService = new();
    private readonly InventoryService _inventoryService = new();
    private readonly RequestService _requestService = new();
    private readonly DashboardService _dashboardService = new();

    // Navigation and Layout Controls
    private Panel _pnlSidebar = null!;
    private Panel _pnlContent = null!;
    private readonly List<Button> _navButtons = new();

    // Views (Panels switched via sidebar)
    private Panel _viewDashboard = null!;
    private Panel _viewDonors = null!;
    private Panel _viewInventory = null!;
    private Panel _viewRequests = null!;
    private Panel _viewReports = null!;
    private Panel _viewAbout = null!;

    // Dashboard Controls
    private Label _lblMetricDonors = null!;
    private Label _lblMetricUnits = null!;
    private Label _lblMetricPending = null!;
    private Label _lblMetricFulfilled = null!;
    private DataGridView _dgvDashboardInventory = null!;
    private Label _lblDashboardAlert = null!;

    // Donor Controls
    private DataGridView _dgvDonors = null!;
    private TextBox _txtDonorSearch = null!;
    private ComboBox _cboDonorBloodGroupFilter = null!;
    private Button _btnEditDonor = null!;
    private Button _btnDeleteDonor = null!;

    // Inventory Controls
    private DataGridView _dgvInventory = null!;

    // Request Controls
    private DataGridView _dgvRequests = null!;
    private ComboBox _cboRequestStatusFilter = null!;
    private ComboBox _cboRequestBloodGroupFilter = null!;
    private Button _btnFulfillRequest = null!;
    private Button _btnRejectRequest = null!;
    private Button _btnDeleteRequest = null!;

    public MainForm()
    {
        InitializeAppLayout();
        LoadInitialData();
        SwitchView(_viewDashboard, _navButtons[0]);
    }

    private void InitializeAppLayout()
    {
        Text = "Blood Bank Management System";
        Width = 1150;
        Height = 720;
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UITheme.BackgroundNeutral;
        Font = UITheme.BodyRegularFont;

        // Top App Bar
        var pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = UITheme.DarkBurgundy,
            Padding = new Padding(20, 0, 20, 0)
        };

        var lblAppTitle = new Label
        {
            Text = "🩸 BLOOD BANK MANAGEMENT SYSTEM",
            Font = UITheme.HeaderMediumFont,
            ForeColor = Color.White,
            AutoSize = false,
            Width = 450,
            Height = 56,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Left
        };

        var lblAppSub = new Label
        {
            Text = ".NET 8 • SQLite • Academic Prototype",
            Font = UITheme.SmallMutedFont,
            ForeColor = Color.FromArgb(254, 226, 226),
            AutoSize = false,
            Width = 280,
            Height = 56,
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Right
        };

        pnlTopBar.Controls.Add(lblAppSub);
        pnlTopBar.Controls.Add(lblAppTitle);

        // Sidebar Navigation
        _pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = UITheme.SidebarDark,
            Padding = new Padding(0, 15, 0, 15)
        };

        // Brand Banner in Sidebar
        var pnlBrand = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UITheme.SidebarDark, Padding = new Padding(16, 0, 0, 0) };
        var lblBrand = new Label
        {
            Text = "ADMIN CONSOLE",
            Font = UITheme.SectionTitleFont,
            ForeColor = UITheme.TextMuted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlBrand.Controls.Add(lblBrand);

        // Navigation Buttons Container
        var pnlNav = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        int btnY = 10;
        AddNavButton(pnlNav, "📊  Dashboard", (s, e) => { RefreshDashboard(); SwitchView(_viewDashboard, (Button)s!); }, ref btnY);
        AddNavButton(pnlNav, "👥  Donor Management", (s, e) => { RefreshDonors(); SwitchView(_viewDonors, (Button)s!); }, ref btnY);
        AddNavButton(pnlNav, "🩸  Blood Inventory", (s, e) => { RefreshInventory(); SwitchView(_viewInventory, (Button)s!); }, ref btnY);
        AddNavButton(pnlNav, "📋  Blood Requests", (s, e) => { RefreshRequests(); SwitchView(_viewRequests, (Button)s!); }, ref btnY);
        AddNavButton(pnlNav, "📑  Reports & CSV", (s, e) => { SwitchView(_viewReports, (Button)s!); }, ref btnY);
        AddNavButton(pnlNav, "ℹ️  About System", (s, e) => { SwitchView(_viewAbout, (Button)s!); }, ref btnY);

        _pnlSidebar.Controls.Add(pnlNav);
        _pnlSidebar.Controls.Add(pnlBrand);

        // Main Content Panel
        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.BackgroundNeutral,
            Padding = new Padding(20)
        };
        // WinForms docks controls in z-order. Add the fill host first so later
        // top/left docked chrome reserves its space instead of covering it.
        Controls.Add(_pnlContent);
        Controls.Add(_pnlSidebar);
        Controls.Add(pnlTopBar);

        // Build All Views
        BuildDashboardView();
        BuildDonorsView();
        BuildInventoryView();
        BuildRequestsView();
        BuildReportsView();
        BuildAboutView();
    }

    private void AddNavButton(Panel parent, string text, EventHandler onClick, ref int y)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(0, y),
            Width = 220,
            Height = 44,
            FlatStyle = FlatStyle.Flat,
            BackColor = UITheme.SidebarDark,
            ForeColor = Color.FromArgb(203, 213, 225),
            Font = UITheme.BodyBoldFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += onClick;

        btn.MouseEnter += (s, e) =>
        {
            if (btn.BackColor != UITheme.SidebarActive)
                btn.BackColor = UITheme.SidebarItemHover;
        };
        btn.MouseLeave += (s, e) =>
        {
            if (btn.BackColor != UITheme.SidebarActive)
                btn.BackColor = UITheme.SidebarDark;
        };

        parent.Controls.Add(btn);
        _navButtons.Add(btn);
        y += 48;
    }

    private void SwitchView(Panel targetView, Button activeNavButton)
    {
        _pnlContent.SuspendLayout();
        _pnlContent.Controls.Clear();
        targetView.Dock = DockStyle.Fill;
        _pnlContent.Controls.Add(targetView);
        _pnlContent.ResumeLayout();

        foreach (var b in _navButtons)
        {
            b.BackColor = UITheme.SidebarDark;
            b.ForeColor = Color.FromArgb(203, 213, 225);
        }
        activeNavButton.BackColor = UITheme.SidebarActive;
        activeNavButton.ForeColor = Color.White;
    }

    // ==========================================
    // 1. DASHBOARD VIEW
    // ==========================================
    private void BuildDashboardView()
    {
        _viewDashboard = new Panel { BackColor = UITheme.BackgroundNeutral };

        // Title row
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 45 };
        var lblTitle = new Label { Text = "Operational Dashboard", Font = UITheme.HeaderLargeFont, ForeColor = UITheme.TextPrimary, AutoSize = true, Location = new Point(0, 5) };
        var btnRefresh = new Button { Text = "🔄 Refresh", Location = new Point(780, 5), Width = 110, Height = 32 };
        UITheme.StyleSecondaryButton(btnRefresh);
        btnRefresh.Click += (s, e) => RefreshDashboard();

        var btnSeed = new Button { Text = "🧪 Load Demo Data", Location = new Point(640, 5), Width = 135, Height = 32 };
        UITheme.StyleSecondaryButton(btnSeed);
        btnSeed.Click += OnLoadDemoDataClicked;

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(btnRefresh);
        pnlHeader.Controls.Add(btnSeed);
        // KPI Cards Row
        var pnlCards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 100,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(0, 10, 0, 10)
        };
        pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        pnlCards.Controls.Add(UITheme.CreateMetricCard("Registered Donors", "0", UITheme.PrimaryBurgundy, out _lblMetricDonors), 0, 0);
        pnlCards.Controls.Add(UITheme.CreateMetricCard("Total Units in Stock", "0", UITheme.SuccessGreen, out _lblMetricUnits), 1, 0);
        pnlCards.Controls.Add(UITheme.CreateMetricCard("Pending Requests", "0", UITheme.WarningAmber, out _lblMetricPending), 2, 0);
        pnlCards.Controls.Add(UITheme.CreateMetricCard("Fulfilled Requests", "0", UITheme.InfoBlue, out _lblMetricFulfilled), 3, 0);


        // Alert Bar
        _lblDashboardAlert = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = UITheme.SoftRedBadge,
            ForeColor = UITheme.PrimaryBurgundy,
            Font = UITheme.BodyBoldFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Text = "Status: All blood groups monitored. Ready for operations."
        };

        // Bottom Container (Inventory Summary Table + Quick Actions)
        var pnlBottom = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 15, 0, 0) };

        var grpInventory = new GroupBox
        {
            Text = "Blood Group Inventory Status (All 8 Groups)",
            Font = UITheme.SectionTitleFont,
            ForeColor = UITheme.TextPrimary,
            Dock = DockStyle.Left,
            Width = 580,
            Padding = new Padding(10)
        };

        _dgvDashboardInventory = new DataGridView { Dock = DockStyle.Fill };
        UITheme.StyleDataGridView(_dgvDashboardInventory);
        _dgvDashboardInventory.Columns.Add("BloodGroup", "Blood Group");
        _dgvDashboardInventory.Columns.Add("UnitsAvailable", "Units Available");
        _dgvDashboardInventory.Columns.Add("StockStatus", "Stock Level");
        _dgvDashboardInventory.Columns.Add("UpdatedAt", "Last Updated");
        _dgvDashboardInventory.CellFormatting += DgvInventory_CellFormatting;
        grpInventory.Controls.Add(_dgvDashboardInventory);

        // Quick Actions Hub
        var grpQuick = new GroupBox
        {
            Text = "Quick Action Hub",
            Font = UITheme.SectionTitleFont,
            ForeColor = UITheme.TextPrimary,
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var pnlActionButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true
        };

        var btnQuickDonor = new Button { Text = "➕ Register New Donor", Width = 280, Height = 42, Margin = new Padding(0, 0, 0, 12) };
        UITheme.StylePrimaryButton(btnQuickDonor);
        btnQuickDonor.Click += (s, e) => OnAddDonorClicked(s, e);

        var btnQuickReq = new Button { Text = "📋 New Transfusion Request", Width = 280, Height = 42, Margin = new Padding(0, 0, 0, 12) };
        UITheme.StylePrimaryButton(btnQuickReq);
        btnQuickReq.Click += (s, e) => OnAddRequestClicked(s, e);

        var btnQuickAdjust = new Button { Text = "🩸 Adjust Stock (Demo)", Width = 280, Height = 42, Margin = new Padding(0, 0, 0, 12) };
        UITheme.StyleWarningButton(btnQuickAdjust);
        btnQuickAdjust.Click += (s, e) => OnAdjustStockClicked(s, e);

        var btnQuickExport = new Button { Text = "💾 Export CSV Reports", Width = 280, Height = 42, Margin = new Padding(0, 0, 0, 12) };
        UITheme.StyleSecondaryButton(btnQuickExport);
        btnQuickExport.Click += (s, e) => SwitchView(_viewReports, _navButtons[4]);

        pnlActionButtons.Controls.Add(btnQuickDonor);
        pnlActionButtons.Controls.Add(btnQuickReq);
        pnlActionButtons.Controls.Add(btnQuickAdjust);
        pnlActionButtons.Controls.Add(btnQuickExport);

        grpQuick.Controls.Add(pnlActionButtons);
        pnlBottom.Controls.Add(grpQuick);
        pnlBottom.Controls.Add(grpInventory);

        // Fill content first; then reserve each fixed-height top section.
        _viewDashboard.Controls.Add(pnlBottom);
        _viewDashboard.Controls.Add(_lblDashboardAlert);
        _viewDashboard.Controls.Add(pnlCards);
        _viewDashboard.Controls.Add(pnlHeader);

    }

    private void RefreshDashboard()
    {
        try
        {
            var stats = _dashboardService.GetDashboardStats();

            _lblMetricDonors.Text = stats.TotalDonors.ToString();
            _lblMetricUnits.Text = stats.TotalAvailableUnits.ToString();
            _lblMetricPending.Text = stats.PendingRequests.ToString();
            _lblMetricFulfilled.Text = stats.FulfilledRequests.ToString();

            // Populate summary table
            _dgvDashboardInventory.Rows.Clear();
            int criticalCount = 0;
            int lowCount = 0;

            foreach (var item in stats.InventorySummary)
            {
                _dgvDashboardInventory.Rows.Add(
                    item.BloodGroupName,
                    $"{item.UnitsAvailable} units",
                    item.StockStatus,
                    item.UpdatedAt.ToString("yyyy-MM-dd HH:mm")
                );

                if (item.StockStatus == "Critical") criticalCount++;
                else if (item.StockStatus == "Low Stock") lowCount++;
            }

            if (criticalCount > 0)
            {
                _lblDashboardAlert.Text = $"⚠️ Alert: {criticalCount} blood group(s) have CRITICAL stock levels (<= 2 units). Please initiate donor outreach!";
                _lblDashboardAlert.ForeColor = UITheme.DangerRed;
                _lblDashboardAlert.BackColor = Color.FromArgb(254, 226, 226);
            }
            else if (lowCount > 0)
            {
                _lblDashboardAlert.Text = $"ℹ️ Notice: {lowCount} blood group(s) are running low on stock. Sufficient for normal demands.";
                _lblDashboardAlert.ForeColor = UITheme.WarningAmber;
                _lblDashboardAlert.BackColor = Color.FromArgb(254, 243, 199);
            }
            else
            {
                _lblDashboardAlert.Text = "✅ All blood group inventory levels are adequate and healthy.";
                _lblDashboardAlert.ForeColor = UITheme.SuccessGreen;
                _lblDashboardAlert.BackColor = Color.FromArgb(240, 253, 244);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load dashboard metrics:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnLoadDemoDataClicked(object? sender, EventArgs e)
    {
        var confirm = MessageBox.Show(
            "Load realistic demonstration donors and transfusion requests?\n\nThis will provide starter data for your academic presentation if no records exist.",
            "Load Sample Demo Data",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm == DialogResult.Yes)
        {
            try
            {
                DatabaseHelper.SeedSampleDemoDataIfEmpty();
                RefreshDashboard();
                MessageBox.Show("Demonstration data loaded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading demo data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ==========================================
    // 2. DONOR MANAGEMENT VIEW
    // ==========================================
    private void BuildDonorsView()
    {
        _viewDonors = new Panel { BackColor = UITheme.BackgroundNeutral };

        // Toolbar
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 104 };

        // Filter / Search Toolbar Row
        var pnlToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 104,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 4, 0, 4)
        };

        var lblSearch = new Label { Text = "Search:", AutoSize = true, Margin = new Padding(0, 10, 4, 4), Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary };
        _txtDonorSearch = new TextBox { Width = 170, Margin = new Padding(0, 5, 10, 4), Font = UITheme.BodyRegularFont };
        _txtDonorSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { RefreshDonors(); e.SuppressKeyPress = true; } };

        var lblFilter = new Label { Text = "Blood Group:", AutoSize = true, Margin = new Padding(0, 10, 4, 4), Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary };
        _cboDonorBloodGroupFilter = new ComboBox { Width = 110, Margin = new Padding(0, 5, 8, 4), DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.BodyRegularFont };
        _cboDonorBloodGroupFilter.SelectedIndexChanged += (s, e) => RefreshDonors();

        var btnSearch = new Button { Text = "🔍 Filter", Width = 80, Height = 34, Margin = new Padding(2, 4, 4, 4) };
        UITheme.StyleSecondaryButton(btnSearch);
        btnSearch.Text = "Filter";
        btnSearch.Width = 82;
        btnSearch.Height = 42;
        btnSearch.Padding = new Padding(6, 2, 6, 2);
        btnSearch.Click += (s, e) => RefreshDonors();

        var btnClear = new Button { Text = "✕ Clear", Width = 75, Height = 34, Margin = new Padding(2, 4, 4, 4) };
        UITheme.StyleSecondaryButton(btnClear);
        btnClear.Text = "Clear";
        btnClear.Width = 76;
        btnClear.Height = 42;
        btnClear.Padding = new Padding(6, 2, 6, 2);
        btnClear.Click += (s, e) =>
        {
            _txtDonorSearch.Text = string.Empty;
            _cboDonorBloodGroupFilter.SelectedIndex = 0;
            RefreshDonors();
        };

        var btnAddDonor = new Button { Text = "➕ Add Donor", Width = 115, Height = 34, Margin = new Padding(2, 4, 4, 4) };
        UITheme.StylePrimaryButton(btnAddDonor);
        btnAddDonor.Text = "Add Donor";
        btnAddDonor.Width = 112;
        btnAddDonor.Height = 42;
        btnAddDonor.Padding = new Padding(6, 2, 6, 2);
        btnAddDonor.Click += OnAddDonorClicked;

        _btnEditDonor = new Button { Text = "✏️ Edit", Width = 85, Height = 34, Margin = new Padding(2, 4, 4, 4), Enabled = false };
        UITheme.StyleSecondaryButton(_btnEditDonor);
        _btnEditDonor.Text = "Edit";
        _btnEditDonor.Width = 76;
        _btnEditDonor.Height = 42;
        _btnEditDonor.Padding = new Padding(6, 2, 6, 2);
        _btnEditDonor.Click += OnEditDonorClicked;

        _btnDeleteDonor = new Button { Text = "🗑️ Delete", Width = 90, Height = 34, Margin = new Padding(2, 4, 4, 4), Enabled = false };
        UITheme.StyleDangerButton(_btnDeleteDonor);
        _btnDeleteDonor.Text = "Delete";
        _btnDeleteDonor.Width = 86;
        _btnDeleteDonor.Height = 42;
        _btnDeleteDonor.Padding = new Padding(6, 2, 6, 2);
        _btnDeleteDonor.Click += OnDeleteDonorClicked;

        var btnExportDonors = new Button { Text = "💾 CSV", Width = 80, Height = 34, Margin = new Padding(2, 4, 4, 4) };
        UITheme.StyleSecondaryButton(btnExportDonors);
        btnExportDonors.Text = "Export CSV";
        btnExportDonors.Width = 100;
        btnExportDonors.Height = 42;
        btnExportDonors.Padding = new Padding(6, 2, 6, 2);
        btnExportDonors.Click += OnExportDonorsClicked;

        pnlToolbar.Controls.Add(lblSearch);
        pnlToolbar.Controls.Add(_txtDonorSearch);
        pnlToolbar.Controls.Add(lblFilter);
        pnlToolbar.Controls.Add(_cboDonorBloodGroupFilter);
        pnlToolbar.Controls.Add(btnSearch);
        pnlToolbar.Controls.Add(btnClear);
        pnlToolbar.Controls.Add(btnAddDonor);
        pnlToolbar.Controls.Add(_btnEditDonor);
        pnlToolbar.Controls.Add(_btnDeleteDonor);
        pnlToolbar.Controls.Add(btnExportDonors);

        pnlHeader.Controls.Add(pnlToolbar);

        // DataGridView
        _dgvDonors = new DataGridView { Dock = DockStyle.Fill };
        UITheme.StyleDataGridView(_dgvDonors);

        _dgvDonors.Columns.Add("DonorId", "ID");
        _dgvDonors.Columns["DonorId"]!.Width = 50;
        _dgvDonors.Columns.Add("FullName", "Full Name");
        _dgvDonors.Columns.Add("BloodGroup", "Blood Group");
        _dgvDonors.Columns["BloodGroup"]!.Width = 100;
        _dgvDonors.Columns.Add("Age", "Age");
        _dgvDonors.Columns["Age"]!.Width = 60;
        _dgvDonors.Columns.Add("Gender", "Gender");
        _dgvDonors.Columns["Gender"]!.Width = 80;
        _dgvDonors.Columns.Add("Phone", "Phone Number");
        _dgvDonors.Columns.Add("Email", "Email");
        _dgvDonors.Columns.Add("City", "City");
        _dgvDonors.Columns.Add("LastDonation", "Last Donated");
        _dgvDonors.Columns.Add("CreatedAt", "Registered On");

        _dgvDonors.SelectionChanged += (s, e) =>
        {
            bool hasSelection = _dgvDonors.SelectedRows.Count > 0;
            _btnEditDonor.Enabled = hasSelection;
            _btnDeleteDonor.Enabled = hasSelection;
        };

        _dgvDonors.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0) OnEditDonorClicked(s, e);
        };

        _viewDonors.Controls.Add(_dgvDonors);
        _viewDonors.Controls.Add(pnlHeader);

    }

    private void RefreshDonors()
    {
        try
        {
            string? search = string.IsNullOrWhiteSpace(_txtDonorSearch.Text) ? null : _txtDonorSearch.Text.Trim();
            int? bgId = null;

            if (_cboDonorBloodGroupFilter.SelectedItem is BloodGroup selectedGroup && selectedGroup.BloodGroupId > 0)
            {
                bgId = selectedGroup.BloodGroupId;
            }

            var donors = _donorService.GetAllDonors(search, bgId);

            _dgvDonors.Rows.Clear();
            foreach (var d in donors)
            {
                int rowIndex = _dgvDonors.Rows.Add(
                    d.DonorId,
                    d.FullName,
                    d.BloodGroupName,
                    d.Age,
                    d.Gender,
                    d.Phone,
                    d.Email ?? "-",
                    d.City ?? "-",
                    d.FormattedLastDonation,
                    d.CreatedAt.ToString("yyyy-MM-dd")
                );
                _dgvDonors.Rows[rowIndex].Tag = d;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to fetch donors list:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnAddDonorClicked(object? sender, EventArgs e)
    {
        using var dialog = new DonorDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                _donorService.AddDonor(dialog.ResultDonor);
                MessageBox.Show($"Donor '{dialog.ResultDonor.FullName}' successfully registered!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshDonors();
                RefreshDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add donor: {ex.Message}", "Validation / Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnEditDonorClicked(object? sender, EventArgs e)
    {
        if (_dgvDonors.SelectedRows.Count == 0) return;

        var donor = _dgvDonors.SelectedRows[0].Tag as Donor;
        if (donor == null) return;

        using var dialog = new DonorDialog(donor);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                _donorService.UpdateDonor(dialog.ResultDonor);
                MessageBox.Show($"Donor '{dialog.ResultDonor.FullName}' updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshDonors();
                RefreshDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update donor: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnDeleteDonorClicked(object? sender, EventArgs e)
    {
        if (_dgvDonors.SelectedRows.Count == 0) return;

        var donor = _dgvDonors.SelectedRows[0].Tag as Donor;
        if (donor == null) return;

        var confirm = MessageBox.Show(
            $"Are you sure you want to delete donor '{donor.FullName}' (ID #{donor.DonorId})?\n\nThis action cannot be undone.",
            "Confirm Delete Donor",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            try
            {
                _donorService.DeleteDonor(donor.DonorId);
                MessageBox.Show("Donor deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshDonors();
                RefreshDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete donor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnExportDonorsClicked(object? sender, EventArgs e)
    {
        try
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"Donors_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Donors List to CSV"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                var donors = _donorService.GetAllDonors();
                CsvExportService.ExportDonorsToCsv(sfd.FileName, donors);
                MessageBox.Show($"Exported {donors.Count} donor record(s) to:\n{sfd.FileName}", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ==========================================
    // 3. BLOOD INVENTORY VIEW
    // ==========================================
    private void BuildInventoryView()
    {
        _viewInventory = new Panel { BackColor = UITheme.BackgroundNeutral };

        // Header Panel
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 160 };

        var lblTitle = new Label { Text = "Blood Inventory Management", Font = UITheme.HeaderLargeFont, ForeColor = UITheme.TextPrimary, AutoSize = true, Location = new Point(0, 0) };
        pnlHeader.Controls.Add(lblTitle);

        var lblBanner = new Label
        {
            Text = "Academic Demonstration Prototype: Stock can be adjusted manually via the adjustment dialog below. Negative inventory is strictly prevented.",
            Font = UITheme.SmallMutedFont,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(0, 32),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblBanner);

        var pnlToolbar = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoScroll = true, Height = 90, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 3, 0, 3) };

        var btnAdjust = new Button { Text = "🩸 Adjust Stock (Demonstration)...", Width = 250, Height = 34, Margin = new Padding(0, 0, 8, 2) };
        UITheme.StylePrimaryButton(btnAdjust);
        btnAdjust.Text = "Adjust Stock...";
        btnAdjust.Width = 170;
        btnAdjust.Height = 42;
        btnAdjust.Padding = new Padding(6, 2, 6, 2);
        btnAdjust.Click += OnAdjustStockClicked;

        var btnQuickAdd1 = new Button { Text = "+1 Quick Unit", Width = 110, Height = 34, Margin = new Padding(0, 0, 8, 2) };
        UITheme.StyleSuccessButton(btnQuickAdd1);
        btnQuickAdd1.Text = "+1 Unit";
        btnQuickAdd1.Width = 90;
        btnQuickAdd1.Height = 42;
        btnQuickAdd1.Padding = new Padding(6, 2, 6, 2);
        btnQuickAdd1.Click += (s, e) => OnQuickStockAdd(1);

        var btnQuickAdd5 = new Button { Text = "+5 Quick Units", Width = 120, Height = 34, Margin = new Padding(0, 0, 8, 2) };
        UITheme.StyleSuccessButton(btnQuickAdd5);
        btnQuickAdd5.Text = "+5 Units";
        btnQuickAdd5.Width = 90;
        btnQuickAdd5.Height = 42;
        btnQuickAdd5.Padding = new Padding(6, 2, 6, 2);
        btnQuickAdd5.Click += (s, e) => OnQuickStockAdd(5);

        var btnExportInventory = new Button { Text = "💾 Export CSV", Width = 115, Height = 34, Margin = new Padding(0, 0, 8, 2) };
        UITheme.StyleSecondaryButton(btnExportInventory);
        btnExportInventory.Text = "Export CSV";
        btnExportInventory.Width = 100;
        btnExportInventory.Height = 42;
        btnExportInventory.Padding = new Padding(6, 2, 6, 2);
        btnExportInventory.Click += OnExportInventoryClicked;

        var btnRefresh = new Button { Text = "🔄 Refresh", Width = 95, Height = 34, Margin = new Padding(0, 0, 8, 2) };
        UITheme.StyleSecondaryButton(btnRefresh);
        btnRefresh.Text = "Refresh";
        btnRefresh.Width = 82;
        btnRefresh.Height = 42;
        btnRefresh.Padding = new Padding(6, 2, 6, 2);
        btnRefresh.Click += (s, e) => RefreshInventory();

        pnlToolbar.Controls.Add(btnAdjust);
        pnlToolbar.Controls.Add(btnQuickAdd1);
        pnlToolbar.Controls.Add(btnQuickAdd5);
        pnlToolbar.Controls.Add(btnExportInventory);
        pnlToolbar.Controls.Add(btnRefresh);

        pnlHeader.Controls.Add(pnlToolbar);

        // DataGridView
        _dgvInventory = new DataGridView { Dock = DockStyle.Fill };
        UITheme.StyleDataGridView(_dgvInventory);

        _dgvInventory.Columns.Add("BloodGroupId", "Group ID");
        _dgvInventory.Columns["BloodGroupId"]!.Width = 80;
        _dgvInventory.Columns.Add("BloodGroupName", "Blood Group");
        _dgvInventory.Columns.Add("UnitsAvailable", "Units Available in Stock");
        _dgvInventory.Columns.Add("StockStatus", "Stock Status Level");
        _dgvInventory.Columns.Add("UpdatedAt", "Last Inventory Update");
        _dgvInventory.CellFormatting += DgvInventory_CellFormatting;

        _viewInventory.Controls.Add(_dgvInventory);
        _viewInventory.Controls.Add(pnlHeader);
    }

    private void RefreshInventory()
    {
        try
        {
            var inventory = _inventoryService.GetAllInventory();
            _dgvInventory.Rows.Clear();

            foreach (var item in inventory)
            {
                int rowIndex = _dgvInventory.Rows.Add(
                    item.BloodGroupId,
                    item.BloodGroupName,
                    $"{item.UnitsAvailable} units",
                    item.StockStatus,
                    item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                );
                _dgvInventory.Rows[rowIndex].Tag = item;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load inventory:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnAdjustStockClicked(object? sender, EventArgs e)
    {
        int? preselectedGroupId = null;
        if (_dgvInventory.SelectedRows.Count > 0 && _dgvInventory.SelectedRows[0].Tag is BloodInventory item)
        {
            preselectedGroupId = item.BloodGroupId;
        }

        using var dialog = new StockAdjustmentDialog(preselectedGroupId);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                _inventoryService.AdjustStock(dialog.SelectedBloodGroupId, dialog.UnitsAdjustment, dialog.Reason);
                MessageBox.Show("Stock adjustment applied successfully!", "Inventory Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshInventory();
                RefreshDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Stock adjustment failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnQuickStockAdd(int unitsToAdd)
    {
        if (_dgvInventory.SelectedRows.Count == 0 || _dgvInventory.SelectedRows[0].Tag is not BloodInventory item)
        {
            MessageBox.Show("Please select a blood group row first to apply quick stock addition.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            _inventoryService.AdjustStock(item.BloodGroupId, unitsToAdd, "Quick Demonstration Replenishment");
            RefreshInventory();
            RefreshDashboard();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Quick addition failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnExportInventoryClicked(object? sender, EventArgs e)
    {
        try
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"BloodInventory_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Blood Inventory to CSV"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                var inventory = _inventoryService.GetAllInventory();
                CsvExportService.ExportInventoryToCsv(sfd.FileName, inventory);
                MessageBox.Show($"Exported inventory report to:\n{sfd.FileName}", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DgvInventory_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        var dgv = sender as DataGridView;
        if (dgv == null || e.RowIndex < 0) return;

        if (dgv.Columns[e.ColumnIndex].Name == "StockStatus" && e.Value != null)
        {
            string status = e.Value.ToString()!;
            if (status == "Critical")
            {
                e.CellStyle!.ForeColor = UITheme.DangerRed;
                e.CellStyle!.Font = UITheme.BodyBoldFont;
            }
            else if (status == "Low Stock")
            {
                e.CellStyle!.ForeColor = UITheme.WarningAmber;
                e.CellStyle!.Font = UITheme.BodyBoldFont;
            }
            else
            {
                e.CellStyle!.ForeColor = UITheme.SuccessGreen;
                e.CellStyle!.Font = UITheme.BodyBoldFont;
            }
        }
    }

    // ==========================================
    // 4. BLOOD REQUESTS VIEW
    // ==========================================
    private void BuildRequestsView()
    {
        _viewRequests = new Panel { BackColor = UITheme.BackgroundNeutral };

        // Header Panel
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 154 };

        var lblTitle = new Label { Text = "Blood Requisition Management", Font = UITheme.HeaderLargeFont, ForeColor = UITheme.TextPrimary, AutoSize = true, Location = new Point(0, 0) };
        pnlHeader.Controls.Add(lblTitle);

        // Toolbar
        var pnlToolbar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 104, AutoScroll = true, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 4) };

        var lblStatus = new Label { Text = "Status:", AutoSize = true, Margin = new Padding(0, 10, 4, 4), Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary };
        _cboRequestStatusFilter = new ComboBox { Width = 110, Margin = new Padding(0, 5, 8, 4), DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.BodyRegularFont };
        _cboRequestStatusFilter.Items.AddRange(new object[] { "All", "Pending", "Fulfilled", "Rejected" });
        _cboRequestStatusFilter.SelectedIndex = 0;
        _cboRequestStatusFilter.SelectedIndexChanged += (s, e) => RefreshRequests();

        var lblGroup = new Label { Text = "Group:", AutoSize = true, Margin = new Padding(0, 10, 4, 4), Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary };
        _cboRequestBloodGroupFilter = new ComboBox { Width = 100, Margin = new Padding(0, 5, 8, 4), DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.BodyRegularFont };
        _cboRequestBloodGroupFilter.SelectedIndexChanged += (s, e) => RefreshRequests();

        var btnAddReq = new Button { Text = "➕ New Request", Location = new Point(320, 5), Width = 125, Height = 30 };
        UITheme.StylePrimaryButton(btnAddReq);
        btnAddReq.Text = "New Request";
        btnAddReq.Width = 112;
        btnAddReq.Height = 42;
        btnAddReq.Padding = new Padding(6, 2, 6, 2);
        btnAddReq.Click += OnAddRequestClicked;

        _btnFulfillRequest = new Button { Text = "✅ Fulfill Request", Location = new Point(455, 5), Width = 135, Height = 30, Enabled = false };
        UITheme.StyleSuccessButton(_btnFulfillRequest);
        _btnFulfillRequest.Text = "Fulfill";
        _btnFulfillRequest.Width = 82;
        _btnFulfillRequest.Height = 42;
        _btnFulfillRequest.Padding = new Padding(6, 2, 6, 2);
        _btnFulfillRequest.Click += OnFulfillRequestClicked;

        _btnRejectRequest = new Button { Text = "❌ Reject", Location = new Point(600, 5), Width = 90, Height = 30, Enabled = false };
        UITheme.StyleWarningButton(_btnRejectRequest);
        _btnRejectRequest.Text = "Reject";
        _btnRejectRequest.Width = 82;
        _btnRejectRequest.Height = 42;
        _btnRejectRequest.Padding = new Padding(6, 2, 6, 2);
        _btnRejectRequest.Click += OnRejectRequestClicked;

        _btnDeleteRequest = new Button { Text = "🗑️ Delete", Location = new Point(700, 5), Width = 85, Height = 30, Enabled = false };
        UITheme.StyleDangerButton(_btnDeleteRequest);
        _btnDeleteRequest.Text = "Delete";
        _btnDeleteRequest.Width = 82;
        _btnDeleteRequest.Height = 42;
        _btnDeleteRequest.Padding = new Padding(6, 2, 6, 2);
        _btnDeleteRequest.Click += OnDeleteRequestClicked;

        var btnExportReq = new Button { Text = "💾 CSV", Location = new Point(795, 5), Width = 75, Height = 30 };
        UITheme.StyleSecondaryButton(btnExportReq);
        btnExportReq.Text = "Export CSV";
        btnExportReq.Width = 100;
        btnExportReq.Height = 42;
        btnExportReq.Padding = new Padding(6, 2, 6, 2);
        btnExportReq.Click += OnExportRequestsClicked;

        var btnRefresh = new Button { Text = "🔄 Refresh", Location = new Point(880, 5), Width = 90, Height = 30 };
        UITheme.StyleSecondaryButton(btnRefresh);
        btnRefresh.Text = "Refresh";
        btnRefresh.Width = 82;
        btnRefresh.Height = 42;
        btnRefresh.Padding = new Padding(6, 2, 6, 2);
        btnRefresh.Click += (s, e) => RefreshRequests();

        pnlToolbar.Controls.Add(lblStatus);
        pnlToolbar.Controls.Add(_cboRequestStatusFilter);
        pnlToolbar.Controls.Add(lblGroup);
        pnlToolbar.Controls.Add(_cboRequestBloodGroupFilter);
        pnlToolbar.Controls.Add(btnAddReq);
        pnlToolbar.Controls.Add(_btnFulfillRequest);
        pnlToolbar.Controls.Add(_btnRejectRequest);
        pnlToolbar.Controls.Add(_btnDeleteRequest);
        pnlToolbar.Controls.Add(btnExportReq);
        pnlToolbar.Controls.Add(btnRefresh);

        pnlHeader.Controls.Add(pnlToolbar);

        // DataGridView
        _dgvRequests = new DataGridView { Dock = DockStyle.Fill };
        UITheme.StyleDataGridView(_dgvRequests);

        _dgvRequests.Columns.Add("RequestId", "ID");
        _dgvRequests.Columns["RequestId"]!.Width = 50;
        _dgvRequests.Columns.Add("PatientName", "Patient Name");
        _dgvRequests.Columns.Add("HospitalName", "Hospital / Clinic");
        _dgvRequests.Columns.Add("ContactNumber", "Emergency Contact");
        _dgvRequests.Columns.Add("BloodGroup", "Blood Group");
        _dgvRequests.Columns["BloodGroup"]!.Width = 95;
        _dgvRequests.Columns.Add("UnitsRequired", "Units Required");
        _dgvRequests.Columns["UnitsRequired"]!.Width = 110;
        _dgvRequests.Columns.Add("RequestDate", "Date Requested");
        _dgvRequests.Columns.Add("Status", "Status");
        _dgvRequests.Columns["Status"]!.Width = 100;
        _dgvRequests.Columns.Add("Notes", "Clinical Notes");

        _dgvRequests.SelectionChanged += (s, e) =>
        {
            if (_dgvRequests.SelectedRows.Count > 0 && _dgvRequests.SelectedRows[0].Tag is BloodRequest req)
            {
                bool isPending = string.Equals(req.Status, "Pending", StringComparison.OrdinalIgnoreCase);
                _btnFulfillRequest.Enabled = isPending;
                _btnRejectRequest.Enabled = isPending;
                _btnDeleteRequest.Enabled = true;
            }
            else
            {
                _btnFulfillRequest.Enabled = false;
                _btnRejectRequest.Enabled = false;
                _btnDeleteRequest.Enabled = false;
            }
        };

        _dgvRequests.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0) return;
            if (_dgvRequests.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                string status = e.Value.ToString()!;
                if (status == "Fulfilled")
                {
                    e.CellStyle!.ForeColor = UITheme.SuccessGreen;
                    e.CellStyle!.Font = UITheme.BodyBoldFont;
                }
                else if (status == "Pending")
                {
                    e.CellStyle!.ForeColor = UITheme.WarningAmber;
                    e.CellStyle!.Font = UITheme.BodyBoldFont;
                }
                else if (status == "Rejected")
                {
                    e.CellStyle!.ForeColor = UITheme.DangerRed;
                    e.CellStyle!.Font = UITheme.BodyBoldFont;
                }
            }
        };

        _viewRequests.Controls.Add(_dgvRequests);
        _viewRequests.Controls.Add(pnlHeader);

    }

    private void RefreshRequests()
    {
        try
        {
            string? status = _cboRequestStatusFilter.SelectedItem?.ToString();
            int? bgId = null;

            if (_cboRequestBloodGroupFilter.SelectedItem is BloodGroup selectedGroup && selectedGroup.BloodGroupId > 0)
            {
                bgId = selectedGroup.BloodGroupId;
            }

            var requests = _requestService.GetAllRequests(status, bgId);

            _dgvRequests.Rows.Clear();
            foreach (var r in requests)
            {
                int rowIndex = _dgvRequests.Rows.Add(
                    r.RequestId,
                    r.PatientName,
                    r.HospitalName,
                    r.ContactNumber,
                    r.BloodGroupName,
                    $"{r.UnitsRequired} unit(s)",
                    r.RequestDate.ToString("yyyy-MM-dd HH:mm"),
                    r.Status,
                    r.Notes ?? "-"
                );
                _dgvRequests.Rows[rowIndex].Tag = r;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load requests:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnAddRequestClicked(object? sender, EventArgs e)
    {
        using var dialog = new RequestDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                _requestService.AddRequest(dialog.ResultRequest);
                MessageBox.Show($"Requisition for patient '{dialog.ResultRequest.PatientName}' submitted successfully!", "Request Registered", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshRequests();
                RefreshDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register blood request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnFulfillRequestClicked(object? sender, EventArgs e)
    {
        if (_dgvRequests.SelectedRows.Count == 0 || _dgvRequests.SelectedRows[0].Tag is not BloodRequest req)
        {
            MessageBox.Show("Please select a pending request to fulfill.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Fulfill request #{req.RequestId} for patient '{req.PatientName}'?\n\nRequired: {req.UnitsRequired} unit(s) of {req.BloodGroupName}\nHospital: {req.HospitalName}\n\nThis will execute an atomic database transaction to deduct stock and update the request status.",
            "Confirm Transactional Fulfillment",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        var (success, message) = _requestService.FulfillRequest(req.RequestId);
        if (success)
        {
            MessageBox.Show(message, "Request Fulfilled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshRequests();
            RefreshInventory();
            RefreshDashboard();
        }
        else
        {
            MessageBox.Show(message, "Fulfillment Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnRejectRequestClicked(object? sender, EventArgs e)
    {
        if (_dgvRequests.SelectedRows.Count == 0 || _dgvRequests.SelectedRows[0].Tag is not BloodRequest req)
        {
            MessageBox.Show("Please select a pending request to reject.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Are you sure you want to REJECT request #{req.RequestId} for patient '{req.PatientName}'?",
            "Confirm Rejection",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            var (success, message) = _requestService.RejectRequest(req.RequestId, "Clinically rejected by blood bank administrator.");
            if (success)
            {
                MessageBox.Show(message, "Request Rejected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshRequests();
                RefreshDashboard();
            }
            else
            {
                MessageBox.Show(message, "Rejection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnDeleteRequestClicked(object? sender, EventArgs e)
    {
        if (_dgvRequests.SelectedRows.Count == 0 || _dgvRequests.SelectedRows[0].Tag is not BloodRequest req)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"Are you sure you want to permanently delete request #{req.RequestId} for '{req.PatientName}'?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            try
            {
                _requestService.DeleteRequest(req.RequestId);
                MessageBox.Show("Request deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshRequests();
                RefreshDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnExportRequestsClicked(object? sender, EventArgs e)
    {
        try
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"BloodRequests_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Blood Requests to CSV"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                var requests = _requestService.GetAllRequests();
                CsvExportService.ExportRequestsToCsv(sfd.FileName, requests);
                MessageBox.Show($"Exported {requests.Count} request(s) to:\n{sfd.FileName}", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ==========================================
    // 5. REPORTS & CSV EXPORT VIEW
    // ==========================================
    private void BuildReportsView()
    {
        _viewReports = new Panel { BackColor = UITheme.BackgroundNeutral };

        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70 };
        var lblTitle = new Label { Text = "Reports & Data Export Center", Font = UITheme.HeaderLargeFont, ForeColor = UITheme.TextPrimary, AutoSize = true, Location = new Point(0, 0) };
        pnlHeader.Controls.Add(lblTitle);

        var lblSub = new Label
        {
            Text = "Export standardized CSV reports for hospital audits, statistical analysis, and administrative record-keeping.",
            Font = UITheme.BodyRegularFont,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(0, 32),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblSub);

        var pnlContainer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true,
            WrapContents = false
        };

        // Report Card 1: Donors
        pnlContainer.Controls.Add(CreateReportCard(
            "📋 Donor Registry Report",
            "Generates a complete CSV record of all registered donors including blood groups, contact information, eligibility, and donation history.",
            "Export Donors CSV",
            OnExportDonorsClicked
        ));

        // Report Card 2: Inventory
        pnlContainer.Controls.Add(CreateReportCard(
            "🩸 Blood Stock Inventory Report",
            "Generates a comprehensive snapshot of stock availability across all 8 ABO/Rh blood groups with current shortage indicators.",
            "Export Inventory CSV",
            OnExportInventoryClicked
        ));

        // Report Card 3: Requests
        pnlContainer.Controls.Add(CreateReportCard(
            "🏥 Hospital Blood Requisitions Report",
            "Generates a log of all patient requisitions, fulfillment statuses, hospital details, and transfusion unit counts.",
            "Export Requests CSV",
            OnExportRequestsClicked
        ));

        void ResizeReportCards(object? sender, EventArgs e)
        {
            int cardWidth = Math.Max(400, pnlContainer.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 12);
            foreach (Control card in pnlContainer.Controls)
                card.Width = cardWidth;
        }
        pnlContainer.ClientSizeChanged += ResizeReportCards;
        ResizeReportCards(pnlContainer, EventArgs.Empty);

        _viewReports.Controls.Add(pnlContainer);
        _viewReports.Controls.Add(pnlHeader);
    }

    private static Panel CreateReportCard(string title, string description, string buttonText, EventHandler onExport)
    {
        var card = new Panel
        {
            Width = 850,
            Height = 120,
            BackColor = UITheme.CardWhite,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = UITheme.CardWhite,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 74f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26f));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var lblCardTitle = new Label { Text = title, Font = UITheme.SectionTitleFont, ForeColor = UITheme.TextPrimary, AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(4, 4, 4, 6) };
        var lblDesc = new Label { Text = description, Font = UITheme.BodyRegularFont, ForeColor = UITheme.TextSecondary, Dock = DockStyle.Fill, AutoSize = true, MaximumSize = new Size(0, 0), Padding = new Padding(4) };
        var btnExport = new Button { Text = buttonText, Width = 160, Height = 40, Anchor = AnchorStyles.None, Margin = new Padding(6) };
        UITheme.StylePrimaryButton(btnExport);
        btnExport.Click += onExport;

        layout.Controls.Add(lblCardTitle, 0, 0);
        layout.Controls.Add(lblDesc, 0, 1);
        layout.Controls.Add(btnExport, 1, 0);
        layout.SetRowSpan(btnExport, 2);
        void ResizeDescription(object? sender, EventArgs e)
        {
            lblDesc.MaximumSize = new Size(Math.Max(220, (int)(layout.ClientSize.Width * 0.74) - 20), 0);
        }
        layout.ClientSizeChanged += ResizeDescription;
        ResizeDescription(layout, EventArgs.Empty);
        card.Controls.Add(layout);

        card.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.BorderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        return card;
    }

    // ==========================================
    // 6. ABOUT & VIVA INFORMATION VIEW
    // ==========================================
    private void BuildAboutView()
    {
        _viewAbout = new Panel { BackColor = UITheme.BackgroundNeutral };

        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 48 };
        var lblTitle = new Label { Text = "About Blood Bank Management System", Font = UITheme.HeaderLargeFont, ForeColor = UITheme.TextPrimary, AutoSize = true, Location = new Point(0, 0) };
        pnlHeader.Controls.Add(lblTitle);

        var pnlCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.CardWhite,
            Padding = new Padding(24),
            AutoScroll = true
        };

        var lblInfo = new Label
        {
            Font = UITheme.BodyRegularFont,
            ForeColor = UITheme.TextPrimary,
            Text =
@"PROJECT SPECIFICATION & VIVA OVERVIEW

• Project Title: Blood Bank Management System (BBMS)
• Technology Stack: C# .NET 8 (net8.0-windows), Windows Forms, SQLite (Microsoft.Data.Sqlite)
• Architectural Pattern: Multi-Tier Architecture (Presentation, Business Services, Data Access, Entity Models)

KEY ENGINEERING HIGHLIGHTS FOR COLLEGE EVALUATION:

1. Local SQLite Relational Database Engine
   - Automatic database initialization on startup with schema creation and indexing.
   - Foreign-key enforcement explicitly enabled via 'PRAGMA foreign_keys = ON;' on every connection.
   - 100% parameterized SQL queries to prevent SQL Injection vulnerabilities.

2. Eight Standard ABO/Rh Blood Groups
   - Seeded automatically: A+, A-, B+, B-, AB+, AB-, O+, O-.
   - Strictly enforced foreign keys prevent orphan records.

3. Transactional Requisition Fulfillment
   - Uses SQLite transactions (BeginTransaction / Commit / Rollback).
   - Validates that requests are in 'Pending' state before fulfillment.
   - Atomically checks inventory stock, deducts units, and updates request status to 'Fulfilled'.
   - Automatically rolls back if inventory is insufficient, guaranteeing non-negative inventory constraints.

4. Input Validation & Business Rules
   - Donor age restriction: 18 to 65 years.
   - Mandatory phone numbers and required patient/hospital information.
   - Positive requisition quantities (UnitsRequired > 0).

5. CSV Reporting System
   - RFC 4180 compliant CSV export for Donors, Inventory Levels, and Requisition History.

DEVELOPER & DEPLOYMENT INSTRUCTIONS:
   - On Windows: Open 'BloodBankManagementSystem.sln' in Visual Studio 2022 or run 'dotnet run' in terminal.
   - On Linux / Codespaces: Compiled and verified via .NET 8 SDK CLI ('dotnet build'). GUI runs on any Windows OS.",
            AutoSize = true,
            Location = Point.Empty
        };

        void ResizeAboutText(object? sender, EventArgs e)
        {
            lblInfo.MaximumSize = new Size(Math.Max(200, pnlCard.ClientSize.Width - pnlCard.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth), 0);
        }
        pnlCard.ClientSizeChanged += ResizeAboutText;
        ResizeAboutText(pnlCard, EventArgs.Empty);

        pnlCard.Controls.Add(lblInfo);
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.BorderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        _viewAbout.Controls.Add(pnlCard);
        _viewAbout.Controls.Add(pnlHeader);
    }

    private void LoadInitialData()
    {
        // Populate Blood Group dropdown filters
        var groups = _bloodGroupService.GetAllBloodGroups();

        var donorFilterList = new List<BloodGroup> { new() { BloodGroupId = 0, GroupName = "All Blood Groups" } };
        donorFilterList.AddRange(groups);
        _cboDonorBloodGroupFilter.DisplayMember = "GroupName";
        _cboDonorBloodGroupFilter.ValueMember = "BloodGroupId";
        _cboDonorBloodGroupFilter.DataSource = donorFilterList;

        var reqFilterList = new List<BloodGroup> { new() { BloodGroupId = 0, GroupName = "All" } };
        reqFilterList.AddRange(groups);
        _cboRequestBloodGroupFilter.DisplayMember = "GroupName";
        _cboRequestBloodGroupFilter.ValueMember = "BloodGroupId";
        _cboRequestBloodGroupFilter.DataSource = reqFilterList;

        RefreshDashboard();
    }
}
