using System.Drawing;
using System.Windows.Forms;
using BloodBankManagementSystem.Helpers;
using BloodBankManagementSystem.Models;
using BloodBankManagementSystem.Services;

namespace BloodBankManagementSystem.Forms;

/// <summary>
/// Modal dialog for registering a new blood donor or editing an existing donor record.
/// </summary>
public class DonorDialog : Form
{
    private readonly BloodGroupService _bloodGroupService = new();
    private readonly Donor? _existingDonor;

    private TextBox _txtFullName = null!;
    private NumericUpDown _numAge = null!;
    private ComboBox _cboGender = null!;
    private TextBox _txtPhone = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtCity = null!;
    private ComboBox _cboBloodGroup = null!;
    private CheckBox _chkHasDonated = null!;
    private DateTimePicker _dtpLastDonation = null!;
    private Label _lblError = null!;

    public Donor ResultDonor { get; private set; } = null!;

    public DonorDialog(Donor? existingDonor = null)
    {
        _existingDonor = existingDonor;
        InitializeComponent();
        LoadBloodGroups();

        if (_existingDonor != null)
        {
            PopulateExistingData(_existingDonor);
        }
    }

    private void InitializeComponent()
    {
        Text = _existingDonor == null ? "Register New Blood Donor" : "Edit Donor Information";
        Width = 520;
        Height = 580;
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
            Text = _existingDonor == null ? "New Donor Registration" : "Edit Donor Record",
            Font = UITheme.HeaderMediumFont,
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 12)
        };

        var lblSubHeader = new Label
        {
            Text = "Please enter accurate donor details. Donor must be 18+ years old.",
            Font = UITheme.SmallMutedFont,
            ForeColor = Color.FromArgb(254, 226, 226),
            AutoSize = true,
            Location = new Point(20, 36)
        };

        pnlHeader.Controls.Add(lblHeader);
        pnlHeader.Controls.Add(lblSubHeader);
        Controls.Add(pnlHeader);

        // Body Form Container
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30, 80, 30, 20),
            AutoScroll = true
        };

        int y = 80;
        int inputWidth = 280;

        // Full Name
        pnlBody.Controls.Add(CreateFieldLabel("Full Name *", 30, y));
        _txtFullName = new TextBox { Location = new Point(160, y), Width = inputWidth, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_txtFullName);
        y += 40;

        // Age
        pnlBody.Controls.Add(CreateFieldLabel("Age (18-65) *", 30, y));
        _numAge = new NumericUpDown { Location = new Point(160, y), Width = 100, Minimum = 18, Maximum = 65, Value = 25, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_numAge);

        // Gender (inline next to Age)
        var lblGender = new Label { Text = "Gender *", Location = new Point(280, y + 2), AutoSize = true, Font = UITheme.BodyBoldFont, ForeColor = UITheme.TextPrimary };
        _cboGender = new ComboBox { Location = new Point(345, y), Width = 95, DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.BodyRegularFont };
        _cboGender.Items.AddRange(new object[] { "Male", "Female", "Other" });
        _cboGender.SelectedIndex = 0;
        pnlBody.Controls.Add(lblGender);
        pnlBody.Controls.Add(_cboGender);
        y += 40;

        // Blood Group
        pnlBody.Controls.Add(CreateFieldLabel("Blood Group *", 30, y));
        _cboBloodGroup = new ComboBox { Location = new Point(160, y), Width = inputWidth, DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_cboBloodGroup);
        y += 40;

        // Phone
        pnlBody.Controls.Add(CreateFieldLabel("Phone Number *", 30, y));
        _txtPhone = new TextBox { Location = new Point(160, y), Width = inputWidth, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_txtPhone);
        y += 40;

        // Email
        pnlBody.Controls.Add(CreateFieldLabel("Email Address", 30, y));
        _txtEmail = new TextBox { Location = new Point(160, y), Width = inputWidth, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_txtEmail);
        y += 40;

        // City
        pnlBody.Controls.Add(CreateFieldLabel("City / Location", 30, y));
        _txtCity = new TextBox { Location = new Point(160, y), Width = inputWidth, Font = UITheme.BodyRegularFont };
        pnlBody.Controls.Add(_txtCity);
        y += 40;

        // Last Donation Date
        _chkHasDonated = new CheckBox
        {
            Text = "Has donated blood before",
            Location = new Point(160, y),
            AutoSize = true,
            Font = UITheme.BodyRegularFont,
            ForeColor = UITheme.TextPrimary
        };
        pnlBody.Controls.Add(_chkHasDonated);
        y += 30;

        _dtpLastDonation = new DateTimePicker
        {
            Location = new Point(160, y),
            Width = inputWidth,
            Format = DateTimePickerFormat.Short,
            Font = UITheme.BodyRegularFont,
            Enabled = false,
            MaxDate = DateTime.Today
        };
        _chkHasDonated.CheckedChanged += (s, e) => _dtpLastDonation.Enabled = _chkHasDonated.Checked;
        pnlBody.Controls.Add(_dtpLastDonation);
        y += 45;

        // Error message label
        _lblError = new Label
        {
            Location = new Point(30, y),
            Width = 420,
            Height = 35,
            ForeColor = UITheme.DangerRed,
            Font = UITheme.SmallMutedFont,
            Visible = false
        };
        pnlBody.Controls.Add(_lblError);
        y += 40;

        // Action Buttons
        var btnSave = new Button
        {
            Text = _existingDonor == null ? "Save Donor" : "Update Donor",
            Location = new Point(230, y),
            Width = 120,
            Height = 36
        };
        UITheme.StylePrimaryButton(btnSave);
        btnSave.Click += OnSaveClicked;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(360, y),
            Width = 90,
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

    private static Label CreateFieldLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y + 3),
            Width = 120,
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

    private void PopulateExistingData(Donor donor)
    {
        _txtFullName.Text = donor.FullName;
        _numAge.Value = Math.Clamp(donor.Age, 18, 65);
        _cboGender.SelectedItem = donor.Gender;
        _txtPhone.Text = donor.Phone;
        _txtEmail.Text = donor.Email ?? string.Empty;
        _txtCity.Text = donor.City ?? string.Empty;

        _cboBloodGroup.SelectedValue = donor.BloodGroupId;

        if (donor.LastDonationDate.HasValue)
        {
            _chkHasDonated.Checked = true;
            _dtpLastDonation.Enabled = true;
            _dtpLastDonation.Value = donor.LastDonationDate.Value <= DateTime.Today ? donor.LastDonationDate.Value : DateTime.Today;
        }
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        _lblError.Visible = false;

        var bloodGroup = _cboBloodGroup.SelectedItem as BloodGroup;
        if (bloodGroup == null)
        {
            ShowError("Please select a blood group.");
            return;
        }

        var donor = new Donor
        {
            DonorId = _existingDonor?.DonorId ?? 0,
            FullName = _txtFullName.Text.Trim(),
            Age = (int)_numAge.Value,
            Gender = _cboGender.SelectedItem?.ToString() ?? "Other",
            Phone = _txtPhone.Text.Trim(),
            Email = string.IsNullOrWhiteSpace(_txtEmail.Text) ? null : _txtEmail.Text.Trim(),
            City = string.IsNullOrWhiteSpace(_txtCity.Text) ? null : _txtCity.Text.Trim(),
            BloodGroupId = bloodGroup.BloodGroupId,
            BloodGroupName = bloodGroup.GroupName,
            LastDonationDate = _chkHasDonated.Checked ? _dtpLastDonation.Value.Date : null,
            CreatedAt = _existingDonor?.CreatedAt ?? DateTime.Now
        };

        if (!ValidationHelper.ValidateDonor(donor, out var validationError))
        {
            ShowError(validationError);
            return;
        }

        ResultDonor = donor;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowError(string message)
    {
        _lblError.Text = message;
        _lblError.Visible = true;
    }
}
