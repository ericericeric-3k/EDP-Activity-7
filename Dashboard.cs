// ============================================================
//  Dashboard.cs – Main Hub
//  Displays live e-commerce stats from MySQL
// ============================================================
using System;
using System.Drawing;
using System.Windows.Forms;
using MySqlConnector;

namespace ECommSystem
{
    public class Dashboard : Form
    {
        private MenuStrip   menuStrip;
        private Panel       pnlHeader;
        private Label       lblWelcome, lblClock;
        private TableLayoutPanel tblMetrics;
        private DataGridView dgvOrders, dgvTopSellers;
        private System.Windows.Forms.Timer clockTimer;

        public Dashboard()
        {
            InitializeComponent();
            LoadMetrics();
            LoadRecentOrders();
            LoadTopSellers();
        }

        private void InitializeComponent()
        {
            this.Text            = "Dashboard – E-Commerce System";
            this.Size            = new Size(1050, 680);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.MinimumSize     = new Size(950, 620);
            this.BackColor       = Color.FromArgb(240, 244, 248);
            this.Font            = new Font("Segoe UI", 9.5f);

            // ── Menu ──────────────────────────────────────────
            menuStrip = new MenuStrip
            {
                BackColor = Color.FromArgb(30, 58, 95),
                Renderer  = new NavRenderer()
            };

            var mBrand = new ToolStripMenuItem("🛒  E-Commerce IS")
            { ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

            var mUsers = new ToolStripMenuItem("👥  User Management")
            { ForeColor = Color.White };
            mUsers.Click += (s, e) =>
            {
                new UserManagement().ShowDialog(this);
                LoadMetrics();
            };

            // ── Transactions menu ─────────────────────────────
            var mTrans = new ToolStripMenuItem("🔄  Transactions")
            { ForeColor = Color.White };

            var mSales = new ToolStripMenuItem("🛒  Sales Transaction")
            { ForeColor = Color.Black };
            mSales.Click += (s, e) =>
            {
                new SalesTransaction().ShowDialog(this);
                LoadMetrics();
                LoadRecentOrders();
                LoadTopSellers();
            };

            var mProducts = new ToolStripMenuItem("📦  Product Management")
            { ForeColor = Color.Black };
            mProducts.Click += (s, e) =>
            {
                new ProductManagement().ShowDialog(this);
                LoadMetrics();
                LoadTopSellers();
            };

            var mInventory = new ToolStripMenuItem("📋  Inventory Count")
            { ForeColor = Color.Black };
            mInventory.Click += (s, e) =>
            {
                new InventoryCount().ShowDialog(this);
                LoadMetrics();
            };

            mTrans.DropDownItems.AddRange(new ToolStripItem[] { mSales, mProducts, mInventory });

            // ── Reports menu ──────────────────────────────────
            var mReports = new ToolStripMenuItem("📊  Reports")
            { ForeColor = Color.White };
            mReports.Click += (s, e) =>
            {
                new Reports().ShowDialog(this);
            };

            var mLogout = new ToolStripMenuItem("🚪  Logout")
            { ForeColor = Color.White };
            mLogout.Click += (s, e) =>
            {
                SessionManager.Clear();
                this.Close();
            };

            menuStrip.Items.AddRange(new ToolStripItem[] { mBrand, mUsers, mTrans, mReports, mLogout });
            this.MainMenuStrip = menuStrip;

            // ── Header ────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 68,
                BackColor = Color.FromArgb(30, 58, 95)
            };

            lblWelcome = new Label
            {
                Text      = $"Welcome, {SessionManager.Username}  [{SessionManager.Role.ToUpper()}]",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(20, 14)
            };

            lblClock = new Label
            {
                Text      = DateTime.Now.ToString("dddd, MMMM d, yyyy  hh:mm:ss tt"),
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 210, 240),
                AutoSize  = true,
                Location  = new Point(20, 42)
            };

            pnlHeader.Controls.AddRange(new Control[] { lblWelcome, lblClock });

            // ── Metrics row ───────────────────────────────────
            tblMetrics = new TableLayoutPanel
            {
                Location    = new Point(16, 95),
                Size        = new Size(1008, 110),
                ColumnCount = 5,
                RowCount    = 1,
                BackColor   = Color.Transparent,
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            for (int i = 0; i < 5; i++)
                tblMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

            // Placeholders — filled by LoadMetrics()
            tblMetrics.Controls.Add(MetricCard("Total Users",    "...", Color.FromArgb(59,130,246)),  0, 0);
            tblMetrics.Controls.Add(MetricCard("Total Products", "...", Color.FromArgb(16,185,129)),  1, 0);
            tblMetrics.Controls.Add(MetricCard("Total Orders",   "...", Color.FromArgb(245,158,11)),  2, 0);
            tblMetrics.Controls.Add(MetricCard("Revenue",        "...", Color.FromArgb(139,92,246)),  3, 0);
            tblMetrics.Controls.Add(MetricCard("Pending Orders", "...", Color.FromArgb(239,68,68)),   4, 0);

            // ── Section labels ────────────────────────────────
            Label lblOrd = SectionLabel("Recent Orders  (vw_customer_order)", 16, 215);
            Label lblTop = SectionLabel("Top Sellers  (vw_top_sellers)", 545, 215);

            // ── Recent Orders grid ────────────────────────────
            dgvOrders = MakeGrid(new Point(16, 240), new Size(510, 370));
            dgvOrders.Columns.Add("username",     "Customer");
            dgvOrders.Columns.Add("order_id",     "Order ID");
            dgvOrders.Columns.Add("order_date",   "Date");
            dgvOrders.Columns.Add("total_amount", "Amount");
            dgvOrders.Columns.Add("status",       "Status");

            // ── Top Sellers grid ──────────────────────────────
            dgvTopSellers = MakeGrid(new Point(545, 240), new Size(505, 370));
            dgvTopSellers.Columns.Add("product_name", "Product");
            dgvTopSellers.Columns.Add("total_sold",   "Units Sold");

            // ── Clock ─────────────────────────────────────────
            clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) =>
                lblClock.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy  hh:mm:ss tt");
            clockTimer.Start();

            // ── Anchor grids on resize ────────────────────────
            dgvOrders.Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            dgvTopSellers.Anchor= AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;

            this.Controls.AddRange(new Control[]
            {
                menuStrip, pnlHeader,
                tblMetrics,
                lblOrd, dgvOrders,
                lblTop, dgvTopSellers
            });
        }

        // ── Data loaders ──────────────────────────────────────
        private void LoadMetrics()
        {
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();

                string[] queries =
                {
                    "SELECT COUNT(*) FROM user",
                    "SELECT COUNT(*) FROM product",
                    "SELECT COUNT(*) FROM `order`",
                    "SELECT IFNULL(SUM(total_amount),0) FROM `order` WHERE status != 'Cancelled'",
                    "SELECT COUNT(*) FROM `order` WHERE status = 'Pending'"
                };

                Color[] colors =
                {
                    Color.FromArgb(59,130,246),
                    Color.FromArgb(16,185,129),
                    Color.FromArgb(245,158,11),
                    Color.FromArgb(139,92,246),
                    Color.FromArgb(239,68,68)
                };

                string[] labels = { "Total Users","Total Products","Total Orders","Revenue (₱)","Pending Orders" };

                tblMetrics.Controls.Clear();
                for (int i = 0; i < queries.Length; i++)
                {
                    using var cmd = new MySqlCommand(queries[i], conn);
                    var val = cmd.ExecuteScalar();
                    string display = i == 3
                        ? $"₱ {Convert.ToDecimal(val):N2}"
                        : val?.ToString() ?? "0";
                    tblMetrics.Controls.Add(MetricCard(labels[i], display, colors[i]), i, 0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading metrics: " + ex.Message);
            }
        }

        private void LoadRecentOrders()
        {
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "SELECT username, order_id, order_date, total_amount, status " +
                    "FROM vw_customer_order ORDER BY order_id DESC LIMIT 20", conn);
                using var r = cmd.ExecuteReader();
                dgvOrders.Rows.Clear();
                while (r.Read())
                {
                    int idx = dgvOrders.Rows.Add(
                        r["username"],
                        r["order_id"],
                        Convert.ToDateTime(r["order_date"]).ToString("MM/dd/yyyy HH:mm"),
                        $"₱ {Convert.ToDecimal(r["total_amount"]):N2}",
                        r["status"]);

                    // Color-code status
                    string status = r["status"].ToString();
                    dgvOrders.Rows[idx].Cells["status"].Style.ForeColor = status switch
                    {
                        "Paid"      => Color.FromArgb(5, 150, 90),
                        "Shipped"   => Color.FromArgb(30, 100, 200),
                        "Pending"   => Color.FromArgb(180, 110, 0),
                        "Cancelled" => Color.FromArgb(185, 28, 28),
                        _           => Color.Black
                    };
                    dgvOrders.Rows[idx].Cells["status"].Style.Font =
                        new Font("Segoe UI", 9f, FontStyle.Bold);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading orders: " + ex.Message);
            }
        }

        private void LoadTopSellers()
        {
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "SELECT product_name, total_sold FROM vw_top_sellers", conn);
                using var r = cmd.ExecuteReader();
                dgvTopSellers.Rows.Clear();
                int rank = 1;
                while (r.Read())
                {
                    dgvTopSellers.Rows.Add(
                        $"{rank++}. {r["product_name"]}",
                        r["total_sold"]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading top sellers: " + ex.Message);
            }
        }

        // ── Helpers ───────────────────────────────────────────
        private Panel MetricCard(string label, string value, Color accent)
        {
            var p = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Margin    = new Padding(5)
            };
            var strip = new Panel { Width = 5, Dock = DockStyle.Left, BackColor = accent };
            var lbl   = new Label
            {
                Text      = label,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.Gray,
                AutoSize  = true,
                Location  = new Point(14, 12)
            };
            var val = new Label
            {
                Text      = value,
                Font      = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 30, 45),
                AutoSize  = true,
                Location  = new Point(14, 30)
            };
            p.Controls.AddRange(new Control[] { strip, lbl, val });
            return p;
        }

        private DataGridView MakeGrid(Point loc, Size sz)
        {
            var g = new DataGridView
            {
                Location              = loc,
                Size                  = sz,
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
                Font                  = new Font("Segoe UI", 9f)
            };
            g.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(30, 58, 95);
            g.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            g.EnableHeadersVisualStyles                = false;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 253);
            return g;
        }

        private Label SectionLabel(string text, int x, int y) =>
            new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 95)
            };

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            clockTimer.Stop();
            base.OnFormClosed(e);
        }
    }

    // ── Custom menu renderer ──────────────────────────────────
    class NavRenderer : ToolStripProfessionalRenderer
    {
        public NavRenderer() : base(new NavColors()) { }
    }
    class NavColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected       => Color.FromArgb(45, 80, 130);
        public override Color MenuItemBorder         => Color.Transparent;
        public override Color MenuBorder             => Color.Transparent;
        public override Color ToolStripGradientBegin => Color.FromArgb(30, 58, 95);
        public override Color ToolStripGradientEnd   => Color.FromArgb(30, 58, 95);
        public override Color ToolStripGradientMiddle=> Color.FromArgb(30, 58, 95);
    }
}
