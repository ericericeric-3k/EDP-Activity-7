// ============================================================
//  UserManagement.cs
//  Add Account | Update Profile | Activate | Deactivate | Search
// ============================================================
using System;
using System.Drawing;
using System.Windows.Forms;
using MySqlConnector;

namespace ECommSystem
{
    public class UserManagement : Form
    {
        // ── Left panel – Account List ─────────────────────────
        private Panel       pnlHeader, pnlLeft, pnlRight;
        private Label       lblTitle;
        private TextBox     txtSearch;
        private Button      btnSearch, btnRefresh;
        private DataGridView dgvUsers;
        private Label       lblCount;

        // ── Right panel – Detail / Form ───────────────────────
        private Label   lblPanelTitle;
        private Label   lblUsr, lblEmail, lblPass, lblRole, lblStatus;
        private TextBox txtUsr, txtEmail, txtPass;
        private ComboBox cboRole;
        private CheckBox chkActive;
        private Button  btnAdd, btnSave, btnActivate, btnDeactivate,
                        btnClearForm, btnClose;

        private int selectedUserId = -1;
        private bool isEditMode    = false;

        public UserManagement()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void InitializeComponent()
        {
            this.Text            = "User Management";
            this.Size            = new Size(1020, 660);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.MinimumSize     = new Size(900, 580);
            this.BackColor       = Color.FromArgb(240, 244, 248);
            this.Font            = new Font("Segoe UI", 9.5f);

            // ── Header ────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 62,
                BackColor = Color.FromArgb(30, 58, 95)
            };
            lblTitle = new Label
            {
                Text      = "👥  User Management",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(18, 10)
            };
            Label lblSub = new Label
            {
                Text      = "Add · Update · Activate · Deactivate · Search",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 210, 240),
                AutoSize  = true,
                Location  = new Point(20, 38)
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // ════════════════════════════════════════════════
            // LEFT PANEL – User List
            // ════════════════════════════════════════════════
            pnlLeft = new Panel
            {
                Location  = new Point(12, 74),
                Size      = new Size(585, 552),
                BackColor = Color.White,
                Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            Label lblListTitle = SLabel("Account List", 14, 12,
                new Font("Segoe UI", 11f, FontStyle.Bold),
                Color.FromArgb(30, 58, 95));

            // Search bar
            txtSearch = new TextBox
            {
                Location        = new Point(14, 44),
                Size            = new Size(370, 28),
                PlaceholderText = "Search by username or email…",
                Font            = new Font("Segoe UI", 9.5f)
            };
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) LoadUsers(txtSearch.Text.Trim());
            };

            btnSearch = Btn("🔍 Search", 392, 44, 80,
                Color.FromArgb(30, 58, 95), Color.White);
            btnSearch.Click += (s, e) => LoadUsers(txtSearch.Text.Trim());

            btnRefresh = Btn("↺", 480, 44, 40,
                Color.FromArgb(200, 210, 225), Color.FromArgb(40, 40, 40));
            btnRefresh.Click += (s, e) =>
            {
                txtSearch.Clear();
                LoadUsers();
            };

            // Grid
            dgvUsers = new DataGridView
            {
                Location              = new Point(14, 80),
                Size                  = new Size(556, 420),
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                ColumnHeadersHeight   = 34,
                RowHeadersVisible     = false,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                ReadOnly              = true,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                MultiSelect           = false,
                Font                  = new Font("Segoe UI", 9f),
                Anchor                = AnchorStyles.Top | AnchorStyles.Bottom
                                      | AnchorStyles.Left | AnchorStyles.Right
            };
            dgvUsers.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(30, 58, 95);
            dgvUsers.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
            dgvUsers.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvUsers.EnableHeadersVisualStyles                = false;
            dgvUsers.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 253);

            dgvUsers.Columns.Add("user_id",    "ID");
            dgvUsers.Columns.Add("username",   "Username");
            dgvUsers.Columns.Add("email",      "Email");
            dgvUsers.Columns.Add("role",       "Role");
            dgvUsers.Columns.Add("is_active",  "Status");
            dgvUsers.Columns.Add("created_at", "Created");

            dgvUsers.Columns["user_id"].FillWeight   = 40;
            dgvUsers.Columns["username"].FillWeight  = 90;
            dgvUsers.Columns["email"].FillWeight     = 140;
            dgvUsers.Columns["role"].FillWeight      = 55;
            dgvUsers.Columns["is_active"].FillWeight = 60;
            dgvUsers.Columns["created_at"].FillWeight= 90;

            dgvUsers.SelectionChanged += DgvUsers_SelectionChanged;

            lblCount = new Label
            {
                Text      = "0 record(s) found",
                Location  = new Point(14, 507),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            // Action buttons below grid
            Button btnActRow = Btn("✅ Activate",   14, 528, 110,
                Color.FromArgb(13, 148, 100), Color.White);
            btnActRow.Click += BtnActivate_Click;

            Button btnDeactRow = Btn("⛔ Deactivate", 134, 528, 115,
                Color.FromArgb(210, 50, 50), Color.White);
            btnDeactRow.Click += BtnDeactivate_Click;

            Button btnEditRow = Btn("✏ Edit", 259, 528, 80,
                Color.FromArgb(245, 158, 11), Color.White);
            btnEditRow.Click += BtnEditRow_Click;

            pnlLeft.Controls.AddRange(new Control[]
            {
                lblListTitle, txtSearch, btnSearch, btnRefresh,
                dgvUsers, lblCount,
                btnActRow, btnDeactRow, btnEditRow
            });

            // ════════════════════════════════════════════════
            // RIGHT PANEL – Add / Edit Form
            // ════════════════════════════════════════════════
            pnlRight = new Panel
            {
                Location  = new Point(608, 74),
                Size      = new Size(390, 552),
                BackColor = Color.White,
                Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right
            };

            lblPanelTitle = SLabel("Add New Account", 14, 12,
                new Font("Segoe UI", 11f, FontStyle.Bold),
                Color.FromArgb(30, 58, 95));

            // Divider line
            Panel divider = new Panel
            {
                Location  = new Point(14, 38),
                Size      = new Size(358, 1),
                BackColor = Color.FromArgb(220, 228, 240)
            };

            // Username
            lblUsr = FL("Username *", 14, 50);
            txtUsr = FT(14, 70, "Enter username");

            // Email
            lblEmail = FL("Email Address *", 14, 108);
            txtEmail = FT(14, 128, "Enter email");

            // Password
            lblPass = FL("Password (leave blank to keep current)", 14, 166);
            txtPass = new TextBox
            {
                Location        = new Point(14, 186),
                Size            = new Size(358, 28),
                PasswordChar    = '●',
                PlaceholderText = "Enter password",
                Font            = new Font("Segoe UI", 9.5f)
            };

            // Role
            lblRole = FL("Role *", 14, 228);
            cboRole = new ComboBox
            {
                Location      = new Point(14, 248),
                Size          = new Size(170, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 9.5f)
            };
            cboRole.Items.AddRange(new object[] { "user", "admin" });
            cboRole.SelectedIndex = 0;

            // Active status
            lblStatus = FL("Account Status", 200, 228);
            chkActive = new CheckBox
            {
                Text     = "Active Account",
                Location = new Point(200, 252),
                AutoSize = true,
                Checked  = true,
                Font     = new Font("Segoe UI", 9.5f)
            };

            // Separator
            Panel div2 = new Panel
            {
                Location  = new Point(14, 292),
                Size      = new Size(358, 1),
                BackColor = Color.FromArgb(220, 228, 240)
            };

            // Buttons
            btnAdd = Btn("➕ Add Account", 14, 306, 170,
                Color.FromArgb(30, 58, 95), Color.White);
            btnAdd.Font   = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnAdd.Click += BtnAdd_Click;

            btnSave = Btn("💾 Save Changes", 14, 306, 170,
                Color.FromArgb(13, 148, 100), Color.White);
            btnSave.Font    = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnSave.Visible = false;
            btnSave.Click  += BtnSave_Click;

            btnClearForm = Btn("✖ Cancel Edit", 194, 306, 170,
                Color.FromArgb(180, 190, 205), Color.FromArgb(40, 40, 40));
            btnClearForm.Click += BtnClearForm_Click;

            // Separator
            Panel div3 = new Panel
            {
                Location  = new Point(14, 358),
                Size      = new Size(358, 1),
                BackColor = Color.FromArgb(220, 228, 240)
            };

            btnActivate = Btn("✅ Activate Selected User", 14, 372, 358,
                Color.FromArgb(13, 148, 100), Color.White);
            btnActivate.Click += BtnActivate_Click;

            btnDeactivate = Btn("⛔ Deactivate Selected User", 14, 418, 358,
                Color.FromArgb(210, 50, 50), Color.White);
            btnDeactivate.Click += BtnDeactivate_Click;

            btnClose = Btn("Close", 14, 504, 358,
                Color.FromArgb(100, 110, 125), Color.White);
            btnClose.Click += (s, e) => this.Close();

            pnlRight.Controls.AddRange(new Control[]
            {
                lblPanelTitle, divider,
                lblUsr,   txtUsr,
                lblEmail, txtEmail,
                lblPass,  txtPass,
                lblRole,  cboRole,
                lblStatus, chkActive,
                div2,
                btnAdd, btnSave, btnClearForm,
                div3,
                btnActivate, btnDeactivate,
                btnClose
            });

            this.Controls.AddRange(new Control[]
            { pnlHeader, pnlLeft, pnlRight });
        }

        // ════════════════════════════════════════════════════
        // DATA OPERATIONS
        // ════════════════════════════════════════════════════

        private void LoadUsers(string search = "")
        {
            dgvUsers.Rows.Clear();
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();

                string sql = "SELECT user_id, username, email, role, is_active, created_at " +
                             "FROM user WHERE 1=1 ";
                if (!string.IsNullOrEmpty(search))
                    sql += "AND (username LIKE @s OR email LIKE @s) ";
                sql += "ORDER BY user_id DESC";

                using var cmd = new MySqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(search))
                    cmd.Parameters.AddWithValue("@s", $"%{search}%");

                using var r = cmd.ExecuteReader();
                int count = 0;
                while (r.Read())
                {
                    bool active = r.GetInt32("is_active") == 1;
                    int  idx    = dgvUsers.Rows.Add(
                        r["user_id"],
                        r["username"],
                        r["email"],
                        r["role"],
                        active ? "Active" : "Inactive",
                        Convert.ToDateTime(r["created_at"]).ToString("MM/dd/yyyy")
                    );

                    dgvUsers.Rows[idx].Cells["is_active"].Style.ForeColor =
                        active ? Color.FromArgb(5, 150, 90) : Color.FromArgb(185, 28, 28);
                    dgvUsers.Rows[idx].Cells["is_active"].Style.Font =
                        new Font("Segoe UI", 9f, FontStyle.Bold);
                    count++;
                }
                lblCount.Text = $"{count} record(s) found";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading users:\n" + ex.Message);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateForm(requirePassword: true)) return;

            try
            {
                string hash = BCrypt.Net.BCrypt.HashPassword(txtPass.Text, 10);

                using var conn = DatabaseConnection.GetConnection();
                conn.Open();

                // Check duplicate username/email
                using var chk = new MySqlCommand(
                    "SELECT COUNT(*) FROM user WHERE username=@u OR email=@em", conn);
                chk.Parameters.AddWithValue("@u",  txtUsr.Text.Trim());
                chk.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                if (Convert.ToInt32(chk.ExecuteScalar()) > 0)
                {
                    MessageBox.Show("Username or email already exists.",
                        "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var cmd = new MySqlCommand(
                    "INSERT INTO user (username, email, pass_hash, role, is_active) " +
                    "VALUES (@u, @em, @h, @r, @a)", conn);
                cmd.Parameters.AddWithValue("@u",  txtUsr.Text.Trim());
                cmd.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                cmd.Parameters.AddWithValue("@h",  hash);
                cmd.Parameters.AddWithValue("@r",  cboRole.SelectedItem?.ToString() ?? "user");
                cmd.Parameters.AddWithValue("@a",  chkActive.Checked ? 1 : 0);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Account created successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding account:\n" + ex.Message);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (selectedUserId < 0) return;
            if (!ValidateForm(requirePassword: false)) return;

            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();

                string sql = "UPDATE user SET username=@u, email=@em, role=@r, is_active=@a ";
                if (!string.IsNullOrEmpty(txtPass.Text))
                {
                    string hash = BCrypt.Net.BCrypt.HashPassword(txtPass.Text, 10);
                    sql += ", pass_hash=@h ";
                }
                sql += "WHERE user_id=@id";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@u",  txtUsr.Text.Trim());
                cmd.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                cmd.Parameters.AddWithValue("@r",  cboRole.SelectedItem?.ToString() ?? "user");
                cmd.Parameters.AddWithValue("@a",  chkActive.Checked ? 1 : 0);
                if (!string.IsNullOrEmpty(txtPass.Text))
                    cmd.Parameters.AddWithValue("@h",
                        BCrypt.Net.BCrypt.HashPassword(txtPass.Text, 10));
                cmd.Parameters.AddWithValue("@id", selectedUserId);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Account updated successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating account:\n" + ex.Message);
            }
        }

        private void BtnActivate_Click(object sender, EventArgs e)
            => SetActiveStatus(1);

        private void BtnDeactivate_Click(object sender, EventArgs e)
            => SetActiveStatus(0);

        private void SetActiveStatus(int status)
        {
            if (selectedUserId < 0)
            {
                MessageBox.Show("Please select a user first.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string action = status == 1 ? "activate" : "deactivate";
            var confirm = MessageBox.Show(
                $"Are you sure you want to {action} this account?",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "UPDATE user SET is_active=@a WHERE user_id=@id", conn);
                cmd.Parameters.AddWithValue("@a",  status);
                cmd.Parameters.AddWithValue("@id", selectedUserId);
                cmd.ExecuteNonQuery();

                MessageBox.Show($"Account {action}d successfully!", "Done",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        // ── Grid selection → populate right panel ─────────────
        private void DgvUsers_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0) return;
            var row = dgvUsers.SelectedRows[0];
            selectedUserId     = Convert.ToInt32(row.Cells["user_id"].Value);
        }

        private void BtnEditRow_Click(object sender, EventArgs e)
        {
            if (selectedUserId < 0)
            {
                MessageBox.Show("Please select a user to edit.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "SELECT username, email, role, is_active FROM user WHERE user_id=@id", conn);
                cmd.Parameters.AddWithValue("@id", selectedUserId);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return;

                txtUsr.Text         = r.GetString("username");
                txtEmail.Text       = r.GetString("email");
                txtPass.Text        = "";
                cboRole.SelectedItem= r.GetString("role");
                chkActive.Checked   = r.GetInt32("is_active") == 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading user: " + ex.Message);
                return;
            }

            isEditMode             = true;
            lblPanelTitle.Text     = $"Edit Account  [ID: {selectedUserId}]";
            lblPanelTitle.ForeColor= Color.FromArgb(13, 148, 100);
            btnAdd.Visible         = false;
            btnSave.Visible        = true;
            lblPass.Text           = "Password (leave blank to keep current)";
        }

        private void BtnClearForm_Click(object sender, EventArgs e) => ClearForm();

        private void ClearForm()
        {
            selectedUserId         = -1;
            isEditMode             = false;
            txtUsr.Clear();
            txtEmail.Clear();
            txtPass.Clear();
            cboRole.SelectedIndex  = 0;
            chkActive.Checked      = true;
            lblPanelTitle.Text     = "Add New Account";
            lblPanelTitle.ForeColor= Color.FromArgb(30, 58, 95);
            btnAdd.Visible         = true;
            btnSave.Visible        = false;
            lblPass.Text           = "Password *";
        }

        // ── Validation ────────────────────────────────────────
        private bool ValidateForm(bool requirePassword)
        {
            if (string.IsNullOrWhiteSpace(txtUsr.Text))
            {
                MessageBox.Show("Username is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsr.Focus(); return false;
            }
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains("@"))
            {
                MessageBox.Show("A valid email address is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus(); return false;
            }
            if (requirePassword && txtPass.Text.Length < 8)
            {
                MessageBox.Show("Password must be at least 8 characters.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPass.Focus(); return false;
            }
            return true;
        }

        // ── UI Factories ──────────────────────────────────────
        private Label SLabel(string t, int x, int y, Font f, Color c) =>
            new Label { Text = t, Location = new Point(x, y), AutoSize = true, Font = f, ForeColor = c };

        private Label FL(string t, int x, int y) =>
            new Label
            {
                Text      = t,
                Location  = new Point(x, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(60, 70, 80)
            };

        private TextBox FT(int x, int y, string ph) =>
            new TextBox
            {
                Location        = new Point(x, y),
                Size            = new Size(358, 28),
                PlaceholderText = ph,
                Font            = new Font("Segoe UI", 9.5f)
            };

        private Button Btn(string t, int x, int y, int w, Color bg, Color fg)
        {
            var b = new Button
            {
                Text      = t,
                Location  = new Point(x, y),
                Size      = new Size(w, 34),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
