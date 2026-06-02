// ============================================================
//  Login.cs – User Authentication
//  Connects to MySQL, verifies BCrypt password hash
// ============================================================
using System;
using System.Drawing;
using System.Windows.Forms;
using MySqlConnector;
using BCrypt.Net;

namespace ECommSystem
{
    public class Login : Form
    {
        private Panel   pnlHeader, pnlFooter;
        private Label   lblTitle, lblSub, lblUser, lblPass, lblVersion;
        private TextBox txtUsername, txtPassword;
        private Button  btnLogin, btnExit, btnForgot;
        private CheckBox chkShow;

        public Login()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text            = "E-Commerce System – Login";
            this.Size            = new Size(420, 530);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox     = false;
            this.BackColor       = Color.FromArgb(245, 247, 250);
            this.Font            = new Font("Segoe UI", 9.5f);

            // ── Header ────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 140,
                BackColor = Color.FromArgb(30, 58, 95)
            };

            lblTitle = new Label
            {
                Text      = "E-COMMERCE SYSTEM",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(0, 30),
                Size      = new Size(420, 40)
            };

            lblSub = new Label
            {
                Text      = "User Authentication Portal",
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(180, 210, 240),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(0, 75),
                Size      = new Size(420, 24)
            };

            Label lblDb = new Label
            {
                Text      = $"Database: e-comm  |  {DatabaseConnection.ConnectionString.Split(';')[0]}",
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(130, 170, 210),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(0, 105),
                Size      = new Size(420, 20)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub, lblDb });

            // ── Fields ────────────────────────────────────────
            lblUser = MakeLabel("Username", 50, 160);
            txtUsername = new TextBox
            {
                Location        = new Point(50, 180),
                Size            = new Size(310, 28),
                PlaceholderText = "Enter username",
                Font            = new Font("Segoe UI", 10f)
            };

            lblPass = MakeLabel("Password", 50, 222);
            txtPassword = new TextBox
            {
                Location        = new Point(50, 242),
                Size            = new Size(310, 28),
                PasswordChar    = '●',
                PlaceholderText = "Enter password",
                Font            = new Font("Segoe UI", 10f)
            };
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) BtnLogin_Click(s, e);
            };

            chkShow = new CheckBox
            {
                Text      = "Show password",
                Location  = new Point(50, 278),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9f)
            };
            chkShow.CheckedChanged += (s, e) =>
                txtPassword.PasswordChar = chkShow.Checked ? '\0' : '●';

            btnForgot = new Button
            {
                Text      = "Forgot Password?",
                Location  = new Point(248, 276),
                AutoSize  = true,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(30, 100, 200),
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            btnForgot.FlatAppearance.BorderSize = 0;
            btnForgot.Click += (s, e) =>
            {
                var rec = new PasswordRecovery();
                rec.ShowDialog(this);
            };

            // ── Buttons ───────────────────────────────────────
            btnLogin = new Button
            {
                Text      = "Sign In",
                Location  = new Point(50, 320),
                Size      = new Size(310, 44),
                BackColor = Color.FromArgb(30, 58, 95),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            btnExit = new Button
            {
                Text      = "Exit Application",
                Location  = new Point(50, 375),
                Size      = new Size(310, 38),
                BackColor = Color.FromArgb(210, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += (s, e) => Application.Exit();

            // ── Footer ────────────────────────────────────────
            pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 38,
                BackColor = Color.FromArgb(220, 228, 238)
            };
            lblVersion = new Label
            {
                Text      = "E-Commerce IS v1.0  |  MariaDB 10.4",
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray,
                Font      = new Font("Segoe UI", 8.5f)
            };
            pnlFooter.Controls.Add(lblVersion);

            this.Controls.AddRange(new Control[]
            {
                pnlHeader,
                lblUser, txtUsername,
                lblPass, txtPassword,
                chkShow, btnForgot,
                btnLogin, btnExit,
                pnlFooter
            });
        }

        // ── Login Logic ───────────────────────────────────────
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text    = "Signing in...";

            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();

                string sql = "SELECT user_id, username, email, pass_hash, is_active, role " +
                             "FROM user WHERE username = @u LIMIT 1";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@u", username);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    MessageBox.Show("Username not found.", "Login Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int    userId   = reader.GetInt32("user_id");
                string email    = reader.GetString("email");
                string passHash = reader.GetString("pass_hash");
                bool   isActive = reader.GetInt32("is_active") == 1;
                string role     = reader.GetString("role");

                reader.Close();

                // BCrypt verify — replace $2y$ (PHP) with $2b$ (C# compatible)
                string normalizedHash = passHash.Replace("$2y$", "$2b$");
                bool   passwordOk     = BCrypt.Net.BCrypt.Verify(password, normalizedHash);

                if (!passwordOk)
                {
                    MessageBox.Show("Incorrect password. Please try again.",
                        "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.Clear();
                    return;
                }

                if (!isActive)
                {
                    MessageBox.Show("Your account has been deactivated.\nPlease contact the administrator.",
                        "Account Inactive", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ── Store session ─────────────────────────────
                SessionManager.UserId   = userId;
                SessionManager.Username = username;
                SessionManager.Email    = email;
                SessionManager.Role     = role;
                SessionManager.IsActive = isActive;

                this.Hide();
                var dash = new Dashboard();
                dash.FormClosed += (s2, e2) => this.Close();
                dash.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login error:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text    = "Sign In";
            }
        }

        private Label MakeLabel(string text, int x, int y) =>
            new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(60, 70, 80)
            };
    }
}
