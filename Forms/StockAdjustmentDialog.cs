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
        Width = 540;
        Height = 580;
        MinimumSize = new Size(500, 540);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.CardWhite;
        Font = UITheme.BodyRegularFont;

        SuspendLayout();

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
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
            Location = new Point(20, 36)
        };

        pnlHeader.Controls.Add(lblHeader);
        pnlHeader.Controls.Add(lblSub);

        // Bottom Action Button Bar
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            BackColor = UITheme.CardWhite,
            Padding = new Padding(20, 12, 20, 12)
        };
        pnlBottom.Paint += (s, e) =>
        {
            using var pen = new Pen(UITheme.BorderColor, 1);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        var btnApply = new Button
        {
            Text = "Apply Stock Adjustment",
            Width = 190,
            Height = 36,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 305, 12)
        };
        UITheme.StylePrimaryButton(btnApply);
        btnApply.Click += OnApplyClicked;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Width = 95,
            Height = 36,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 105, 12)
        };
        UITheme.StyleSecondaryButton(btnCancel);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        pnlBottom.Controls.Add(btnApply);
        pnlBottom.Controls.Add(btnCancel);

        // Body Form Container
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 16, 24, 16),
            AutoScroll = true
        };

        var formLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(0)
        };
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        // 0. Blood Group
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Blood Group *"), 0, 0);
        _cboBloodGroup = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UITheme.BodyRegularFont
        };
        _cboBloodGroup.SelectedIndexChanged += (s, e) => UpdateCurrentStockInfo();
        formLayout.Controls.Add(_cboBloodGroup, 1, 0);

        // 1. Current Available Stock
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
        formLayout.Controls.Add(CreateFieldLabel("Current Available:"), 0, 1);
        _lblCurrentStock = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "0 units",
            Font = UITheme.BodyBoldFont,
            ForeColor = UITheme.PrimaryBurgundy
        };
        formLayout.Controls.Add(_lblCurrentStock, 1, 1);

        // 2. Operation Type Radio Buttons
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Operation Type *"), 0, 2);
        var pnlOps = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        _rbAdd = new RadioButton { Text = "+ Add Units", AutoSize = true, Checked = true, Font = UITheme.BodyRegularFont, ForeColor = UITheme.SuccessGreen, Margin = new Padding(0, 8, 16, 0) };
        _rbDeduct = new RadioButton { Text = "- Deduct Units", AutoSize = true, Checked = false, Font = UITheme.BodyRegularFont, ForeColor = UITheme.DangerRed, Margin = new Padding(0, 8, 0, 0) };
        _rbAdd.CheckedChanged += (s, e) => RecalculatePreview();
        _rbDeduct.CheckedChanged += (s, e) => RecalculatePreview();
        pnlOps.Controls.Add(_rbAdd);
        pnlOps.Controls.Add(_rbDeduct);
        formLayout.Controls.Add(pnlOps, 1, 2);

        // 3. Units Quantity
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Units Quantity *"), 0, 3);
        _numUnits = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Width = 140,
            Minimum = 1,
            Maximum = 200,
            Value = 5,
            Font = UITheme.BodyRegularFont
        };
        _numUnits.ValueChanged += (s, e) => RecalculatePreview();
        formLayout.Controls.Add(_numUnits, 1, 3);

        // 4. Reason
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Reason / Source"), 0, 4);
        _cboReason = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDown,
            Font = UITheme.BodyRegularFont
        };
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
        formLayout.Controls.Add(_cboReason, 1, 4);

        // 5. Projected Stock Preview Box
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        formLayout.Controls.Add(CreateFieldLabel("Projected Result"), 0, 5);
        var pnlPreview = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.BackgroundNeutral,
            Padding = new Padding(8),
            Margin = new Padding(0, 4, 0, 4)
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
        formLayout.Controls.Add(pnlPreview, 1, 5);

        // 6. Error Label
        formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _lblError = new Label
        {
            Dock = DockStyle.Fill,
            Height = 30,
            ForeColor = UITheme.DangerRed,
            Font = UITheme.SmallMutedFont,
            Visible = false
        };
        formLayout.Controls.Add(_lblError, 0, 6);
        formLayout.SetColumnSpan(_lblError, 2);

        pnlBody.Controls.Add(formLayout);

        Controls.Add(pnlBody);
        Controls.Add(pnlBottom);
        Controls.Add(pnlHeader);

        pnlHeader.SendToBack();
        pnlBottom.SendToBack();
        pnlBody.BringToFront();

        AcceptButton = btnApply;
        CancelButton = btnCancel;

        ResumeLayout(true);
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UITheme.BodyBoldFont,
            ForeColor = UITheme.TextPrimary
        };
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
