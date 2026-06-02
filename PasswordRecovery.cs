// ============================================================
//  PasswordRecovery.cs – 3-Step Password Reset
//  Looks up user by email, verifies code, updates hash in DB
// ============================================================
using System;
using System.Drawing;
using System.Windows.Forms;
using MySqlConnector;

namespace ECommSystem
{
    public class PasswordRecovery : Form
    {
        private Panel   pnlHeader, pnlStep1, pnlStep2, pnlStep3, pnlSteps;
        private Label   lblTitle, lblStep1, lblStep2, lblStep3;
        private TextBox txtEmail, txtCode, txtNewPass, txtConfirm;
        private Button  btnSend, btnVerify, btnReset, btnBack, btnCancel;
        private int     currentStep = 1;
        private string  foundEmail  = "";
        private int     foundUserId = 0;
        private string  simCode     = "";

        public PasswordRecovery()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text            = "Password Recovery";
            this.Size            = new Size(440, 500);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.BackColor       = Color.FromArgb(245, 247, 250);
            this.Font            = new Font("Segoe UI", 9.5f);

            // ── Header ────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(30, 58, 95)
            };

            lblTitle = new Label
            {
                Text      = "Password Recovery",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(0, 20),
                Size      = new Size(440, 38)
            };

            Label lblSub = new Label
            {
                Text      = "Reset your account password in 3 steps",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 210, 240),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(0, 62),
                Size      = new Size(440, 20)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // ── Step indicators ───────────────────────────────
            pnlSteps = new Panel
            {
                Location  = new Point(0, 90),
                Size      = new Size(440, 44),
                BackColor = Color.FromArgb(218, 228, 242)
            };

            lblStep1 = StepLbl("① Verify Email",  14, 12, true);
            lblStep2 = StepLbl("② Enter Code",   160, 12, false);
            lblStep3 = StepLbl("③ New Password", 295, 12, false);
            pnlSteps.Controls.AddRange(new Control[] { lblStep1, lblStep2, lblStep3 });

            // ── STEP 1 ────────────────────────────────────────
            pnlStep1 = MakePanel();

            AddLabel(pnlStep1, "Enter your registered email address:", 0, 5);
            txtEmail = new TextBox
            {
                Location        = new Point(0, 30),
                Size            = new Size(348, 28),
                PlaceholderText = "yourname@email.com",
                Font            = new Font("Segoe UI", 10f)
            };
            btnSend = MakeBtn("Send Verification Code →", 0, 74,
                Color.FromArgb(30, 58, 95), Color.White, 348);
            btnSend.Click += BtnSend_Click;
            pnlStep1.Controls.AddRange(new Control[] { txtEmail, btnSend });

            // ── STEP 2 ────────────────────────────────────────
            pnlStep2 = MakePanel();
            pnlStep2.Visible = false;

            Label infoLbl = new Label
            {
                Text      = "A 6-digit verification code has been sent to your email.",
                AutoSize  = false,
                Size      = new Size(348, 36),
                Location  = new Point(0, 0),
                ForeColor = Color.FromArgb(5, 130, 90)
            };
            AddLabel(pnlStep2, "Verification Code:", 0, 48);
            txtCode = new TextBox
            {
                Location        = new Point(0, 70),
                Size            = new Size(348, 28),
                MaxLength       = 6,
                PlaceholderText = "6-digit code",
                Font            = new Font("Segoe UI", 12f)
            };
            btnVerify = MakeBtn("Verify Code →", 0, 118,
                Color.FromArgb(30, 58, 95), Color.White, 348);
            btnVerify.Click += BtnVerify_Click;
            pnlStep2.Controls.AddRange(new Control[] { infoLbl, txtCode, btnVerify });

            // ── STEP 3 ────────────────────────────────────────
            pnlStep3 = MakePanel();
            pnlStep3.Visible = false;

            AddLabel(pnlStep3, "New Password (min 8 characters):", 0, 5);
            txtNewPass = new TextBox
            {
                Location        = new Point(0, 28),
                Size            = new Size(348, 28),
                PasswordChar    = '●',
                Font            = new Font("Segoe UI", 10f)
            };
            AddLabel(pnlStep3, "Confirm New Password:", 0, 70);
            txtConfirm = new TextBox
            {
                Location        = new Point(0, 93),
                Size            = new Size(348, 28),
                PasswordChar    = '●',
                Font            = new Font("Segoe UI", 10f)
            };
            btnReset = MakeBtn("Reset Password ✓", 0, 140,
                Color.FromArgb(13, 148, 100), Color.White, 348);
            btnReset.Click += BtnReset_Click;
            pnlStep3.Controls.AddRange(new Control[]
            { txtNewPass, txtConfirm, btnReset });

            // ── Back / Cancel ─────────────────────────────────
            btnBack = MakeBtn("← Back", 46, 400,
                Color.FromArgb(190, 200, 215), Color.FromArgb(40, 40, 40), 100);
            btnBack.Click += BtnBack_Click;

            btnCancel = MakeBtn("Cancel", 290, 400,
                Color.FromArgb(210, 50, 50), Color.White, 100);
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[]
            {
                pnlHeader, pnlSteps,
                pnlStep1, pnlStep2, pnlStep3,
                btnBack, btnCancel
            });
        }

        // ── Handlers ──────────────────────────────────────────
        private void BtnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Please enter your email address.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "SELECT user_id, email FROM user WHERE email = @em AND is_active = 1 LIMIT 1",
                    conn);
                cmd.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                using var r = cmd.ExecuteReader();

                if (!r.Read())
                {
                    MessageBox.Show("No active account found with that email.",
                        "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                foundUserId = r.GetInt32("user_id");
                foundEmail  = r.GetString("email");
            }
            catch (Exception ex)
            {
                MessageBox.Show("DB Error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Simulate sending a code (in production, use SMTP)
            simCode = new Random().Next(100000, 999999).ToString();
            MessageBox.Show(
                $"Verification code sent to: {foundEmail}\n\n" +
                $"[DEMO MODE] Your code is: {simCode}",
                "Code Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);

            GoToStep(2);
        }

        private void BtnVerify_Click(object sender, EventArgs e)
        {
            if (txtCode.Text.Trim() != simCode)
            {
                MessageBox.Show("Incorrect verification code. Please try again.",
                    "Wrong Code", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GoToStep(3);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (txtNewPass.Text.Length < 8)
            {
                MessageBox.Show("Password must be at least 8 characters.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (txtNewPass.Text != txtConfirm.Text)
            {
                MessageBox.Show("Passwords do not match.", "Mismatch",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Hash using BCrypt ($2b$ prefix — C# compatible)
                string newHash = BCrypt.Net.BCrypt.HashPassword(txtNewPass.Text, workFactor: 10);

                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "UPDATE user SET pass_hash = @h WHERE user_id = @id", conn);
                cmd.Parameters.AddWithValue("@h",  newHash);
                cmd.Parameters.AddWithValue("@id", foundUserId);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Password reset successfully!\nYou may now log in.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating password:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (currentStep > 1) GoToStep(currentStep - 1);
        }

        // ── Step navigation ───────────────────────────────────
        private void GoToStep(int step)
        {
            currentStep = step;
            pnlStep1.Visible = (step == 1);
            pnlStep2.Visible = (step == 2);
            pnlStep3.Visible = (step == 3);

            Color active   = Color.FromArgb(30, 58, 95);
            Color inactive = Color.FromArgb(150, 160, 180);
            FontStyle bold = FontStyle.Bold;
            FontStyle norm = FontStyle.Regular;

            lblStep1.ForeColor = step == 1 ? active : inactive;
            lblStep2.ForeColor = step == 2 ? active : inactive;
            lblStep3.ForeColor = step == 3 ? active : inactive;
            lblStep1.Font = new Font("Segoe UI", 9f, step == 1 ? bold : norm);
            lblStep2.Font = new Font("Segoe UI", 9f, step == 2 ? bold : norm);
            lblStep3.Font = new Font("Segoe UI", 9f, step == 3 ? bold : norm);
        }

        // ── Helpers ───────────────────────────────────────────
        private Panel MakePanel() =>
            new Panel { Location = new Point(46, 148), Size = new Size(348, 240) };

        private Label StepLbl(string t, int x, int y, bool active) =>
            new Label
            {
                Text      = t,
                Location  = new Point(x, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9f,
                                active ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = active
                            ? Color.FromArgb(30, 58, 95)
                            : Color.FromArgb(150, 160, 180)
            };

        private void AddLabel(Panel p, string text, int x, int y) =>
            p.Controls.Add(new Label
            {
                Text     = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font     = new Font("Segoe UI", 9f)
            });

        private Button MakeBtn(string t, int x, int y, Color bg, Color fg, int w)
        {
            var b = new Button
            {
                Text      = t,
                Location  = new Point(x, y),
                Size      = new Size(w, 36),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
