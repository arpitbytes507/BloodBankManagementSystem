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
        Width = 540;
        Height = 630;
        MinimumSize = new Size(500, 580);
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
            Text = _existingDonor == null ? "Save Donor" : "Update Donor",
            Width = 130,
            Height = 36,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 245, 12)
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
            RowCount = 9,
            Padding = new Padding(0)
        };
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135f));
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        // 0. Full Name
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Full Name *"), 0, 0);
        _txtFullName = new TextBox { Dock = DockStyle.Fill, Font = UITheme.BodyRegularFont };
        formLayout.Controls.Add(_txtFullName, 1, 0);

        // 1. Age and Gender inline
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Age (18-65) *"), 0, 1);
        var pnlAgeGender = new Panel { Dock = DockStyle.Fill };
        _numAge = new NumericUpDown
        {
            Location = new Point(0, 0),
            Width = 80,
            Minimum = 18,
            Maximum = 65,
            Value = 25,
            Font = UITheme.BodyRegularFont
        };
        var lblGender = new Label
        {
            Text = "Gender *",
            Location = new Point(95, 3),
            Width = 70,
            Font = UITheme.BodyBoldFont,
            ForeColor = UITheme.TextPrimary
        };
        _cboGender = new ComboBox
        {
            Location = new Point(170, 0),
            Width = 110,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UITheme.BodyRegularFont
        };
        _cboGender.Items.AddRange(new object[] { "Male", "Female", "Other" });
        _cboGender.SelectedIndex = 0;
        pnlAgeGender.Controls.Add(_numAge);
        pnlAgeGender.Controls.Add(lblGender);
        pnlAgeGender.Controls.Add(_cboGender);
        formLayout.Controls.Add(pnlAgeGender, 1, 1);

        // 2. Blood Group
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Blood Group *"), 0, 2);
        _cboBloodGroup = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UITheme.BodyRegularFont
        };
        formLayout.Controls.Add(_cboBloodGroup, 1, 2);

        // 3. Phone
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Phone Number *"), 0, 3);
        _txtPhone = new TextBox { Dock = DockStyle.Fill, Font = UITheme.BodyRegularFont };
        formLayout.Controls.Add(_txtPhone, 1, 3);

        // 4. Email
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Email Address"), 0, 4);
        _txtEmail = new TextBox { Dock = DockStyle.Fill, Font = UITheme.BodyRegularFont };
        formLayout.Controls.Add(_txtEmail, 1, 4);

        // 5. City
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("City / Location"), 0, 5);
        _txtCity = new TextBox { Dock = DockStyle.Fill, Font = UITheme.BodyRegularFont };
        formLayout.Controls.Add(_txtCity, 1, 5);

        // 6. Has Donated Before
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
        formLayout.Controls.Add(CreateFieldLabel("Donation History"), 0, 6);
        _chkHasDonated = new CheckBox
        {
            Text = "Has donated blood before",
            Dock = DockStyle.Fill,
            Font = UITheme.BodyRegularFont,
            ForeColor = UITheme.TextPrimary
        };
        formLayout.Controls.Add(_chkHasDonated, 1, 6);

        // 7. Last Donation Date
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        formLayout.Controls.Add(CreateFieldLabel("Last Donated"), 0, 7);
        _dtpLastDonation = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Short,
            Font = UITheme.BodyRegularFont,
            Enabled = false,
            MaxDate = DateTime.Today
        };
        _chkHasDonated.CheckedChanged += (s, e) => _dtpLastDonation.Enabled = _chkHasDonated.Checked;
        formLayout.Controls.Add(_dtpLastDonation, 1, 7);

        // 8. Error Label
        formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _lblError = new Label
        {
            Dock = DockStyle.Fill,
            Height = 30,
            ForeColor = UITheme.DangerRed,
            Font = UITheme.SmallMutedFont,
            Visible = false
        };
        formLayout.Controls.Add(_lblError, 0, 8);
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
