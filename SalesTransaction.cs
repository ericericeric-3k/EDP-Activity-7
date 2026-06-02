// ============================================================
//  SalesTransaction.cs
//  Transaction 1: Create and manage customer sales orders
//  Add order items, update order status
// ============================================================
using System;
using System.Drawing;
using System.Windows.Forms;
using MySqlConnector;

namespace ECommSystem
{
    public class SalesTransaction : Form
    {
        // Header
        private Panel pnlHeader;
        private Label lblTitle;

        // Left: new order form
        private Panel pnlLeft;
        private Label lblCustomerLbl, lblProductLbl, lblQtyLbl, lblUnitPriceLbl;
        private ComboBox cboCustomer, cboProduct;
        private NumericUpDown nudQty;
        private TextBox txtUnitPrice;
        private Button btnAddItem, btnClearItem;
        private DataGridView dgvItems;
        private Label lblSubtotal;
        private Button btnPlaceOrder, btnCancelOrder;

        // Right: existing orders
        private Panel pnlRight;
        private Label lblOrdersTitle;
        private DataGridView dgvOrders;
        private Label lblOrderCount;
        private ComboBox cboStatusFilter;
        private Button btnFilterOrders, btnRefreshOrders;
        private Label lblStatusLbl;
        private ComboBox cboUpdateStatus;
        private Button btnUpdateStatus;

        private int currentOrderId = -1;

        public SalesTransaction()
        {
            InitializeComponent();
            LoadCustomers();
            LoadProducts();
            LoadOrders();
        }

        private void InitializeComponent()
        {
            this.Text          = "Sales Transaction";
            this.Size          = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize   = new Size(1050, 620);
            this.BackColor     = Color.FromArgb(240, 244, 248);
            this.Font          = new Font("Segoe UI", 9.5f);

            // Header
            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.FromArgb(30, 58, 95) };
            lblTitle  = new Label { Text = "🛒  Sales Transaction", Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(18, 10) };
            var lblSub = new Label { Text = "Create Orders · Add Items · Update Status",
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(180,210,240), AutoSize = true, Location = new Point(20,38) };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // ── LEFT PANEL ───────────────────────────────────
            pnlLeft = new Panel { Location = new Point(12, 74), Size = new Size(580, 590),
                BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left };

            var lblNewOrder = SL("New Sales Order", 14, 12, new Font("Segoe UI", 11f, FontStyle.Bold), Color.FromArgb(30,58,95));

            lblCustomerLbl = FL("Customer *", 14, 44);
            cboCustomer    = new ComboBox { Location = new Point(14, 62), Size = new Size(360, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };

            lblProductLbl = FL("Product *", 14, 100);
            cboProduct    = new ComboBox { Location = new Point(14, 118), Size = new Size(360, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            cboProduct.SelectedIndexChanged += CboProduct_SelectedIndexChanged;

            lblQtyLbl = FL("Quantity *", 14, 158);
            nudQty    = new NumericUpDown { Location = new Point(14, 176), Size = new Size(100, 28),
                Minimum = 1, Maximum = 9999, Value = 1, Font = new Font("Segoe UI", 9.5f) };

            lblUnitPriceLbl = FL("Unit Price (₱) *", 130, 158);
            txtUnitPrice    = new TextBox { Location = new Point(130, 176), Size = new Size(140, 28),
                Font = new Font("Segoe UI", 9.5f), ReadOnly = true, BackColor = Color.FromArgb(245,248,253) };

            btnAddItem   = Btn("➕ Add Item", 284, 174, 120, Color.FromArgb(30,58,95), Color.White);
            btnAddItem.Click += BtnAddItem_Click;
            btnClearItem = Btn("✖ Clear", 412, 174, 80, Color.FromArgb(180,190,205), Color.FromArgb(40,40,40));
            btnClearItem.Click += (s,e) => ClearItemForm();

            // Items grid
            dgvItems = MakeGrid(new Point(14, 215), new Size(550, 260));
            dgvItems.Columns.Add("product_id",   "PID");
            dgvItems.Columns.Add("product_name", "Product");
            dgvItems.Columns.Add("quantity",     "Qty");
            dgvItems.Columns.Add("unit_price",   "Unit Price");
            dgvItems.Columns.Add("subtotal",     "Subtotal");
            dgvItems.Columns["product_id"].Visible   = false;
            dgvItems.Columns["product_name"].FillWeight = 180;
            dgvItems.Columns["quantity"].FillWeight     = 50;
            dgvItems.Columns["unit_price"].FillWeight   = 90;
            dgvItems.Columns["subtotal"].FillWeight     = 90;

            // Remove item on Delete key
            dgvItems.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && dgvItems.SelectedRows.Count > 0)
                {
                    dgvItems.Rows.Remove(dgvItems.SelectedRows[0]);
                    UpdateSubtotal();
                }
            };
            dgvItems.CellEndEdit += (s,e) => UpdateSubtotal();

            lblSubtotal = new Label { Location = new Point(14, 484), AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(30,58,95) };
            UpdateSubtotal();

            var div = new Panel { Location = new Point(14, 510), Size = new Size(550, 1), BackColor = Color.FromArgb(220,228,240) };

            btnPlaceOrder  = Btn("✅ Place Order", 14, 522, 180, Color.FromArgb(13,148,100), Color.White);
            btnPlaceOrder.Font  = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnPlaceOrder.Click += BtnPlaceOrder_Click;

            btnCancelOrder  = Btn("⛔ Cancel / Clear", 204, 522, 160, Color.FromArgb(210,50,50), Color.White);
            btnCancelOrder.Click += (s,e) => { dgvItems.Rows.Clear(); ClearItemForm(); UpdateSubtotal(); };

            pnlLeft.Controls.AddRange(new Control[] {
                lblNewOrder,
                lblCustomerLbl, cboCustomer,
                lblProductLbl,  cboProduct,
                lblQtyLbl,      nudQty,
                lblUnitPriceLbl, txtUnitPrice,
                btnAddItem, btnClearItem,
                dgvItems, lblSubtotal, div,
                btnPlaceOrder, btnCancelOrder
            });

            // ── RIGHT PANEL ──────────────────────────────────
            pnlRight = new Panel { Location = new Point(604, 74), Size = new Size(570, 590),
                BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right };

            lblOrdersTitle = SL("Order Records", 14, 12, new Font("Segoe UI", 11f, FontStyle.Bold), Color.FromArgb(30,58,95));

            // Filter row
            var lblFilter = FL("Filter by Status:", 14, 44);
            cboStatusFilter = new ComboBox { Location = new Point(14, 62), Size = new Size(160, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            cboStatusFilter.Items.AddRange(new object[] { "All", "Pending", "Paid", "Shipped", "Cancelled" });
            cboStatusFilter.SelectedIndex = 0;

            btnFilterOrders  = Btn("🔍 Filter", 184, 60, 80, Color.FromArgb(30,58,95), Color.White);
            btnFilterOrders.Click += (s,e) => LoadOrders(cboStatusFilter.SelectedItem?.ToString() ?? "All");

            btnRefreshOrders = Btn("↺", 272, 60, 40, Color.FromArgb(200,210,225), Color.FromArgb(40,40,40));
            btnRefreshOrders.Click += (s,e) => { cboStatusFilter.SelectedIndex=0; LoadOrders(); };

            dgvOrders = MakeGrid(new Point(14, 98), new Size(540, 390));
            dgvOrders.Columns.Add("order_id",    "Order ID");
            dgvOrders.Columns.Add("customer",    "Customer");
            dgvOrders.Columns.Add("order_date",  "Date");
            dgvOrders.Columns.Add("total",       "Total (₱)");
            dgvOrders.Columns.Add("status",      "Status");
            dgvOrders.Columns["order_id"].FillWeight   = 55;
            dgvOrders.Columns["customer"].FillWeight   = 100;
            dgvOrders.Columns["order_date"].FillWeight = 100;
            dgvOrders.Columns["total"].FillWeight      = 90;
            dgvOrders.Columns["status"].FillWeight     = 75;

            lblOrderCount = new Label { Text = "0 orders", Location = new Point(14,495), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic), ForeColor = Color.Gray };

            var div2 = new Panel { Location = new Point(14,515), Size = new Size(540,1), BackColor = Color.FromArgb(220,228,240) };

            lblStatusLbl   = FL("Update Status of Selected Order:", 14, 524);
            cboUpdateStatus = new ComboBox { Location = new Point(14, 542), Size = new Size(160, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            cboUpdateStatus.Items.AddRange(new object[] { "Pending", "Paid", "Shipped", "Cancelled" });
            cboUpdateStatus.SelectedIndex = 0;

            btnUpdateStatus  = Btn("💾 Update", 184, 540, 110, Color.FromArgb(245,158,11), Color.White);
            btnUpdateStatus.Click += BtnUpdateStatus_Click;

            pnlRight.Controls.AddRange(new Control[] {
                lblOrdersTitle,
                lblFilter, cboStatusFilter, btnFilterOrders, btnRefreshOrders,
                dgvOrders, lblOrderCount, div2,
                lblStatusLbl, cboUpdateStatus, btnUpdateStatus
            });

            var btnClose = Btn("Close", 14, 572, 540, Color.FromArgb(100,110,125), Color.White);
            btnClose.Click += (s,e) => this.Close();
            pnlRight.Controls.Add(btnClose);

            this.Controls.AddRange(new Control[] { pnlHeader, pnlLeft, pnlRight });
        }

        // ── Data Loaders ─────────────────────────────────────
        private void LoadCustomers()
        {
            cboCustomer.Items.Clear();
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "SELECT user_id, username FROM user WHERE is_active=1 ORDER BY username", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    cboCustomer.Items.Add(new ComboItem(r.GetInt32(0), r.GetString(1)));
                if (cboCustomer.Items.Count > 0) cboCustomer.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show("Error loading customers: " + ex.Message); }
        }

        private void LoadProducts()
        {
            cboProduct.Items.Clear();
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "SELECT product_id, product_name, price FROM product WHERE is_active=1 AND stock>0 ORDER BY product_name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    cboProduct.Items.Add(new ProductComboItem(r.GetInt32(0), r.GetString(1), r.GetDecimal(2)));
                if (cboProduct.Items.Count > 0) cboProduct.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show("Error loading products: " + ex.Message); }
        }

        private void LoadOrders(string statusFilter = "All")
        {
            dgvOrders.Rows.Clear();
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                string sql = "SELECT o.order_id, u.username, o.order_date, o.total_amount, o.status " +
                             "FROM `order` o JOIN user u ON o.user_id=u.user_id WHERE 1=1 ";
                if (statusFilter != "All") sql += "AND o.status=@s ";
                sql += "ORDER BY o.order_id DESC LIMIT 50";
                using var cmd = new MySqlCommand(sql, conn);
                if (statusFilter != "All") cmd.Parameters.AddWithValue("@s", statusFilter);
                using var r = cmd.ExecuteReader();
                int count = 0;
                while (r.Read())
                {
                    string status = r["status"].ToString();
                    int idx = dgvOrders.Rows.Add(
                        r["order_id"],
                        r["username"],
                        Convert.ToDateTime(r["order_date"]).ToString("MM/dd/yyyy HH:mm"),
                        $"₱ {Convert.ToDecimal(r["total_amount"]):N2}",
                        status);
                    dgvOrders.Rows[idx].Cells["status"].Style.ForeColor = status switch
                    {
                        "Paid"      => Color.FromArgb(5,150,90),
                        "Shipped"   => Color.FromArgb(30,100,200),
                        "Pending"   => Color.FromArgb(180,110,0),
                        "Cancelled" => Color.FromArgb(185,28,28),
                        _           => Color.Black
                    };
                    dgvOrders.Rows[idx].Cells["status"].Style.Font = new Font("Segoe UI",9f,FontStyle.Bold);
                    count++;
                }
                lblOrderCount.Text = $"{count} order(s) found";
            }
            catch (Exception ex) { MessageBox.Show("Error loading orders: " + ex.Message); }
        }

        // ── Event Handlers ───────────────────────────────────
        private void CboProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboProduct.SelectedItem is ProductComboItem p)
                txtUnitPrice.Text = p.Price.ToString("F2");
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (cboProduct.SelectedItem is not ProductComboItem p) return;
            if (!decimal.TryParse(txtUnitPrice.Text, out decimal price)) { MessageBox.Show("Invalid unit price."); return; }
            int qty = (int)nudQty.Value;
            decimal sub = price * qty;

            // Check for duplicate product in grid; if found, add quantity
            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                if (row.Cells["product_id"].Value?.ToString() == p.Id.ToString())
                {
                    int existQty = Convert.ToInt32(row.Cells["quantity"].Value);
                    int newQty   = existQty + qty;
                    row.Cells["quantity"].Value = newQty;
                    row.Cells["subtotal"].Value = $"₱ {price * newQty:N2}";
                    UpdateSubtotal();
                    return;
                }
            }

            dgvItems.Rows.Add(p.Id, p.Name, qty, $"₱ {price:N2}", $"₱ {sub:N2}");
            UpdateSubtotal();
        }

        private void BtnPlaceOrder_Click(object sender, EventArgs e)
        {
            if (cboCustomer.SelectedItem is not ComboItem customer)
            { MessageBox.Show("Please select a customer.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (dgvItems.Rows.Count == 0)
            { MessageBox.Show("Please add at least one item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var confirm = MessageBox.Show("Place this order?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var tx = conn.BeginTransaction();

                // Insert order
                using var cmdOrder = new MySqlCommand(
                    "INSERT INTO `order` (user_id, order_date, status) VALUES (@uid, NOW(), 'Pending')", conn, tx);
                cmdOrder.Parameters.AddWithValue("@uid", customer.Id);
                cmdOrder.ExecuteNonQuery();

                long newOrderId = cmdOrder.LastInsertedId;

                // Insert items
                foreach (DataGridViewRow row in dgvItems.Rows)
                {
                    int     pid   = Convert.ToInt32(row.Cells["product_id"].Value);
                    int     qty   = Convert.ToInt32(row.Cells["quantity"].Value);
                    decimal price = Convert.ToDecimal(row.Cells["unit_price"].Value.ToString().Replace("₱","").Trim());

                    // Check stock
                    using var chk = new MySqlCommand("SELECT stock FROM product WHERE product_id=@pid", conn, tx);
                    chk.Parameters.AddWithValue("@pid", pid);
                    int stock = Convert.ToInt32(chk.ExecuteScalar());
                    if (stock < qty)
                    {
                        string pname = row.Cells["product_name"].Value.ToString();
                        MessageBox.Show($"Insufficient stock for '{pname}'.\nAvailable: {stock}", "Stock Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        tx.Rollback();
                        return;
                    }

                    using var cmdItem = new MySqlCommand(
                        "INSERT INTO order_item (order_id, product_id, quantity, unit_price) VALUES (@oid, @pid, @qty, @price)", conn, tx);
                    cmdItem.Parameters.AddWithValue("@oid",   newOrderId);
                    cmdItem.Parameters.AddWithValue("@pid",   pid);
                    cmdItem.Parameters.AddWithValue("@qty",   qty);
                    cmdItem.Parameters.AddWithValue("@price", price);
                    cmdItem.ExecuteNonQuery();
                }

                tx.Commit();
                MessageBox.Show($"Order #{newOrderId} placed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dgvItems.Rows.Clear();
                UpdateSubtotal();
                LoadOrders();
            }
            catch (Exception ex) { MessageBox.Show("Error placing order:\n" + ex.Message); }
        }

        private void BtnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0)
            { MessageBox.Show("Please select an order.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int    orderId = Convert.ToInt32(dgvOrders.SelectedRows[0].Cells["order_id"].Value);
            string newStatus = cboUpdateStatus.SelectedItem?.ToString() ?? "Pending";

            var confirm = MessageBox.Show($"Update Order #{orderId} status to '{newStatus}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("UPDATE `order` SET status=@s WHERE order_id=@id", conn);
                cmd.Parameters.AddWithValue("@s",  newStatus);
                cmd.Parameters.AddWithValue("@id", orderId);
                cmd.ExecuteNonQuery();
                MessageBox.Show("Order status updated.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadOrders(cboStatusFilter.SelectedItem?.ToString() ?? "All");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        // ── Helpers ──────────────────────────────────────────
        private void UpdateSubtotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                string raw = row.Cells["subtotal"].Value?.ToString()?.Replace("₱","").Trim() ?? "0";
                if (decimal.TryParse(raw, out decimal v)) total += v;
            }
            lblSubtotal.Text = $"Order Total:  ₱ {total:N2}";
        }

        private void ClearItemForm()
        {
            nudQty.Value = 1;
            txtUnitPrice.Clear();
            if (cboProduct.Items.Count > 0) cboProduct.SelectedIndex = 0;
        }

        // Factory helpers
        private Label SL(string t, int x, int y, Font f, Color c) =>
            new Label { Text=t, Location=new Point(x,y), AutoSize=true, Font=f, ForeColor=c };
        private Label FL(string t, int x, int y) =>
            new Label { Text=t, Location=new Point(x,y), AutoSize=true,
                Font=new Font("Segoe UI",8.5f), ForeColor=Color.FromArgb(60,70,80) };
        private Button Btn(string t, int x, int y, int w, Color bg, Color fg)
        {
            var b = new Button { Text=t, Location=new Point(x,y), Size=new Size(w,34),
                BackColor=bg, ForeColor=fg, FlatStyle=FlatStyle.Flat,
                Font=new Font("Segoe UI",9f), Cursor=Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
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

    // ── Helper classes ────────────────────────────────────
    class ComboItem
    {
        public int    Id   { get; }
        public string Name { get; }
        public ComboItem(int id, string name) { Id=id; Name=name; }
        public override string ToString() => Name;
    }

    class ProductComboItem
    {
        public int     Id    { get; }
        public string  Name  { get; }
        public decimal Price { get; }
        public ProductComboItem(int id, string name, decimal price) { Id=id; Name=name; Price=price; }
        public override string ToString() => $"{Name}  (₱{Price:N2})";
    }
}
