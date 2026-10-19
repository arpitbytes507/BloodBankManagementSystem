using System.Drawing;
using System.Windows.Forms;
using BloodBankManagementSystem.Helpers;
using BloodBankManagementSystem.Models;
using BloodBankManagementSystem.Services;

namespace BloodBankManagementSystem.Forms;

/// <summary>
/// Modal dialog for manually adjusting inventory stock during academic prototype testing.
/// Clearly labelled as demonstration inventory adjustments.
/// </summary>
public class StockAdjustmentDialog : Form
{
    private readonly BloodGroupService _bloodGroupService = new();
    private readonly InventoryService _inventoryService = new();
    private readonly int? _preselectedBloodGroupId;

    private ComboBox _cboBloodGroup = null!;
    private Label _lblCurrentStock = null!;
    private RadioButton _rbAdd = null!;
    private RadioButton _rbDeduct = null!;
    private NumericUpDown _numUnits = null!;
    private ComboBox _cboReason = null!;
    private Label _lblProjectedStock = null!;
    private Label _lblError = null!;

    private int _currentStock = 0;

    public int SelectedBloodGroupId { get; private set; }
    public int UnitsAdjustment { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    public StockAdjustmentDialog(int? preselectedBloodGroupId = null)
    {
        _preselectedBloodGroupId = preselectedBloodGroupId;
        InitializeComponent();
        LoadBloodGroups();
    }

    private void InitializeComponent()
    {
        Text = "Demonstration Stock Adjustment";
        Width = 520;
        Height = 520;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.CardWhite;
        Font = UITheme.BodyRegularFont;

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = UITheme.DarkBurgundy,
            Padding = new Padding(20, 10, 20, 10)
        };

        var lblHeader = new Label
        {
            Text = "Demonstration Stock Adjustment",
            Font = UITheme.HeaderMediumFont,
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 12)
        };

        var lblSub = new Label
        {
            Text = "Academic Prototype: Enter manual stock adjustments for demonstration & testing.",
            Font = UITheme.SmallMutedFont,
            ForeColor = Color.FromArgb(254, 226, 226),
            AutoSize = true,
            Location = new Point(20, 38)
        };

        pnlHeader.Controls.Add(lblHeader);
        pnlHeader.Controls.Add(lblSub);
        Controls.Add(pnlHeader);

        // Body
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30, 85, 30, 20)
        };

        int y = 85;

        // Blood Group
        pnlBody.Controls.Add(new Label { Text = "Blood Group *", Location = new Point(30, y + 3), Width = 140, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary });
        _cboBloodGroup = new ComboBox { Location = new Point(180, y), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.BodyRegularFont };
        _cboBloodGroup.SelectedIndexChanged += (s, e) => UpdateCurrentStockInfo();
        pnlBody.Controls.Add(_cboBloodGroup);
        y += 42;

        // Current Stock display
        pnlBody.Controls.Add(new Label { Text = "Current Available:", Location = new Point(30, y + 3), Width = 140, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary });
        _lblCurrentStock = new Label { Text = "0 units", Location = new Point(180, y + 3), Width = 260, Font = UITheme.BodyBoldFont, ForeColor = UITheme.PrimaryBurgundy };
        pnlBody.Controls.Add(_lblCurrentStock);
        y += 38;

        // Adjustment Type (Radio buttons: Add or Deduct)
        pnlBody.Controls.Add(new Label { Text = "Operation Type *", Location = new Point(30, y + 2), Width = 140, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary });

        _rbAdd = new RadioButton { Text = "+ Add Units (Stock In)", Location = new Point(180, y), Width = 160, Checked = true, Font = UITheme.BodyRegularFont, ForeColor = UITheme.SuccessGreen };
        _rbDeduct = new RadioButton { Text = "- Deduct Units (Stock Out)", Location = new Point(345, y), Width = 170, Checked = false, Font = UITheme.BodyRegularFont, ForeColor = UITheme.DangerRed };
        _rbAdd.CheckedChanged += (s, e) => RecalculatePreview();
        _rbDeduct.CheckedChanged += (s, e) => RecalculatePreview();
        pnlBody.Controls.Add(_rbAdd);
        pnlBody.Controls.Add(_rbDeduct);
        y += 42;

        // Units Quantity
        pnlBody.Controls.Add(new Label { Text = "Units Quantity *", Location = new Point(30, y + 3), Width = 140, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary });
        _numUnits = new NumericUpDown { Location = new Point(180, y), Width = 130, Minimum = 1, Maximum = 200, Value = 5, Font = UITheme.BodyRegularFont };
        _numUnits.ValueChanged += (s, e) => RecalculatePreview();
        pnlBody.Controls.Add(_numUnits);
        y += 42;

        // Reason / Category
        pnlBody.Controls.Add(new Label { Text = "Reason / Source", Location = new Point(30, y + 3), Width = 140, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary });
        _cboReason = new ComboBox { Location = new Point(180, y), Width = 260, DropDownStyle = ComboBoxStyle.DropDown, Font = UITheme.BodyRegularFont };
        _cboReason.Items.AddRange(new object[]
        {
            "Voluntary Blood Donation Drive",
            "Hospital Stock Transfer",
            "Emergency Reserve Replenishment",
            "Academic Demonstration & Testing",
            "Laboratory Quality Inspection",
            "Expired Unit Quarantine"
        });
        _cboReason.SelectedIndex = 0;
        pnlBody.Controls.Add(_cboReason);
        y += 42;

        // Projected Stock Preview Box
        var pnlPreview = new Panel
        {
            Location = new Point(30, y),
            Width = 440,
            Height = 45,
            BackColor = UITheme.BackgroundNeutral
        };
        _lblProjectedStock = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UITheme.BodyBoldFont,
            ForeColor = UITheme.TextPrimary,
            Text = "Projected Stock: 0 -> 5 units"
        };
        pnlPreview.Controls.Add(_lblProjectedStock);
        pnlBody.Controls.Add(pnlPreview);
        y += 55;

        // Error message
        _lblError = new Label
        {
            Location = new Point(30, y),
            Width = 440,
            Height = 25,
            ForeColor = UITheme.DangerRed,
            Font = UITheme.SmallMutedFont,
            Visible = false
        };
        pnlBody.Controls.Add(_lblError);
        y += 30;

        // Action Buttons
        var btnApply = new Button
        {
            Text = "Apply Stock Adjustment",
            Location = new Point(190, y),
            Width = 175,
            Height = 36
        };
        UITheme.StylePrimaryButton(btnApply);
        btnApply.Click += OnApplyClicked;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(375, y),
            Width = 95,
            Height = 36
        };
        UITheme.StyleSecondaryButton(btnCancel);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        pnlBody.Controls.Add(btnApply);
        pnlBody.Controls.Add(btnCancel);

        Controls.Add(pnlBody);
        AcceptButton = btnApply;
        CancelButton = btnCancel;
    }

    private void LoadBloodGroups()
    {
        var groups = _bloodGroupService.GetAllBloodGroups();
        _cboBloodGroup.DisplayMember = "GroupName";
        _cboBloodGroup.ValueMember = "BloodGroupId";
        _cboBloodGroup.DataSource = groups;

        if (_preselectedBloodGroupId.HasValue)
        {
            _cboBloodGroup.SelectedValue = _preselectedBloodGroupId.Value;
        }

        UpdateCurrentStockInfo();
    }

    private void UpdateCurrentStockInfo()
    {
        if (_cboBloodGroup.SelectedItem is BloodGroup group)
        {
            var inv = _inventoryService.GetByBloodGroupId(group.BloodGroupId);
            _currentStock = inv?.UnitsAvailable ?? 0;
            _lblCurrentStock.Text = $"{_currentStock} units available";
            RecalculatePreview();
        }
    }

    private void RecalculatePreview()
    {
        int qty = (int)_numUnits.Value;
        int diff = _rbAdd.Checked ? qty : -qty;
        int projected = _currentStock + diff;

        if (projected < 0)
        {
            _lblProjectedStock.Text = $"INVALID: Stock cannot become negative! ({_currentStock} - {qty} = {projected})";
            _lblProjectedStock.ForeColor = UITheme.DangerRed;
        }
        else
        {
            _lblProjectedStock.Text = $"Projected Stock: {_currentStock} {(diff >= 0 ? "+" : "-")} {Math.Abs(diff)} = {projected} units";
            _lblProjectedStock.ForeColor = UITheme.TextPrimary;
        }
    }

    private void OnApplyClicked(object? sender, EventArgs e)
    {
        _lblError.Visible = false;

        var group = _cboBloodGroup.SelectedItem as BloodGroup;
        if (group == null)
        {
            ShowError("Please select a valid blood group.");
            return;
        }

        int qty = (int)_numUnits.Value;
        int adjustment = _rbAdd.Checked ? qty : -qty;

        if (!ValidationHelper.ValidateStockAdjustment(_currentStock, adjustment, out var validationError))
        {
            ShowError(validationError);
            return;
        }

        SelectedBloodGroupId = group.BloodGroupId;
        UnitsAdjustment = adjustment;
        Reason = string.IsNullOrWhiteSpace(_cboReason.Text) ? "Demonstration Stock Adjustment" : _cboReason.Text.Trim();

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowError(string message)
    {
        _lblError.Text = message;
        _lblError.Visible = true;
    }
}
