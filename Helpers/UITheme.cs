using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BloodBankManagementSystem.Helpers;

/// <summary>
/// Centralized UI design system providing a modern, professional,
/// blood-bank-themed appearance for all Windows Forms controls.
/// </summary>
public static class UITheme
{
    // Color Palette - Blood Bank Theme
    public static readonly Color PrimaryBurgundy    = Color.FromArgb(139, 0, 0);     // #8B0000
    public static readonly Color DarkBurgundy       = Color.FromArgb(100, 0, 0);     // #640000
    public static readonly Color LightBurgundy      = Color.FromArgb(185, 28, 28);   // #B91C1C
    public static readonly Color SoftRedBadge       = Color.FromArgb(254, 242, 242); // #FEF2F2

    public static readonly Color SidebarDark        = Color.FromArgb(30, 34, 42);    // #1E222A
    public static readonly Color SidebarItemHover   = Color.FromArgb(45, 52, 64);    // #2D3440
    public static readonly Color SidebarActive      = Color.FromArgb(139, 0, 0);     // #8B0000

    public static readonly Color BackgroundNeutral  = Color.FromArgb(244, 246, 249); // #F4F6F9
    public static readonly Color CardWhite          = Color.FromArgb(255, 255, 255); // #FFFFFF
    public static readonly Color BorderColor        = Color.FromArgb(226, 232, 240); // #E2E8F0

    public static readonly Color TextPrimary        = Color.FromArgb(30, 41, 59);    // #1E293B
    public static readonly Color TextSecondary      = Color.FromArgb(100, 116, 139); // #64748B
    public static readonly Color TextMuted          = Color.FromArgb(148, 163, 184); // #94A3B8

    public static readonly Color SuccessGreen       = Color.FromArgb(22, 163, 74);   // #16A34A
    public static readonly Color WarningAmber       = Color.FromArgb(217, 119, 6);   // #D97706
    public static readonly Color DangerRed          = Color.FromArgb(220, 38, 38);   // #DC2626
    public static readonly Color InfoBlue           = Color.FromArgb(37, 99, 235);   // #2563EB

    // Standard Fonts
    public static readonly Font HeaderLargeFont    = new("Segoe UI", 16f, FontStyle.Bold);
    public static readonly Font HeaderMediumFont   = new("Segoe UI", 12f, FontStyle.Bold);
    public static readonly Font SectionTitleFont   = new("Segoe UI", 10.5f, FontStyle.Bold);
    public static readonly Font BodyRegularFont    = new("Segoe UI", 9.5f, FontStyle.Regular);
    public static readonly Font BodyBoldFont       = new("Segoe UI", 9.5f, FontStyle.Bold);
    public static readonly Font SmallMutedFont     = new("Segoe UI", 8.5f, FontStyle.Regular);
    public static readonly Font MetricNumberFont   = new("Segoe UI", 22f, FontStyle.Bold);

    /// <summary>
    /// Applies a modern flat styling with rounded corners to buttons.
    /// </summary>
    public static void StyleButton(Button button, Color backColor, Color foreColor)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Font = BodyBoldFont;
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(12, 6, 12, 6);
        button.UseVisualStyleBackColor = false;
    }

    public static void StylePrimaryButton(Button button) =>
        StyleButton(button, PrimaryBurgundy, Color.White);

    public static void StyleSuccessButton(Button button) =>
        StyleButton(button, SuccessGreen, Color.White);

    public static void StyleWarningButton(Button button) =>
        StyleButton(button, WarningAmber, Color.White);

    public static void StyleDangerButton(Button button) =>
        StyleButton(button, DangerRed, Color.White);

    public static void StyleSecondaryButton(Button button)
    {
        StyleButton(button, Color.FromArgb(241, 245, 249), TextPrimary);
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
    }

    /// <summary>
    /// Configures a DataGridView with clean spacing, alternating row colors,
    /// and full row selection for readability.
    /// </summary>
    public static void StyleDataGridView(DataGridView dgv)
    {
        dgv.BackgroundColor = Color.White;
        dgv.BorderStyle = BorderStyle.None;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.GridColor = Color.FromArgb(241, 245, 249);
        dgv.RowHeadersVisible = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.MultiSelect = false;
        dgv.ReadOnly = true;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.AllowUserToResizeRows = false;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.EnableHeadersVisualStyles = false;
        dgv.RowTemplate.Height = 36;

        // Header style
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = PrimaryBurgundy;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.Font = BodyBoldFont;
        dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
        dgv.ColumnHeadersHeight = 40;

        // Rows style
        dgv.DefaultCellStyle.BackColor = Color.White;
        dgv.DefaultCellStyle.ForeColor = TextPrimary;
        dgv.DefaultCellStyle.Font = BodyRegularFont;
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 226, 226); // Light burgundy tint
        dgv.DefaultCellStyle.SelectionForeColor = DarkBurgundy;
        dgv.DefaultCellStyle.Padding = new Padding(6, 2, 6, 2);

        // Alternating row color
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
        dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 226, 226);
        dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = DarkBurgundy;
    }

    /// <summary>
    /// Creates a styled summary card with a title, primary metric, and colored accent bar.
    /// </summary>
    public static Panel CreateMetricCard(string title, string value, Color accentColor, out Label valueLabel)
    {
        var card = new Panel
        {
            BackColor = CardWhite,
            Padding = new Padding(14),
            Margin = new Padding(6),
            Dock = DockStyle.Fill
        };

        // Left accent indicator bar
        var accentBar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = accentColor
        };
        card.Controls.Add(accentBar);

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 4, 4, 4),
            BackColor = CardWhite
        };

        var lblTitle = new Label
        {
            Text = title.ToUpperInvariant(),
            Font = SmallMutedFont,
            ForeColor = TextSecondary,
            Dock = DockStyle.Top,
            Height = 20
        };

        valueLabel = new Label
        {
            Text = value,
            Font = MetricNumberFont,
            ForeColor = TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Correct z-order: DockStyle.Top docks first, DockStyle.Fill fills the remainder
        contentPanel.Controls.Add(lblTitle);
        contentPanel.Controls.Add(valueLabel);
        lblTitle.SendToBack();
        valueLabel.BringToFront();

        card.Controls.Add(contentPanel);
        accentBar.SendToBack();
        contentPanel.BringToFront();

        // Subtle border
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        return card;
    }

    /// <summary>
    /// Styles a panel as a modern card container with a crisp border and white background.
    /// </summary>
    public static void StyleCard(Panel panel)
    {
        panel.BackColor = CardWhite;
        panel.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };
    }
}
