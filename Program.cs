// ============================================================
//  Program.cs – Entry Point
//  E-Commerce Information System
// ============================================================
using System.Windows.Forms;

namespace ECommSystem
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Test DB before showing any form
            if (!DatabaseConnection.TestConnection()) return;

            Application.Run(new Login());
        }
    }
}
