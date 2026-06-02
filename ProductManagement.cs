// ============================================================
//  ProductManagement.cs
//  Transaction 2: Add, edit, activate/deactivate products
//  Manage product catalog with category assignment
// ============================================================
using System;
using System.Drawing;
using System.Windows.Forms;
using MySqlConnector;

namespace ECommSystem
{
    public class ProductManagement : Form
    {
        // Header
        private Panel pnlHeader;

        // Left: product list
        private Panel pnlLeft;
        private TextBox txtSearch;
        private Button btnSearch, btnRefresh;
        private DataGridView dgvProducts;
        private Label lblCount;

        // Right: form
        private Panel pnlRight;
        private Label lblPanelTitle;
        private Label lblName, lblCat, lblPrice, lblStock, lblStatus;
        private TextBox txtName, txtPrice, txtStock;
        private ComboBox cboCat;
        private CheckBox chkActive;
        private Button btnAdd, btnSave, btnClear;
        private Button btnActivate, btnDeactivate, btnEditRow, btnClose;

        private int selectedProductId = -1;

        public ProductManagement()
        {
            InitializeComponent();
            LoadCategories();
            LoadProducts();
        }

        private void InitializeComponent()
        {
            this.Text          = "Product Management";
            this.Size          = new Size(1020, 660);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize   = new Size(900, 580);
            this.BackColor     = Color.FromArgb(240,244,248);
            this.Font          = new Font("Segoe UI", 9.5f);

            // Header
            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.FromArgb(30,58,95) };
            pnlHeader.Controls.AddRange(new Control[] {
                new Label { Text="📦  Product Management", Font=new Font("Segoe UI",14f,FontStyle.Bold),
                    ForeColor=Color.White, AutoSize=true, Location=new Point(18,10) },
                new Label { Text="Add · Edit · Activate · Deactivate · Search",
                    Font=new Font("Segoe UI",9f), ForeColor=Color.FromArgb(180,210,240), AutoSize=true, Location=new Point(20,38) }
            });

            // ── LEFT ─────────────────────────────────────────
            pnlLeft = new Panel { Location=new Point(12,74), Size=new Size(585,552),
                BackColor=Color.White, Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left };

            pnlLeft.Controls.Add(SL("Product Catalog", 14, 12, new Font("Segoe UI",11f,FontStyle.Bold), Color.FromArgb(30,58,95)));

            txtSearch = new TextBox { Location=new Point(14,44), Size=new Size(370,28),
                PlaceholderText="Search product name…", Font=new Font("Segoe UI",9.5f) };
            txtSearch.KeyDown += (s,e) => { if (e.KeyCode==Keys.Enter) LoadProducts(txtSearch.Text.Trim()); };

            btnSearch  = Btn("🔍 Search", 392, 44, 80, Color.FromArgb(30,58,95), Color.White);
            btnSearch.Click += (s,e) => LoadProducts(txtSearch.Text.Trim());

            btnRefresh = Btn("↺", 480, 44, 40, Color.FromArgb(200,210,225), Color.FromArgb(40,40,40));
            btnRefresh.Click += (s,e) => { txtSearch.Clear(); LoadProducts(); };

            dgvProducts = MakeGrid(new Point(14,80), new Size(556,420));
            dgvProducts.Columns.Add("product_id",   "ID");
            dgvProducts.Columns.Add("product_name", "Name");
            dgvProducts.Columns.Add("cat_name",     "Category");
            dgvProducts.Columns.Add("price",        "Price (₱)");
            dgvProducts.Columns.Add("stock",        "Stock");
            dgvProducts.Columns.Add("is_active",    "Status");
            dgvProducts.Columns["product_id"].FillWeight   = 35;
            dgvProducts.Columns["product_name"].FillWeight = 130;
            dgvProducts.Columns["cat_name"].FillWeight     = 90;
            dgvProducts.Columns["price"].FillWeight        = 75;
            dgvProducts.Columns["stock"].FillWeight        = 50;
            dgvProducts.Columns["is_active"].FillWeight    = 65;
            dgvProducts.SelectionChanged += (s,e) =>
            {
                if (dgvProducts.SelectedRows.Count > 0)
                    selectedProductId = Convert.ToInt32(dgvProducts.SelectedRows[0].Cells["product_id"].Value);
            };

            lblCount = new Label { Text="0 record(s)", Location=new Point(14,507), AutoSize=true,
                Font=new Font("Segoe UI",8.5f,FontStyle.Italic), ForeColor=Color.Gray };

            btnActivate   = Btn("✅ Activate",   14,  528, 110, Color.FromArgb(13,148,100), Color.White);
            btnActivate.Click += (s,e) => SetStatus(1);
            btnDeactivate = Btn("⛔ Deactivate", 134, 528, 115, Color.FromArgb(210,50,50),  Color.White);
            btnDeactivate.Click += (s,e) => SetStatus(0);
            btnEditRow    = Btn("✏ Edit",        259, 528, 80,  Color.FromArgb(245,158,11), Color.White);
            btnEditRow.Click += BtnEditRow_Click;

            pnlLeft.Controls.AddRange(new Control[] {
                txtSearch, btnSearch, btnRefresh,
                dgvProducts, lblCount,
                btnActivate, btnDeactivate, btnEditRow
            });

            // ── RIGHT ────────────────────────────────────────
            pnlRight = new Panel { Location=new Point(608,74), Size=new Size(390,552),
                BackColor=Color.White, Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Right };

            lblPanelTitle = SL("Add New Product", 14, 12, new Font("Segoe UI",11f,FontStyle.Bold), Color.FromArgb(30,58,95));
            pnlRight.Controls.AddRange(new Control[] {
                lblPanelTitle,
                new Panel { Location=new Point(14,38), Size=new Size(358,1), BackColor=Color.FromArgb(220,228,240) }
            });

            lblName = FL("Product Name *", 14, 50);
            txtName = FT(14, 70, "Enter product name");

            lblCat = FL("Category *", 14, 108);
            cboCat = new ComboBox { Location=new Point(14,128), Size=new Size(358,28),
                DropDownStyle=ComboBoxStyle.DropDownList, Font=new Font("Segoe UI",9.5f) };

            lblPrice = FL("Price (₱) *", 14, 168);
            txtPrice = FT(14, 188, "e.g. 999.00");
            txtPrice.Size = new Size(170, 28);

            lblStock = FL("Initial Stock *", 200, 168);
            txtStock = new TextBox { Location=new Point(200,188), Size=new Size(172,28),
                PlaceholderText="e.g. 100", Font=new Font("Segoe UI",9.5f) };

            lblStatus = FL("Status", 14, 228);
            chkActive = new CheckBox { Text="Active", Location=new Point(14,248), AutoSize=true, Checked=true, Font=new Font("Segoe UI",9.5f) };

            var div2 = new Panel { Location=new Point(14,280), Size=new Size(358,1), BackColor=Color.FromArgb(220,228,240) };

            btnAdd  = Btn("➕ Add Product",  14, 294, 170, Color.FromArgb(30,58,95), Color.White);
            btnAdd.Font   = new Font("Segoe UI",10f,FontStyle.Bold);
            btnAdd.Click += BtnAdd_Click;

            btnSave  = Btn("💾 Save Changes", 14, 294, 170, Color.FromArgb(13,148,100), Color.White);
            btnSave.Font    = new Font("Segoe UI",10f,FontStyle.Bold);
            btnSave.Visible = false;
            btnSave.Click  += BtnSave_Click;

            btnClear  = Btn("✖ Cancel", 194, 294, 170, Color.FromArgb(180,190,205), Color.FromArgb(40,40,40));
            btnClear.Click += (s,e) => ClearForm();

            btnClose  = Btn("Close", 14, 504, 358, Color.FromArgb(100,110,125), Color.White);
            btnClose.Click += (s,e) => this.Close();

            pnlRight.Controls.AddRange(new Control[] {
                lblName, txtName,
                lblCat,  cboCat,
                lblPrice, txtPrice,
                lblStock, txtStock,
                lblStatus, chkActive,
                div2,
                btnAdd, btnSave, btnClear,
                btnClose
            });

            this.Controls.AddRange(new Control[] { pnlHeader, pnlLeft, pnlRight });
        }

        // ── Data ─────────────────────────────────────────────
        private void LoadCategories()
        {
            cboCat.Items.Clear();
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT category_id, cat_name FROM category ORDER BY cat_name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    cboCat.Items.Add(new ComboItem(r.GetInt32(0), r.GetString(1)));
                if (cboCat.Items.Count > 0) cboCat.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show("Error loading categories: " + ex.Message); }
        }

        private void LoadProducts(string search = "")
        {
            dgvProducts.Rows.Clear();
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                string sql = "SELECT p.product_id, p.product_name, c.cat_name, p.price, p.stock, p.is_active " +
                             "FROM product p JOIN category c ON p.category_id=c.category_id WHERE 1=1 ";
                if (!string.IsNullOrEmpty(search)) sql += "AND p.product_name LIKE @s ";
                sql += "ORDER BY p.product_id DESC";
                using var cmd = new MySqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
                using var r = cmd.ExecuteReader();
                int count = 0;
                while (r.Read())
                {
                    bool active = r.GetInt32("is_active") == 1;
                    int idx = dgvProducts.Rows.Add(
                        r["product_id"], r["product_name"], r["cat_name"],
                        $"₱ {Convert.ToDecimal(r["price"]):N2}", r["stock"],
                        active ? "Active" : "Inactive");
                    dgvProducts.Rows[idx].Cells["is_active"].Style.ForeColor =
                        active ? Color.FromArgb(5,150,90) : Color.FromArgb(185,28,28);
                    dgvProducts.Rows[idx].Cells["is_active"].Style.Font = new Font("Segoe UI",9f,FontStyle.Bold);
                    count++;
                }
                lblCount.Text = $"{count} record(s) found";
            }
            catch (Exception ex) { MessageBox.Show("Error loading products: " + ex.Message); }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "INSERT INTO product (category_id, product_name, price, stock, is_active) VALUES (@c,@n,@p,@s,@a)", conn);
                cmd.Parameters.AddWithValue("@c", ((ComboItem)cboCat.SelectedItem).Id);
                cmd.Parameters.AddWithValue("@n", txtName.Text.Trim());
                cmd.Parameters.AddWithValue("@p", decimal.Parse(txtPrice.Text));
                cmd.Parameters.AddWithValue("@s", int.Parse(txtStock.Text));
                cmd.Parameters.AddWithValue("@a", chkActive.Checked ? 1 : 0);
                cmd.ExecuteNonQuery();
                MessageBox.Show("Product added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadProducts();
            }
            catch (Exception ex) { MessageBox.Show("Error adding product:\n" + ex.Message); }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (selectedProductId < 0) return;
            if (!ValidateForm()) return;
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "UPDATE product SET category_id=@c, product_name=@n, price=@p, stock=@s, is_active=@a WHERE product_id=@id", conn);
                cmd.Parameters.AddWithValue("@c",  ((ComboItem)cboCat.SelectedItem).Id);
                cmd.Parameters.AddWithValue("@n",  txtName.Text.Trim());
                cmd.Parameters.AddWithValue("@p",  decimal.Parse(txtPrice.Text));
                cmd.Parameters.AddWithValue("@s",  int.Parse(txtStock.Text));
                cmd.Parameters.AddWithValue("@a",  chkActive.Checked ? 1 : 0);
                cmd.Parameters.AddWithValue("@id", selectedProductId);
                cmd.ExecuteNonQuery();
                MessageBox.Show("Product updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearForm();
                LoadProducts();
            }
            catch (Exception ex) { MessageBox.Show("Error updating product:\n" + ex.Message); }
        }

        private void BtnEditRow_Click(object sender, EventArgs e)
        {
            if (selectedProductId < 0) { MessageBox.Show("Please select a product.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT product_name, category_id, price, stock, is_active FROM product WHERE product_id=@id", conn);
                cmd.Parameters.AddWithValue("@id", selectedProductId);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return;
                txtName.Text = r.GetString("product_name");
                txtPrice.Text = r.GetDecimal("price").ToString("F2");
                txtStock.Text = r.GetInt32("stock").ToString();
                chkActive.Checked = r.GetInt32("is_active") == 1;
                int catId = r.GetInt32("category_id");
                foreach (ComboItem item in cboCat.Items)
                    if (item.Id == catId) { cboCat.SelectedItem = item; break; }
            }
            catch (Exception ex) { MessageBox.Show("Error loading product: " + ex.Message); return; }

            lblPanelTitle.Text     = $"Edit Product  [ID: {selectedProductId}]";
            lblPanelTitle.ForeColor = Color.FromArgb(13,148,100);
            btnAdd.Visible  = false;
            btnSave.Visible = true;
        }

        private void SetStatus(int status)
        {
            if (selectedProductId < 0) { MessageBox.Show("Please select a product.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            string action = status == 1 ? "activate" : "deactivate";
            if (MessageBox.Show($"Are you sure you want to {action} this product?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("UPDATE product SET is_active=@a WHERE product_id=@id", conn);
                cmd.Parameters.AddWithValue("@a",  status);
                cmd.Parameters.AddWithValue("@id", selectedProductId);
                cmd.ExecuteNonQuery();
                MessageBox.Show($"Product {action}d.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadProducts();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Product name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtName.Focus(); return false; }
            if (!decimal.TryParse(txtPrice.Text, out decimal p) || p < 0) { MessageBox.Show("Enter a valid price.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtPrice.Focus(); return false; }
            if (!int.TryParse(txtStock.Text, out int s) || s < 0) { MessageBox.Show("Enter a valid stock quantity.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtStock.Focus(); return false; }
            if (cboCat.SelectedItem == null) { MessageBox.Show("Please select a category.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); cboCat.Focus(); return false; }
            return true;
        }

        private void ClearForm()
        {
            selectedProductId       = -1;
            txtName.Clear(); txtPrice.Clear(); txtStock.Clear();
            chkActive.Checked       = true;
            if (cboCat.Items.Count > 0) cboCat.SelectedIndex = 0;
            lblPanelTitle.Text      = "Add New Product";
            lblPanelTitle.ForeColor = Color.FromArgb(30,58,95);
            btnAdd.Visible  = true;
            btnSave.Visible = false;
        }

        // Factories
        private Label SL(string t, int x, int y, Font f, Color c) =>
            new Label { Text=t, Location=new Point(x,y), AutoSize=true, Font=f, ForeColor=c };
        private Label FL(string t, int x, int y) =>
            new Label { Text=t, Location=new Point(x,y), AutoSize=true, Font=new Font("Segoe UI",8.5f), ForeColor=Color.FromArgb(60,70,80) };
        private TextBox FT(int x, int y, string ph) =>
            new TextBox { Location=new Point(x,y), Size=new Size(358,28), PlaceholderText=ph, Font=new Font("Segoe UI",9.5f) };
        private Button Btn(string t, int x, int y, int w, Color bg, Color fg)
        {
            var b = new Button { Text=t, Location=new Point(x,y), Size=new Size(w,34),
                BackColor=bg, ForeColor=fg, FlatStyle=FlatStyle.Flat,
                Font=new Font("Segoe UI",9f), Cursor=Cursors.Hand };
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
