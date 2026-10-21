using System.Drawing;
using System.Windows.Forms;
using BloodBankManagementSystem.Helpers;
using BloodBankManagementSystem.Models;
using BloodBankManagementSystem.Services;

namespace BloodBankManagementSystem.Forms;

/// <summary>
/// Modal dialog for registering a new blood requisition from a hospital or patient.
/// </summary>
public class RequestDialog : Form
{
    private readonly BloodGroupService _bloodGroupService = new();

    private TextBox _txtPatientName = null!;
    private TextBox _txtHospitalName = null!;
    private TextBox _txtContact = null!;
    private ComboBox _cboBloodGroup = null!;
    private NumericUpDown _numUnits = null!;
    private TextBox _txtNotes = null!;
    private Label _lblError = null!;

    public BloodRequest ResultRequest { get; private set; } = null!;

    public RequestDialog()
    {
        InitializeComponent();
        LoadBloodGroups();
    }

    private void InitializeComponent()
    {
        Text = "New Blood Requisition";
        Width = 540;
        Height = 600;
        MinimumSize = new Size(500, 560);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.CardWhite;
        Font = UITheme.BodyRegularFont;

        SuspendLayout();

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = UITheme.PrimaryBurgundy,
            Padding = new Padding(20, 12, 20, 10)
        };

        var lblHeader = new Label
        {
            Text = "Register Blood Request",
            Font = UITheme.HeaderMediumFont,
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 12)
        };

        var lblSub = new Label
        {
            Text = "Specify patient and hospital transfusion requirements accurately.",
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

        var btnSave = new Button
        {
            Text = "Submit Request",
            Width = 150,
            Height = 36,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 265, 12)
        };
        UITheme.StylePrimaryButton(btnSave);
        btnSave.Click += OnSaveClicked;

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

        pnlBottom.Controls.Add(btnSave);
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

        // 0. Patient Name
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Patient Name *"), 0, 0);
        _txtPatientName = new TextBox { Dock = DockStyle.Fill, Font = UITheme.BodyRegularFont };
        formLayout.Controls.Add(_txtPatientName, 1, 0);

        // 1. Hospital Name
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Hospital / Clinic *"), 0, 1);
        _txtHospitalName = new TextBox { Dock = DockStyle.Fill, Font = UITheme.BodyRegularFont };
        formLayout.Controls.Add(_txtHospitalName, 1, 1);

        // 2. Contact Phone
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Contact Phone *"), 0, 2);
        _txtContact = new TextBox { Dock = DockStyle.Fill, Font = UITheme.BodyRegularFont };
        formLayout.Controls.Add(_txtContact, 1, 2);

        // 3. Blood Group
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Blood Group *"), 0, 3);
        _cboBloodGroup = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UITheme.BodyRegularFont
        };
        formLayout.Controls.Add(_cboBloodGroup, 1, 3);

        // 4. Units Required
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Units Required *"), 0, 4);
        _numUnits = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Width = 120,
            Minimum = 1,
            Maximum = 100,
            Value = 1,
            Font = UITheme.BodyRegularFont
        };
        formLayout.Controls.Add(_numUnits, 1, 4);

        // 5. Notes / Urgency
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));
        formLayout.Controls.Add(CreateFieldLabel("Notes / Urgency"), 0, 5);
        _txtNotes = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = UITheme.BodyRegularFont
        };
        formLayout.Controls.Add(_txtNotes, 1, 5);

        // 6. Error message
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

        AcceptButton = btnSave;
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
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        _lblError.Visible = false;

        var group = _cboBloodGroup.SelectedItem as BloodGroup;
        if (group == null)
        {
            ShowError("Please select a blood group.");
            return;
        }

        var request = new BloodRequest
        {
            PatientName = _txtPatientName.Text.Trim(),
            HospitalName = _txtHospitalName.Text.Trim(),
            ContactNumber = _txtContact.Text.Trim(),
            BloodGroupId = group.BloodGroupId,
            BloodGroupName = group.GroupName,
            UnitsRequired = (int)_numUnits.Value,
            RequestDate = DateTime.Now,
            Status = "Pending",
            Notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim()
        };

        if (!ValidationHelper.ValidateRequest(request, out var validationError))
        {
            ShowError(validationError);
            return;
        }

        ResultRequest = request;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowError(string message)
    {
        _lblError.Text = message;
        _lblError.Visible = true;
    }
}
