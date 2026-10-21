using BloodBankManagementSystem.Data;
using BloodBankManagementSystem.Forms;

namespace BloodBankManagementSystem;

/// <summary>
/// Application entry point.
/// Initializes the SQLite database schema and seeds before launching the main window.
/// </summary>
static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            // Automatically initialize SQLite database schema, foreign keys, and seed blood groups
            DatabaseHelper.InitializeDatabase();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize the local SQLite database:\n\n{ex.Message}\n\nPlease verify write permissions in the application directory.",
                "Database Initialization Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new MainForm());
    }
}