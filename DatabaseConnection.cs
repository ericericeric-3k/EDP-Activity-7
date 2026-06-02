// ============================================================
//  DatabaseConnection.cs
//  PUBLIC class – shared across all forms
//  E-Commerce Information System
// ============================================================
using MySqlConnector;
using System.Windows.Forms;

namespace ECommSystem
{
    /// <summary>
    /// Public static class that centralizes all MySQL connection logic.
    /// Every form uses DatabaseConnection.GetConnection() to talk to the DB.
    /// </summary>
    public static class DatabaseConnection
    {
        // ── Connection settings ───────────────────────────────
        private const string Server   = "127.0.0.1";
        private const string Port     = "3306";
        private const string Database = "e-comm";
        private const string DbUser   = "root";
        private const string DbPass   = "!!PassworD1@^4$&4$&3$*5$^3%^4@^!!"; 

    
        public static string ConnectionString =>
            $"Server={Server};Port={Port};Database={Database};" +
            $"User={DbUser};Password={DbPass};AllowZeroDateTime=True;";

        // ── Factory method ────────────────────────────────────
        /// <summary>
        /// Returns a new (closed) MySqlConnection.
        /// Caller is responsible for opening and disposing it.
        /// </summary>
        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        // ── Health check ──────────────────────────────────────
        /// <summary>
        /// Opens a quick connection to verify the DB is reachable.
        /// Shows an error MessageBox and returns false on failure.
        /// </summary>
        public static bool TestConnection()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Cannot connect to the database.\n\n" +
                    $"Details: {ex.Message}\n\n" +
                    $"Please check that MySQL/MariaDB is running and\n" +
                    $"your connection settings in DatabaseConnection.cs are correct.",
                    "Database Connection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
