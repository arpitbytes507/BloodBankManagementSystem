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
        Width = 520;
        Height = 540;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.CardWhite;
        Font = UITheme.BodyRegularFont;

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
        Controls.Add(pnlHeader);

        // Body Form Container
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30, 80, 30, 20)
        };

        int y = 80;
        int inputWidth = 270;

        // Patient Name
        pnlBody.Controls.Add(new Label { Text = "Patient Name *", Location = new Point(30, y + 3), Width = 130, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary });
        _txtPatientName = new TextBox { Location = new Point(170, y), Width = inputWidth, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_txtPatientName);
        y += 40;

        // Hospital Name
        pnlBody.Controls.Add(new Label { Text = "Hospital / Clinic *", Location = new Point(30, y + 3), Width = 130, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary });
        _txtHospitalName = new TextBox { Location = new Point(170, y), Width = inputWidth, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_txtHospitalName);
        y += 40;

        // Contact Number
        pnlBody.Controls.Add(new Label { Text = "Contact Phone *", Location = new Point(30, y + 3), Width = 130, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary });
        _txtContact = new TextBox { Location = new Point(170, y), Width = inputWidth, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_txtContact);
        y += 40;

        // Blood Group
        pnlBody.Controls.Add(new Label { Text = "Blood Group *", Location = new Point(30, y + 3), Width = 130, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary });
        _cboBloodGroup = new ComboBox { Location = new Point(170, y), Width = inputWidth, DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_cboBloodGroup);
        y += 40;

        // Units Required
        pnlBody.Controls.Add(new Label { Text = "Units Required *", Location = new Point(30, y + 3), Width = 130, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary });
        _numUnits = new NumericUpDown { Location = new Point(170, y), Width = 110, Minimum = 1, Maximum = 100, Value = 1, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_numUnits);
        y += 40;

        // Notes / Clinical reason
        pnlBody.Controls.Add(new Label { Text = "Notes / Urgency", Location = new Point(30, y + 3), Width = 130, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary });
        _txtNotes = new TextBox { Location = new Point(170, y), Width = inputWidth, Height = 60, Multiline = true, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_txtNotes);
        y += 75;

        // Error message
        _lblError = new Label
        {
            Location = new Point(30, y),
            Width = 410,
            Height = 30,
            ForeColor = UITheme.DangerRed,
            Font = UITheme.SmallMutedFont,
            Visible = false
        };
        pnlBody.Controls.Add(_lblError);
        y += 35;

        // Action Buttons
        var btnSave = new Button
        {
            Text = "Submit Request",
            Location = new Point(200, y),
            Width = 130,
            Height = 36
        };
        UITheme.StylePrimaryButton(btnSave);
        btnSave.Click += OnSaveClicked;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(340, y),
            Width = 100,
            Height = 36
        };
        UITheme.StyleSecondaryButton(btnCancel);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        pnlBody.Controls.Add(btnSave);
        pnlBody.Controls.Add(btnCancel);

        Controls.Add(pnlBody);
        AcceptButton = btnSave;
        CancelButton = btnCancel;
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
