// ============================================================
//  InventoryCount.cs
//  Transaction 3: Physical inventory count – adjust stock
//  View current stock, record counted qty, apply adjustments
// ============================================================
using System;
using System.Drawing;
using System.Windows.Forms;
using MySqlConnector;

namespace ECommSystem
{
    public class InventoryCount : Form
    {
        private Panel pnlHeader;
        private Panel pnlLeft, pnlRight;

        // Left: inventory grid
        private DataGridView dgvInventory;
        private TextBox txtSearch;
        private Button  btnSearch, btnRefresh;
        private Label   lblCount;
        private ComboBox cboCatFilter;
        private Button  btnCatFilter;

        // Right: adjustment form
        private Label lblPanelTitle;
        private Label lblSelectedProduct, lblCurrentStock, lblCountedLbl, lblReasonLbl, lblNoteLbl;
        private Label lblCurrentVal;
        private NumericUpDown nudCounted;
        private TextBox txtNote;
        private ComboBox cboReason;
        private Button btnApply, btnClearAdj;
        private DataGridView dgvLog;
        private Label lblLogTitle;

        private int    selectedProductId   = -1;
        private string selectedProductName = "";
        private int    currentStock        = 0;

        public InventoryCount()
        {
            InitializeComponent();
            LoadCategoryFilter();
            LoadInventory();
            LoadAdjustmentLog();
        }

        private void InitializeComponent()
        {
            this.Text          = "Inventory Count";
            this.Size          = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize   = new Size(1050, 620);
            this.BackColor     = Color.FromArgb(240,244,248);
            this.Font          = new Font("Segoe UI", 9.5f);

            // Header
            pnlHeader = new Panel { Dock=DockStyle.Top, Height=62, BackColor=Color.FromArgb(30,58,95) };
            pnlHeader.Controls.AddRange(new Control[] {
                new Label { Text="📋  Inventory Count", Font=new Font("Segoe UI",14f,FontStyle.Bold),
                    ForeColor=Color.White, AutoSize=true, Location=new Point(18,10) },
                new Label { Text="View Stock · Record Physical Count · Apply Adjustments",
                    Font=new Font("Segoe UI",9f), ForeColor=Color.FromArgb(180,210,240), AutoSize=true, Location=new Point(20,38) }
            });

            // ── LEFT ─────────────────────────────────────────
            pnlLeft = new Panel { Location=new Point(12,74), Size=new Size(680,590),
                BackColor=Color.White, Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left };

            pnlLeft.Controls.Add(SL("Current Stock Levels", 14, 12, new Font("Segoe UI",11f,FontStyle.Bold), Color.FromArgb(30,58,95)));

            // Search + filter row
            txtSearch = new TextBox { Location=new Point(14,44), Size=new Size(250,28),
                PlaceholderText="Search product…", Font=new Font("Segoe UI",9.5f) };
            txtSearch.KeyDown += (s,e) => { if (e.KeyCode==Keys.Enter) LoadInventory(txtSearch.Text.Trim()); };

            cboCatFilter = new ComboBox { Location=new Point(270,44), Size=new Size(160,28),
                DropDownStyle=ComboBoxStyle.DropDownList, Font=new Font("Segoe UI",9.5f) };

            btnCatFilter = Btn("🔍 Filter", 440, 44, 80, Color.FromArgb(30,58,95), Color.White);
            btnCatFilter.Click += (s,e) => ApplyFilter();

            btnRefresh = Btn("↺", 528, 44, 40, Color.FromArgb(200,210,225), Color.FromArgb(40,40,40));
            btnRefresh.Click += (s,e) => { txtSearch.Clear(); cboCatFilter.SelectedIndex=0; LoadInventory(); };

            btnSearch = Btn("Search", 576, 44, 70, Color.FromArgb(59,130,246), Color.White);
            btnSearch.Click += (s,e) => LoadInventory(txtSearch.Text.Trim());

            // Inventory grid
            dgvInventory = MakeGrid(new Point(14,80), new Size(650,460));
            dgvInventory.Columns.Add("product_id",   "ID");
            dgvInventory.Columns.Add("product_name", "Product Name");
            dgvInventory.Columns.Add("cat_name",     "Category");
            dgvInventory.Columns.Add("price",        "Price (₱)");
            dgvInventory.Columns.Add("stock",        "Current Stock");
            dgvInventory.Columns.Add("stock_status", "Stock Status");
            dgvInventory.Columns["product_id"].FillWeight   = 35;
            dgvInventory.Columns["product_name"].FillWeight = 160;
            dgvInventory.Columns["cat_name"].FillWeight     = 90;
            dgvInventory.Columns["price"].FillWeight        = 80;
            dgvInventory.Columns["stock"].FillWeight        = 70;
            dgvInventory.Columns["stock_status"].FillWeight = 80;
            dgvInventory.SelectionChanged += DgvInventory_SelectionChanged;

            lblCount = new Label { Text="0 record(s)", Location=new Point(14,547), AutoSize=true,
                Font=new Font("Segoe UI",8.5f,FontStyle.Italic), ForeColor=Color.Gray };

            var lblLegend = new Label { Text="🟢 = Adequate   🟡 = Low (≤10)   🔴 = Out of Stock",
                Location=new Point(14,563), AutoSize=true, Font=new Font("Segoe UI",8.5f), ForeColor=Color.FromArgb(90,100,115) };

            pnlLeft.Controls.AddRange(new Control[] {
                txtSearch, cboCatFilter, btnCatFilter, btnRefresh, btnSearch,
                dgvInventory, lblCount, lblLegend
            });

            // ── RIGHT ────────────────────────────────────────
            pnlRight = new Panel { Location=new Point(704,74), Size=new Size(470,590),
                BackColor=Color.White, Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Right };

            lblPanelTitle = SL("Inventory Adjustment", 14, 12, new Font("Segoe UI",11f,FontStyle.Bold), Color.FromArgb(30,58,95));
            pnlRight.Controls.AddRange(new Control[] {
                lblPanelTitle,
                new Panel { Location=new Point(14,38), Size=new Size(440,1), BackColor=Color.FromArgb(220,228,240) }
            });

            // Selected product info
            lblSelectedProduct = SL("← Select a product from the list", 14, 48, new Font("Segoe UI",9.5f,FontStyle.Italic), Color.Gray);
            lblCurrentStock    = FL("Current System Stock:", 14, 82);
            lblCurrentVal      = SL("—", 14, 100, new Font("Segoe UI",14f,FontStyle.Bold), Color.FromArgb(30,58,95));

            var div1 = new Panel { Location=new Point(14,128), Size=new Size(440,1), BackColor=Color.FromArgb(220,228,240) };

            lblCountedLbl = FL("Physically Counted Quantity *", 14, 138);
            nudCounted    = new NumericUpDown { Location=new Point(14,158), Size=new Size(140,28),
                Minimum=0, Maximum=999999, Value=0, Font=new Font("Segoe UI",9.5f) };

            lblReasonLbl = FL("Adjustment Reason *", 170, 138);
            cboReason    = new ComboBox { Location=new Point(170,158), Size=new Size(280,28),
                DropDownStyle=ComboBoxStyle.DropDownList, Font=new Font("Segoe UI",9.5f) };
            cboReason.Items.AddRange(new object[] {
                "Physical Count Correction",
                "Damaged / Expired Goods",
                "Stock Received",
                "Returns from Customer",
                "Internal Transfer",
                "Other"
            });
            cboReason.SelectedIndex = 0;

            lblNoteLbl = FL("Notes / Remarks (optional)", 14, 198);
            txtNote    = new TextBox { Location=new Point(14,218), Size=new Size(440,60),
                Multiline=true, Font=new Font("Segoe UI",9.5f), PlaceholderText="Add any remarks about this count…" };

            var div2 = new Panel { Location=new Point(14,292), Size=new Size(440,1), BackColor=Color.FromArgb(220,228,240) };

            // Diff preview label
            var lblDiffTitle = FL("Adjustment Preview:", 14, 302);
            var lblDiff      = new Label { Name="lblDiff", Text="Select a product and enter counted qty.",
                Location=new Point(14,320), AutoSize=true, Font=new Font("Segoe UI",9.5f), ForeColor=Color.Gray };
            nudCounted.ValueChanged += (s,e) => UpdateDiffPreview(lblDiff);

            btnApply   = Btn("✅ Apply Adjustment", 14, 348, 210, Color.FromArgb(13,148,100), Color.White);
            btnApply.Font   = new Font("Segoe UI",10f,FontStyle.Bold);
            btnApply.Click += (s,e) => ApplyAdjustment(lblDiff);

            btnClearAdj = Btn("✖ Clear", 234, 348, 100, Color.FromArgb(180,190,205), Color.FromArgb(40,40,40));
            btnClearAdj.Click += (s,e) => { nudCounted.Value=0; txtNote.Clear(); cboReason.SelectedIndex=0; };

            // Adjustment log
            var div3 = new Panel { Location=new Point(14,396), Size=new Size(440,1), BackColor=Color.FromArgb(220,228,240) };
            lblLogTitle = SL("Recent Adjustments", 14, 406, new Font("Segoe UI",10f,FontStyle.Bold), Color.FromArgb(30,58,95));

            dgvLog = MakeGrid(new Point(14,428), new Size(440,120));
            dgvLog.Columns.Add("adj_date",    "Date");
            dgvLog.Columns.Add("product",     "Product");
            dgvLog.Columns.Add("old_stock",   "Old");
            dgvLog.Columns.Add("new_stock",   "New");
            dgvLog.Columns.Add("diff",        "Diff");
            dgvLog.Columns.Add("reason",      "Reason");
            dgvLog.Columns["adj_date"].FillWeight  = 100;
            dgvLog.Columns["product"].FillWeight   = 120;
            dgvLog.Columns["old_stock"].FillWeight = 40;
            dgvLog.Columns["new_stock"].FillWeight = 40;
            dgvLog.Columns["diff"].FillWeight      = 40;
            dgvLog.Columns["reason"].FillWeight    = 120;

            var btnClose = Btn("Close", 14, 558, 440, Color.FromArgb(100,110,125), Color.White);
            btnClose.Click += (s,e) => this.Close();

            pnlRight.Controls.AddRange(new Control[] {
                lblSelectedProduct, lblCurrentStock, lblCurrentVal,
                div1,
                lblCountedLbl, nudCounted,
                lblReasonLbl, cboReason,
                lblNoteLbl, txtNote,
                div2, lblDiffTitle, lblDiff,
                btnApply, btnClearAdj,
                div3, lblLogTitle, dgvLog,
                btnClose
            });

            this.Controls.AddRange(new Control[] { pnlHeader, pnlLeft, pnlRight });
        }

        // ── Data ─────────────────────────────────────────────
        private void LoadCategoryFilter()
        {
            cboCatFilter.Items.Clear();
            cboCatFilter.Items.Add(new ComboItem(0, "All Categories"));
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT category_id, cat_name FROM category ORDER BY cat_name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    cboCatFilter.Items.Add(new ComboItem(r.GetInt32(0), r.GetString(1)));
            }
            catch { }
            cboCatFilter.SelectedIndex = 0;
        }

        private void ApplyFilter()
        {
            string search = txtSearch.Text.Trim();
            int catId = cboCatFilter.SelectedItem is ComboItem ci ? ci.Id : 0;
            LoadInventory(search, catId);
        }

        private void LoadInventory(string search = "", int catId = 0)
        {
            dgvInventory.Rows.Clear();
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                string sql = "SELECT p.product_id, p.product_name, c.cat_name, p.price, p.stock " +
                             "FROM product p JOIN category c ON p.category_id=c.category_id WHERE p.is_active=1 ";
                if (!string.IsNullOrEmpty(search)) sql += "AND p.product_name LIKE @s ";
                if (catId > 0)                     sql += "AND p.category_id=@cid ";
                sql += "ORDER BY p.product_name";
                using var cmd = new MySqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
                if (catId > 0)                     cmd.Parameters.AddWithValue("@cid", catId);
                using var r = cmd.ExecuteReader();
                int count = 0;
                while (r.Read())
                {
                    int stock = r.GetInt32("stock");
                    string statusText; Color statusColor;
                    if (stock == 0)     { statusText="Out of Stock"; statusColor=Color.FromArgb(185,28,28); }
                    else if (stock<=10) { statusText="Low Stock";    statusColor=Color.FromArgb(180,110,0); }
                    else                { statusText="Adequate";     statusColor=Color.FromArgb(5,150,90);  }

                    int idx = dgvInventory.Rows.Add(
                        r["product_id"], r["product_name"], r["cat_name"],
                        $"₱ {Convert.ToDecimal(r["price"]):N2}", stock, statusText);
                    dgvInventory.Rows[idx].Cells["stock_status"].Style.ForeColor = statusColor;
                    dgvInventory.Rows[idx].Cells["stock_status"].Style.Font = new Font("Segoe UI",9f,FontStyle.Bold);
                    if (stock == 0)
                        dgvInventory.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255,240,240);
                    else if (stock <= 10)
                        dgvInventory.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255,253,235);
                    count++;
                }
                lblCount.Text = $"{count} product(s)";
            }
            catch (Exception ex) { MessageBox.Show("Error loading inventory: " + ex.Message); }
        }

        private void DgvInventory_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvInventory.SelectedRows.Count == 0) return;
            var row = dgvInventory.SelectedRows[0];
            selectedProductId   = Convert.ToInt32(row.Cells["product_id"].Value);
            selectedProductName = row.Cells["product_name"].Value.ToString();
            currentStock        = Convert.ToInt32(row.Cells["stock"].Value);
            lblSelectedProduct.Text      = $"Selected: {selectedProductName}";
            lblSelectedProduct.Font      = new Font("Segoe UI",9.5f,FontStyle.Bold);
            lblSelectedProduct.ForeColor = Color.FromArgb(30,58,95);
            lblCurrentVal.Text           = currentStock.ToString("N0") + " units";
            nudCounted.Value             = currentStock;
        }

        private void UpdateDiffPreview(Label lblDiff)
        {
            if (selectedProductId < 0) { lblDiff.Text="Select a product first."; lblDiff.ForeColor=Color.Gray; return; }
            int counted = (int)nudCounted.Value;
            int diff    = counted - currentStock;
            if (diff == 0)       { lblDiff.Text=$"No change  (Current: {currentStock} → Counted: {counted})"; lblDiff.ForeColor=Color.Gray; }
            else if (diff > 0)   { lblDiff.Text=$"▲ Increase by {diff}  ({currentStock} → {counted})"; lblDiff.ForeColor=Color.FromArgb(5,150,90); }
            else                 { lblDiff.Text=$"▼ Decrease by {Math.Abs(diff)}  ({currentStock} → {counted})"; lblDiff.ForeColor=Color.FromArgb(185,28,28); }
        }

        private void LoadAdjustmentLog()
        {
            dgvLog.Rows.Clear();
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "SELECT adj_date, product_name, old_stock, new_stock, (new_stock-old_stock) AS diff, reason " +
                    "FROM inventory_adjustment ORDER BY adj_id DESC LIMIT 15", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int d = r.GetInt32("diff");
                    int idx = dgvLog.Rows.Add(
                        Convert.ToDateTime(r["adj_date"]).ToString("MM/dd HH:mm"),
                        r["product_name"], r["old_stock"], r["new_stock"],
                        d > 0 ? $"+{d}" : d.ToString(),
                        r["reason"]);
                    dgvLog.Rows[idx].Cells["diff"].Style.ForeColor = d > 0 ? Color.FromArgb(5,150,90) : d < 0 ? Color.FromArgb(185,28,28) : Color.Gray;
                }
            }
            catch { /* table may not exist yet */ }
        }

        private void ApplyAdjustment(Label lblDiff)
        {
            if (selectedProductId < 0)
            { MessageBox.Show("Please select a product from the list.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int newStock = (int)nudCounted.Value;
            int diff     = newStock - currentStock;
            string reason = cboReason.SelectedItem?.ToString() ?? "Physical Count Correction";
            string note   = txtNote.Text.Trim();

            if (diff == 0 && string.IsNullOrEmpty(note))
            { MessageBox.Show("No change detected. Adjustment not needed.", "No Change", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            string confirmMsg = diff == 0
                ? $"Record a zero-change count for '{selectedProductName}'?"
                : $"Apply adjustment for '{selectedProductName}'?\n\nCurrent Stock: {currentStock}\nCounted Stock: {newStock}\nDifference: {(diff>=0?"+":"")}{diff}\nReason: {reason}";

            if (MessageBox.Show(confirmMsg, "Confirm Adjustment", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();

                // Update product stock
                using var cmdUpd = new MySqlCommand("UPDATE product SET stock=@s WHERE product_id=@id", conn);
                cmdUpd.Parameters.AddWithValue("@s",  newStock);
                cmdUpd.Parameters.AddWithValue("@id", selectedProductId);
                cmdUpd.ExecuteNonQuery();

                // Log adjustment (create table if not exists first)
                using var cmdCreate = new MySqlCommand(
                    "CREATE TABLE IF NOT EXISTS inventory_adjustment (" +
                    "adj_id INT AUTO_INCREMENT PRIMARY KEY, " +
                    "product_id INT NOT NULL, product_name VARCHAR(175) NOT NULL, " +
                    "old_stock INT NOT NULL, new_stock INT NOT NULL, " +
                    "reason VARCHAR(100), note TEXT, adj_by VARCHAR(50), " +
                    "adj_date DATETIME DEFAULT CURRENT_TIMESTAMP)", conn);
                cmdCreate.ExecuteNonQuery();

                using var cmdLog = new MySqlCommand(
                    "INSERT INTO inventory_adjustment (product_id, product_name, old_stock, new_stock, reason, note, adj_by) " +
                    "VALUES (@pid, @pname, @old, @new, @reason, @note, @by)", conn);
                cmdLog.Parameters.AddWithValue("@pid",    selectedProductId);
                cmdLog.Parameters.AddWithValue("@pname",  selectedProductName);
                cmdLog.Parameters.AddWithValue("@old",    currentStock);
                cmdLog.Parameters.AddWithValue("@new",    newStock);
                cmdLog.Parameters.AddWithValue("@reason", reason);
                cmdLog.Parameters.AddWithValue("@note",   note);
                cmdLog.Parameters.AddWithValue("@by",     SessionManager.Username);
                cmdLog.ExecuteNonQuery();

                MessageBox.Show("Inventory adjustment applied successfully!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                currentStock = newStock;
                lblCurrentVal.Text = newStock.ToString("N0") + " units";
                nudCounted.Value   = newStock;
                UpdateDiffPreview(lblDiff);
                LoadInventory();
                LoadAdjustmentLog();
            }
            catch (Exception ex) { MessageBox.Show("Error applying adjustment:\n" + ex.Message); }
        }

        // Factories
        private Label SL(string t, int x, int y, Font f, Color c) =>
            new Label { Text=t, Location=new Point(x,y), AutoSize=true, Font=f, ForeColor=c };
        private Label FL(string t, int x, int y) =>
            new Label { Text=t, Location=new Point(x,y), AutoSize=true, Font=new Font("Segoe UI",8.5f), ForeColor=Color.FromArgb(60,70,80) };
        private Button Btn(string t, int x, int y, int w, Color bg, Color fg)
        {
            var b = new Button { Text=t, Location=new Point(x,y), Size=new Size(w,34),
                BackColor=bg, ForeColor=fg, FlatStyle=FlatStyle.Flat, Font=new Font("Segoe UI",9f), Cursor=Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }
        private DataGridView MakeGrid(Point loc, Size sz)
        {
            var g = new DataGridView { Location=loc, Size=sz, BackgroundColor=Color.White,
                BorderStyle=BorderStyle.None, ColumnHeadersHeight=34, RowHeadersVisible=false,
                AllowUserToAddRows=false, AllowUserToDeleteRows=false, ReadOnly=true,
                AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode=DataGridViewSelectionMode.FullRowSelect,
                CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal, Font=new Font("Segoe UI",9f) };
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30,58,95);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI",9.5f,FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245,248,253);
            return g;
        }
    }
}
