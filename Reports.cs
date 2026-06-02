// ============================================================
//  Reports.cs
//  Report Generation Module
//  Three reports: Sales Summary | Product Inventory | User Activity
//  DataGrid view + Export to Excel with header/logo/signature/chart
// ============================================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MySqlConnector;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;

namespace ECommSystem
{
    public class Reports : Form
    {
        // Header
        private Panel pnlHeader;

        // Tab strip
        private TabControl tabReports;
        private TabPage    tabSales, tabInventory, tabUsers;

        // ── Sales tab ────────────────────────────────────────
        private DateTimePicker dtpSalesFrom, dtpSalesTo;
        private Button  btnLoadSales, btnExportSales;
        private DataGridView dgvSales;
        private Label   lblSalesCount;

        // ── Inventory tab ────────────────────────────────────
        private ComboBox cboCatRpt;
        private Button  btnLoadInventory, btnExportInventory;
        private DataGridView dgvInventory;
        private Label   lblInvCount;

        // ── User Activity tab ────────────────────────────────
        private ComboBox cboRoleFilter;
        private Button  btnLoadUsers, btnExportUsers;
        private DataGridView dgvUsers;
        private Label   lblUserCount;

        // Cached data tables for export
        private DataTable dtSales, dtInventory, dtUsers;

        public Reports()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            InitializeComponent();
            LoadCategoryFilter();
        }

        private void InitializeComponent()
        {
            this.Text          = "Report Generation";
            this.Size          = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize   = new Size(980, 640);
            this.BackColor     = Color.FromArgb(240,244,248);
            this.Font          = new Font("Segoe UI", 9.5f);

            // Header
            pnlHeader = new Panel { Dock=DockStyle.Top, Height=62, BackColor=Color.FromArgb(30,58,95) };
            pnlHeader.Controls.AddRange(new Control[] {
                new Label { Text="📊  Report Generation", Font=new Font("Segoe UI",14f,FontStyle.Bold),
                    ForeColor=Color.White, AutoSize=true, Location=new Point(18,10) },
                new Label { Text="Sales Summary · Product Inventory · User Activity  |  Export to Excel",
                    Font=new Font("Segoe UI",9f), ForeColor=Color.FromArgb(180,210,240), AutoSize=true, Location=new Point(20,38) }
            });

            // Tab control
            tabReports = new TabControl
            {
                Location = new Point(12,74), Size = new Size(1064,608),
                Anchor   = AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right,
                Font     = new Font("Segoe UI",10f)
            };

            BuildSalesTab();
            BuildInventoryTab();
            BuildUsersTab();

            tabReports.TabPages.AddRange(new TabPage[] { tabSales, tabInventory, tabUsers });

            this.Controls.AddRange(new Control[] { pnlHeader, tabReports });
        }

        // ── SALES TAB ─────────────────────────────────────────
        private void BuildSalesTab()
        {
            tabSales = new TabPage("📄  Sales Summary") { BackColor = Color.FromArgb(245,248,253), Padding = new Padding(10) };

            // Toolbar
            var pnlTool = new Panel { Location=new Point(10,10), Size=new Size(1030,46), BackColor=Color.White };

            var lblFrom = FL("Date From:", 10, 14);
            dtpSalesFrom = new DateTimePicker { Location=new Point(80,10), Size=new Size(140,28), Format=DateTimePickerFormat.Short };
            dtpSalesFrom.Value = DateTime.Today.AddMonths(-1);

            var lblTo  = FL("Date To:", 230, 14);
            dtpSalesTo  = new DateTimePicker { Location=new Point(290,10), Size=new Size(140,28), Format=DateTimePickerFormat.Short };
            dtpSalesTo.Value = DateTime.Today;

            btnLoadSales   = Btn("🔍 Load Report", 450, 10, 130, Color.FromArgb(30,58,95), Color.White);
            btnLoadSales.Click += BtnLoadSales_Click;

            btnExportSales = Btn("📥 Export to Excel", 590, 10, 160, Color.FromArgb(13,148,100), Color.White);
            btnExportSales.Click += (s,e) => ExportSales();

            lblSalesCount = new Label { Text="", Location=new Point(760,14), AutoSize=true,
                Font=new Font("Segoe UI",8.5f,FontStyle.Italic), ForeColor=Color.Gray };

            pnlTool.Controls.AddRange(new Control[] { lblFrom, dtpSalesFrom, lblTo, dtpSalesTo,
                btnLoadSales, btnExportSales, lblSalesCount });

            dgvSales = MakeGrid(new Point(10,66), new Size(1030,490));
            dgvSales.Anchor = AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;

            tabSales.Controls.AddRange(new Control[] { pnlTool, dgvSales });
        }

        private void BtnLoadSales_Click(object sender, EventArgs e)
        {
            dgvSales.Columns.Clear();
            dgvSales.Rows.Clear();
            dtSales = new DataTable();
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                string sql =
                    "SELECT o.order_id AS `Order ID`, u.username AS `Customer`, " +
                    "DATE_FORMAT(o.order_date,'%m/%d/%Y') AS `Order Date`, " +
                    "o.status AS `Status`, " +
                    "GROUP_CONCAT(p.product_name ORDER BY p.product_name SEPARATOR ', ') AS `Products`, " +
                    "SUM(oi.quantity) AS `Total Items`, " +
                    "o.total_amount AS `Total Amount (₱)` " +
                    "FROM `order` o " +
                    "JOIN user u ON o.user_id=u.user_id " +
                    "JOIN order_item oi ON o.order_id=oi.order_id " +
                    "JOIN product p ON oi.product_id=p.product_id " +
                    "WHERE DATE(o.order_date) BETWEEN @from AND @to " +
                    "GROUP BY o.order_id ORDER BY o.order_id DESC";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@from", dtpSalesFrom.Value.Date);
                cmd.Parameters.AddWithValue("@to",   dtpSalesTo.Value.Date);
                using var adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(dtSales);

                // Bind to grid
                BindTableToGrid(dgvSales, dtSales);
                lblSalesCount.Text = $"{dtSales.Rows.Count} record(s)";

                // Color-code status column
                int statusCol = dgvSales.Columns["Status"]?.Index ?? -1;
                if (statusCol >= 0)
                {
                    foreach (DataGridViewRow row in dgvSales.Rows)
                    {
                        string st = row.Cells[statusCol].Value?.ToString() ?? "";
                        row.Cells[statusCol].Style.ForeColor = st switch
                        {
                            "Paid"      => Color.FromArgb(5,150,90),
                            "Shipped"   => Color.FromArgb(30,100,200),
                            "Pending"   => Color.FromArgb(180,110,0),
                            "Cancelled" => Color.FromArgb(185,28,28),
                            _           => Color.Black
                        };
                        row.Cells[statusCol].Style.Font = new Font("Segoe UI",9f,FontStyle.Bold);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading sales report:\n" + ex.Message); }
        }

        // ── INVENTORY TAB ─────────────────────────────────────
        private void BuildInventoryTab()
        {
            tabInventory = new TabPage("📦  Product Inventory") { BackColor=Color.FromArgb(245,248,253), Padding=new Padding(10) };

            var pnlTool = new Panel { Location=new Point(10,10), Size=new Size(1030,46), BackColor=Color.White };

            var lblCat = FL("Category:", 10, 14);
            cboCatRpt  = new ComboBox { Location=new Point(75,10), Size=new Size(200,28),
                DropDownStyle=ComboBoxStyle.DropDownList, Font=new Font("Segoe UI",9.5f) };

            btnLoadInventory  = Btn("🔍 Load Report",    285, 10, 130, Color.FromArgb(30,58,95),    Color.White);
            btnLoadInventory.Click += BtnLoadInventory_Click;

            btnExportInventory = Btn("📥 Export to Excel", 425, 10, 160, Color.FromArgb(13,148,100), Color.White);
            btnExportInventory.Click += (s,e) => ExportInventory();

            lblInvCount = new Label { Text="", Location=new Point(595,14), AutoSize=true,
                Font=new Font("Segoe UI",8.5f,FontStyle.Italic), ForeColor=Color.Gray };

            pnlTool.Controls.AddRange(new Control[] { lblCat, cboCatRpt, btnLoadInventory, btnExportInventory, lblInvCount });

            dgvInventory = MakeGrid(new Point(10,66), new Size(1030,490));
            dgvInventory.Anchor = AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;

            tabInventory.Controls.AddRange(new Control[] { pnlTool, dgvInventory });
        }

        private void LoadCategoryFilter()
        {
            cboCatRpt.Items.Clear();
            cboCatRpt.Items.Add(new ComboItem(0, "All Categories"));
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT category_id, cat_name FROM category ORDER BY cat_name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read()) cboCatRpt.Items.Add(new ComboItem(r.GetInt32(0), r.GetString(1)));
            }
            catch { }
            cboCatRpt.SelectedIndex = 0;
        }

        private void BtnLoadInventory_Click(object sender, EventArgs e)
        {
            dgvInventory.Columns.Clear();
            dgvInventory.Rows.Clear();
            dtInventory = new DataTable();
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                int catId = cboCatRpt.SelectedItem is ComboItem ci ? ci.Id : 0;
                string sql =
                    "SELECT p.product_id AS `Product ID`, p.product_name AS `Product Name`, " +
                    "c.cat_name AS `Category`, p.price AS `Price (₱)`, p.stock AS `Current Stock`, " +
                    "IFNULL(SUM(oi.quantity),0) AS `Total Sold`, " +
                    "CASE WHEN p.stock=0 THEN 'Out of Stock' WHEN p.stock<=10 THEN 'Low Stock' ELSE 'Adequate' END AS `Stock Status`, " +
                    "CASE WHEN p.is_active=1 THEN 'Active' ELSE 'Inactive' END AS `Product Status` " +
                    "FROM product p " +
                    "JOIN category c ON p.category_id=c.category_id " +
                    "LEFT JOIN order_item oi ON p.product_id=oi.product_id " +
                    (catId > 0 ? "WHERE p.category_id=@cid " : "") +
                    "GROUP BY p.product_id ORDER BY c.cat_name, p.product_name";
                using var cmd = new MySqlCommand(sql, conn);
                if (catId > 0) cmd.Parameters.AddWithValue("@cid", catId);
                using var adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(dtInventory);
                BindTableToGrid(dgvInventory, dtInventory);
                lblInvCount.Text = $"{dtInventory.Rows.Count} record(s)";

                int statusIdx = dgvInventory.Columns["Stock Status"]?.Index ?? -1;
                if (statusIdx >= 0)
                {
                    foreach (DataGridViewRow row in dgvInventory.Rows)
                    {
                        string st = row.Cells[statusIdx].Value?.ToString() ?? "";
                        row.Cells[statusIdx].Style.ForeColor = st switch
                        {
                            "Out of Stock" => Color.FromArgb(185,28,28),
                            "Low Stock"    => Color.FromArgb(180,110,0),
                            _              => Color.FromArgb(5,150,90)
                        };
                        row.Cells[statusIdx].Style.Font = new Font("Segoe UI",9f,FontStyle.Bold);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading inventory report:\n" + ex.Message); }
        }

        // ── USERS TAB ─────────────────────────────────────────
        private void BuildUsersTab()
        {
            tabUsers = new TabPage("👥  User Activity") { BackColor=Color.FromArgb(245,248,253), Padding=new Padding(10) };

            var pnlTool = new Panel { Location=new Point(10,10), Size=new Size(1030,46), BackColor=Color.White };

            var lblRole  = FL("Role Filter:", 10, 14);
            cboRoleFilter = new ComboBox { Location=new Point(85,10), Size=new Size(160,28),
                DropDownStyle=ComboBoxStyle.DropDownList, Font=new Font("Segoe UI",9.5f) };
            cboRoleFilter.Items.AddRange(new object[] { "All Roles", "admin", "user" });
            cboRoleFilter.SelectedIndex = 0;

            btnLoadUsers  = Btn("🔍 Load Report",    255, 10, 130, Color.FromArgb(30,58,95),    Color.White);
            btnLoadUsers.Click += BtnLoadUsers_Click;

            btnExportUsers = Btn("📥 Export to Excel", 395, 10, 160, Color.FromArgb(13,148,100), Color.White);
            btnExportUsers.Click += (s,e) => ExportUsers();

            lblUserCount = new Label { Text="", Location=new Point(565,14), AutoSize=true,
                Font=new Font("Segoe UI",8.5f,FontStyle.Italic), ForeColor=Color.Gray };

            pnlTool.Controls.AddRange(new Control[] { lblRole, cboRoleFilter, btnLoadUsers, btnExportUsers, lblUserCount });

            dgvUsers = MakeGrid(new Point(10,66), new Size(1030,490));
            dgvUsers.Anchor = AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;

            tabUsers.Controls.AddRange(new Control[] { pnlTool, dgvUsers });
        }

        private void BtnLoadUsers_Click(object sender, EventArgs e)
        {
            dgvUsers.Columns.Clear();
            dgvUsers.Rows.Clear();
            dtUsers = new DataTable();
            try
            {
                using var conn = DatabaseConnection.GetConnection();
                conn.Open();
                string role = cboRoleFilter.SelectedItem?.ToString() ?? "All Roles";
                string sql =
                    "SELECT u.user_id AS `User ID`, u.username AS `Username`, u.email AS `Email`, " +
                    "u.role AS `Role`, " +
                    "DATE_FORMAT(u.created_at,'%m/%d/%Y') AS `Registered Date`, " +
                    "CASE WHEN u.is_active=1 THEN 'Active' ELSE 'Inactive' END AS `Account Status`, " +
                    "COUNT(o.order_id) AS `Total Orders`, " +
                    "IFNULL(SUM(o.total_amount),0) AS `Total Spent (₱)` " +
                    "FROM user u LEFT JOIN `order` o ON u.user_id=o.user_id " +
                    (role != "All Roles" ? "WHERE u.role=@role " : "") +
                    "GROUP BY u.user_id ORDER BY u.user_id";
                using var cmd = new MySqlCommand(sql, conn);
                if (role != "All Roles") cmd.Parameters.AddWithValue("@role", role);
                using var adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(dtUsers);
                BindTableToGrid(dgvUsers, dtUsers);
                lblUserCount.Text = $"{dtUsers.Rows.Count} record(s)";

                int statusIdx = dgvUsers.Columns["Account Status"]?.Index ?? -1;
                if (statusIdx >= 0)
                    foreach (DataGridViewRow row in dgvUsers.Rows)
                    {
                        string st = row.Cells[statusIdx].Value?.ToString() ?? "";
                        row.Cells[statusIdx].Style.ForeColor = st == "Active" ? Color.FromArgb(5,150,90) : Color.FromArgb(185,28,28);
                        row.Cells[statusIdx].Style.Font = new Font("Segoe UI",9f,FontStyle.Bold);
                    }
            }
            catch (Exception ex) { MessageBox.Show("Error loading user report:\n" + ex.Message); }
        }

        // ══════════════════════════════════════════════════════
        // EXCEL EXPORT METHODS
        // ══════════════════════════════════════════════════════

        private void ExportSales()
        {
            if (dtSales == null || dtSales.Rows.Count == 0)
            { MessageBox.Show("Please load the sales report first.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string path = ChooseSavePath("Sales_Report");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using var pkg = new ExcelPackage();
                var ws = pkg.Workbook.Worksheets.Add("Sales Report");

                // Company header + logo
                WriteReportHeader(ws, "ECOMMERCE INFORMATION SYSTEM",
                    "Sales Transaction Summary Report",
                    $"Period: {dtpSalesFrom.Value:MM/dd/yyyy} – {dtpSalesTo.Value:MM/dd/yyyy}",
                    dtSales.Columns.Count);

                // Data
                WriteDataTable(ws, dtSales, startRow: 7);

                // Totals row
                int lastDataRow = 7 + dtSales.Rows.Count;
                int totalAmtCol = GetColumnIndex(dtSales, "Total Amount (₱)");
                if (totalAmtCol > 0)
                {
                    ws.Cells[lastDataRow+1, 1].Value = "GRAND TOTAL";
                    ws.Cells[lastDataRow+1, 1].Style.Font.Bold = true;
                    ws.Cells[lastDataRow+1, totalAmtCol].Formula = $"=SUM({ws.Cells[8, totalAmtCol].Address}:{ws.Cells[lastDataRow, totalAmtCol].Address})";
                    ws.Cells[lastDataRow+1, totalAmtCol].Style.Font.Bold = true;
                    ws.Cells[lastDataRow+1, totalAmtCol].Style.Numberformat.Format = "₱#,##0.00";
                    ws.Cells[lastDataRow+1, 1, lastDataRow+1, dtSales.Columns.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[lastDataRow+1, 1, lastDataRow+1, dtSales.Columns.Count].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(219,234,254));
                }

                // Signature
                WriteSignaturePlaceholder(ws, lastDataRow + 4, dtSales.Columns.Count);

                ws.Cells.AutoFitColumns(10, 50);

                // Sheet 2: Chart
                var wsChart = pkg.Workbook.Worksheets.Add("Chart");
                BuildSalesChart(wsChart, pkg, dtSales, ws.Name);

                pkg.SaveAs(new FileInfo(path));
                MessageBox.Show($"Sales report exported to:\n{path}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // TryOpenFile(path);
            }
            catch (Exception ex) { MessageBox.Show("Export error:\n" + ex.Message); }
        }

        private void ExportInventory()
        {
            if (dtInventory == null || dtInventory.Rows.Count == 0)
            { MessageBox.Show("Please load the inventory report first.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string path = ChooseSavePath("Inventory_Report");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using var pkg = new ExcelPackage();
                var ws = pkg.Workbook.Worksheets.Add("Inventory Report");

                WriteReportHeader(ws, "ECOMMERCE INFORMATION SYSTEM",
                    "Product Inventory Report",
                    $"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}",
                    dtInventory.Columns.Count);

                WriteDataTable(ws, dtInventory, startRow: 7);

                int lastDataRow = 7 + dtInventory.Rows.Count;
                int stockCol    = GetColumnIndex(dtInventory, "Current Stock");
                int soldCol     = GetColumnIndex(dtInventory, "Total Sold");
                if (stockCol > 0)
                {
                    ws.Cells[lastDataRow+1, 1].Value = "TOTALS";
                    ws.Cells[lastDataRow+1, 1].Style.Font.Bold = true;
                    ws.Cells[lastDataRow+1, stockCol].Formula = $"=SUM({ws.Cells[8, stockCol].Address}:{ws.Cells[lastDataRow, stockCol].Address})";
                    ws.Cells[lastDataRow+1, stockCol].Style.Font.Bold = true;
                    if (soldCol > 0)
                    {
                        ws.Cells[lastDataRow+1, soldCol].Formula = $"=SUM({ws.Cells[8, soldCol].Address}:{ws.Cells[lastDataRow, soldCol].Address})";
                        ws.Cells[lastDataRow+1, soldCol].Style.Font.Bold = true;
                    }
                    ws.Cells[lastDataRow+1, 1, lastDataRow+1, dtInventory.Columns.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[lastDataRow+1, 1, lastDataRow+1, dtInventory.Columns.Count].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(219,234,254));
                }

                WriteSignaturePlaceholder(ws, lastDataRow + 4, dtInventory.Columns.Count);
                ws.Cells.AutoFitColumns(10, 50);

                var wsChart = pkg.Workbook.Worksheets.Add("Chart");
                BuildInventoryChart(wsChart, pkg, dtInventory, ws.Name);

                pkg.SaveAs(new FileInfo(path));
                MessageBox.Show($"Inventory report exported to:\n{path}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // TryOpenFile(path);
            }
            catch (Exception ex) { MessageBox.Show("Export error:\n" + ex.Message); }
        }

        private void ExportUsers()
        {
            if (dtUsers == null || dtUsers.Rows.Count == 0)
            { MessageBox.Show("Please load the user report first.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string path = ChooseSavePath("UserActivity_Report");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using var pkg = new ExcelPackage();
                var ws = pkg.Workbook.Worksheets.Add("User Activity Report");

                WriteReportHeader(ws, "ECOMMERCE INFORMATION SYSTEM",
                    "User Activity Report",
                    $"Generated: {DateTime.Now:MM/dd/yyyy HH:mm}",
                    dtUsers.Columns.Count);

                WriteDataTable(ws, dtUsers, startRow: 7);

                int lastDataRow = 7 + dtUsers.Rows.Count;
                int spentCol    = GetColumnIndex(dtUsers, "Total Spent (₱)");
                int ordersCol   = GetColumnIndex(dtUsers, "Total Orders");
                if (spentCol > 0)
                {
                    ws.Cells[lastDataRow+1, 1].Value = "TOTALS";
                    ws.Cells[lastDataRow+1, 1].Style.Font.Bold = true;
                    ws.Cells[lastDataRow+1, spentCol].Formula = $"=SUM({ws.Cells[8, spentCol].Address}:{ws.Cells[lastDataRow, spentCol].Address})";
                    ws.Cells[lastDataRow+1, spentCol].Style.Font.Bold = true;
                    ws.Cells[lastDataRow+1, spentCol].Style.Numberformat.Format = "₱#,##0.00";
                    if (ordersCol > 0)
                    {
                        ws.Cells[lastDataRow+1, ordersCol].Formula = $"=SUM({ws.Cells[8, ordersCol].Address}:{ws.Cells[lastDataRow, ordersCol].Address})";
                        ws.Cells[lastDataRow+1, ordersCol].Style.Font.Bold = true;
                    }
                    ws.Cells[lastDataRow+1, 1, lastDataRow+1, dtUsers.Columns.Count].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[lastDataRow+1, 1, lastDataRow+1, dtUsers.Columns.Count].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(219,234,254));
                }

                WriteSignaturePlaceholder(ws, lastDataRow + 4, dtUsers.Columns.Count);
                ws.Cells.AutoFitColumns(10, 50);

                var wsChart = pkg.Workbook.Worksheets.Add("Chart");
                BuildUsersChart(wsChart, pkg, dtUsers, ws.Name);

                pkg.SaveAs(new FileInfo(path));
                MessageBox.Show($"User activity report exported to:\n{path}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // TryOpenFile(path);
            }
            catch (Exception ex) { MessageBox.Show("Export error:\n" + ex.Message); }
        }

        // ══════════════════════════════════════════════════════
        // SHARED EXCEL HELPERS
        // ══════════════════════════════════════════════════════

        private void WriteReportHeader(ExcelWorksheet ws, string company, string title, string subtitle, int colCount)
        {
            // Row 1: Company name (merged)
            ws.Cells[1, 1, 1, colCount].Merge = true;
            ws.Cells[1, 1].Value = company;
            ws.Cells[1, 1].Style.Font.Bold  = true;
            ws.Cells[1, 1].Style.Font.Size  = 16;
            ws.Cells[1, 1].Style.Font.Name  = "Arial";
            ws.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30,58,95));
            ws.Row(1).Height = 28;

            // Row 2: Logo placeholder + report title
            ws.Cells[2, 1, 2, 2].Merge = true;
            ws.Cells[2, 1].Value = "🛒 E-COMM IS";
            ws.Cells[2, 1].Style.Font.Bold  = true;
            ws.Cells[2, 1].Style.Font.Size  = 11;
            ws.Cells[2, 1].Style.Font.Name  = "Arial";
            ws.Cells[2, 1].Style.Font.Color.SetColor(Color.FromArgb(30,58,95));

            ws.Cells[2, 3, 2, colCount].Merge = true;
            ws.Cells[2, 3].Value = title;
            ws.Cells[2, 3].Style.Font.Bold  = true;
            ws.Cells[2, 3].Style.Font.Size  = 13;
            ws.Cells[2, 3].Style.Font.Name  = "Arial";
            ws.Cells[2, 3].Style.Font.Color.SetColor(Color.FromArgb(30,58,95));
            ws.Cells[2, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Row(2).Height = 22;

            // Row 3: Subtitle
            ws.Cells[3, 1, 3, colCount].Merge = true;
            ws.Cells[3, 1].Value = subtitle;
            ws.Cells[3, 1].Style.Font.Italic = true;
            ws.Cells[3, 1].Style.Font.Name   = "Arial";
            ws.Cells[3, 1].Style.Font.Size   = 10;
            ws.Cells[3, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Row 4: Generated by
            ws.Cells[4, 1, 4, colCount].Merge = true;
            ws.Cells[4, 1].Value = $"Generated by: {SessionManager.Username}  |  {DateTime.Now:dddd, MMMM d, yyyy  hh:mm tt}";
            ws.Cells[4, 1].Style.Font.Size   = 9;
            ws.Cells[4, 1].Style.Font.Name   = "Arial";
            ws.Cells[4, 1].Style.Font.Color.SetColor(Color.Gray);
            ws.Cells[4, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Row 5: blank spacer
            ws.Row(5).Height = 8;
        }

        private void WriteDataTable(ExcelWorksheet ws, DataTable dt, int startRow)
        {
            int headerRow = startRow;

            // Column headers
            for (int c = 0; c < dt.Columns.Count; c++)
            {
                ws.Cells[headerRow, c+1].Value = dt.Columns[c].ColumnName;
                ws.Cells[headerRow, c+1].Style.Font.Bold   = true;
                ws.Cells[headerRow, c+1].Style.Font.Name   = "Arial";
                ws.Cells[headerRow, c+1].Style.Font.Size   = 10;
                ws.Cells[headerRow, c+1].Style.Font.Color.SetColor(Color.White);
                ws.Cells[headerRow, c+1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[headerRow, c+1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[headerRow, c+1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30,58,95));
                ws.Cells[headerRow, c+1].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.White);
            }

            // Data rows
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                bool alt = r % 2 == 1;
                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    var cell = ws.Cells[headerRow+1+r, c+1];
                    object val = dt.Rows[r][c];
                    cell.Value = val is DBNull ? null : val;
                    cell.Style.Font.Name = "Arial";
                    cell.Style.Font.Size = 9;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Hair, Color.FromArgb(200,210,220));
                    if (alt) { cell.Style.Fill.PatternType = ExcelFillStyle.Solid; cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(245,248,253)); }

                    // Number formatting
                    string colName = dt.Columns[c].ColumnName;
                    if (colName.Contains("₱") || colName.Contains("Amount") || colName.Contains("Spent") || colName.Contains("Price"))
                        cell.Style.Numberformat.Format = "#,##0.00";
                    else if (dt.Rows[r][c] is int || dt.Rows[r][c] is long)
                        cell.Style.Numberformat.Format = "#,##0";
                }
            }
        }

        private void WriteSignaturePlaceholder(ExcelWorksheet ws, int row, int colCount)
        {
            int midLeft = Math.Max(1, colCount - 3);

            // Left: "Prepared by"
            ws.Cells[row,   1].Value = "Prepared by:";
            ws.Cells[row,   1].Style.Font.Bold = true;
            ws.Cells[row,   1].Style.Font.Name = "Arial";
            ws.Cells[row+2, 1, row+2, 3].Merge = true;
            ws.Cells[row+2, 1].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            ws.Cells[row+3, 1].Value = SessionManager.Username;
            ws.Cells[row+3, 1].Style.Font.Italic = true;
            ws.Cells[row+3, 1].Style.Font.Name = "Arial";
            ws.Cells[row+4, 1].Value = "Report Preparer / Encoder";
            ws.Cells[row+4, 1].Style.Font.Name = "Arial";
            ws.Cells[row+4, 1].Style.Font.Size  = 9;
            ws.Cells[row+4, 1].Style.Font.Color.SetColor(Color.Gray);

            // Right: "Approved by"
            ws.Cells[row,   midLeft].Value = "Approved by:";
            ws.Cells[row,   midLeft].Style.Font.Bold = true;
            ws.Cells[row,   midLeft].Style.Font.Name = "Arial";
            ws.Cells[row+2, midLeft, row+2, colCount].Merge = true;
            ws.Cells[row+2, midLeft].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            ws.Cells[row+3, midLeft].Value = "_________________________";
            ws.Cells[row+4, midLeft].Value = "Authorized Signatory / Manager";
            ws.Cells[row+4, midLeft].Style.Font.Name = "Arial";
            ws.Cells[row+4, midLeft].Style.Font.Size  = 9;
            ws.Cells[row+4, midLeft].Style.Font.Color.SetColor(Color.Gray);
        }

        // ── Chart builders ────────────────────────────────────
        private void BuildSalesChart(ExcelWorksheet wsChart, ExcelPackage pkg, DataTable dt, string dataSheetName)
        {
            // Build a summary: status counts from data
            var statusCounts = new Dictionary<string, int>();
            int statusColIdx = dt.Columns.IndexOf("Status");
            int amtColIdx    = dt.Columns.IndexOf("Total Amount (₱)");
            if (statusColIdx < 0) { wsChart.Cells[1,1].Value = "No chart data available."; return; }

            foreach (DataRow row in dt.Rows)
            {
                string st = row[statusColIdx]?.ToString() ?? "Unknown";
                statusCounts.TryGetValue(st, out int cnt);
                statusCounts[st] = cnt + 1;
            }

            // Write summary data onto chart sheet
            wsChart.Cells[1,1].Value = "Order Status";
            wsChart.Cells[1,2].Value = "Count";
            wsChart.Cells[1,1].Style.Font.Bold = true;
            wsChart.Cells[1,2].Style.Font.Bold = true;
            int r = 2;
            foreach (var kv in statusCounts)
            {
                wsChart.Cells[r,1].Value = kv.Key;
                wsChart.Cells[r,2].Value = kv.Value;
                r++;
            }

            int lastRow = r - 1;
            var chart = wsChart.Drawings.AddChart("SalesStatusChart", eChartType.ColumnClustered);
            chart.Title.Text = "Sales Orders by Status";
            chart.SetPosition(3, 0, 0, 0);
            chart.SetSize(600, 350);
            chart.Series.Add(wsChart.Cells[2,2,lastRow,2], wsChart.Cells[2,1,lastRow,1]);
            chart.Series[0].Header = "Order Count";
            chart.XAxis.Title.Text = "Status";
            chart.YAxis.Title.Text = "Number of Orders";
            chart.Legend.Remove();

            // Also add a second chart: Revenue by status
            if (amtColIdx >= 0)
            {
                var amtByStatus = new Dictionary<string, decimal>();
                foreach (DataRow row in dt.Rows)
                {
                    string st  = row[statusColIdx]?.ToString() ?? "Unknown";
                    decimal amt = row[amtColIdx] is DBNull ? 0 : Convert.ToDecimal(row[amtColIdx]);
                    amtByStatus.TryGetValue(st, out decimal existing);
                    amtByStatus[st] = existing + amt;
                }
                wsChart.Cells[1,4].Value = "Order Status"; wsChart.Cells[1,4].Style.Font.Bold = true;
                wsChart.Cells[1,5].Value = "Revenue (₱)";  wsChart.Cells[1,5].Style.Font.Bold = true;
                int r2 = 2;
                foreach (var kv in amtByStatus)
                {
                    wsChart.Cells[r2,4].Value = kv.Key;
                    wsChart.Cells[r2,5].Value = (double)kv.Value;
                    wsChart.Cells[r2,5].Style.Numberformat.Format = "#,##0.00";
                    r2++;
                }
                int lastRow2 = r2 - 1;
                var chart2 = wsChart.Drawings.AddChart("SalesRevenueChart", eChartType.Pie);
                chart2.Title.Text = "Revenue Distribution by Status (₱)";
                chart2.SetPosition(25, 0, 0, 0);
                chart2.SetSize(600, 300);
                chart2.Series.Add(wsChart.Cells[2,5,lastRow2,5], wsChart.Cells[2,4,lastRow2,4]);
                chart2.Series[0].Header = "Revenue";
            }
        }

        private void BuildInventoryChart(ExcelWorksheet wsChart, ExcelPackage pkg, DataTable dt, string dataSheetName)
        {
            int nameCol  = dt.Columns.IndexOf("Product Name");
            int stockCol = dt.Columns.IndexOf("Current Stock");
            int soldCol  = dt.Columns.IndexOf("Total Sold");
            if (nameCol < 0 || stockCol < 0) { wsChart.Cells[1,1].Value = "No chart data available."; return; }

            // Write top 10 by stock to chart sheet
            wsChart.Cells[1,1].Value = "Product";
            wsChart.Cells[1,2].Value = "Current Stock";
            wsChart.Cells[1,3].Value = "Total Sold";
            wsChart.Cells[1,1].Style.Font.Bold = true;
            wsChart.Cells[1,2].Style.Font.Bold = true;
            wsChart.Cells[1,3].Style.Font.Bold = true;

            int rows = Math.Min(dt.Rows.Count, 10);
            for (int r = 0; r < rows; r++)
            {
                wsChart.Cells[r+2,1].Value = dt.Rows[r][nameCol]?.ToString();
                wsChart.Cells[r+2,2].Value = dt.Rows[r][stockCol] is DBNull ? 0 : Convert.ToInt32(dt.Rows[r][stockCol]);
                if (soldCol >= 0)
                    wsChart.Cells[r+2,3].Value = dt.Rows[r][soldCol] is DBNull ? 0 : Convert.ToInt64(dt.Rows[r][soldCol]);
            }

            var chart = wsChart.Drawings.AddChart("InventoryChart", eChartType.BarClustered);
            chart.Title.Text = "Product Stock Levels (Top 10)";
            chart.SetPosition(rows + 3, 0, 0, 0);
            chart.SetSize(680, 380);
            var series1 = chart.Series.Add(wsChart.Cells[2,2,rows+1,2], wsChart.Cells[2,1,rows+1,1]);
            series1.Header = "Current Stock";
            if (soldCol >= 0)
            {
                var series2 = chart.Series.Add(wsChart.Cells[2,3,rows+1,3], wsChart.Cells[2,1,rows+1,1]);
                series2.Header = "Total Sold";
            }
            chart.YAxis.Title.Text = "Quantity";
            chart.XAxis.Title.Text = "Product";
        }

        private void BuildUsersChart(ExcelWorksheet wsChart, ExcelPackage pkg, DataTable dt, string dataSheetName)
        {
            int nameCol  = dt.Columns.IndexOf("Username");
            int spentCol = dt.Columns.IndexOf("Total Spent (₱)");
            int ordersCol= dt.Columns.IndexOf("Total Orders");
            if (nameCol < 0 || spentCol < 0) { wsChart.Cells[1,1].Value = "No chart data available."; return; }

            // Top 10 by spending
            var rows = new List<DataRow>(dt.Select("", "Total Spent (₱) DESC"));
            int topN = Math.Min(rows.Count, 10);

            wsChart.Cells[1,1].Value = "Username";
            wsChart.Cells[1,2].Value = "Total Spent (₱)";
            wsChart.Cells[1,3].Value = "Total Orders";
            wsChart.Cells[1,1].Style.Font.Bold = true;
            wsChart.Cells[1,2].Style.Font.Bold = true;
            wsChart.Cells[1,3].Style.Font.Bold = true;

            for (int r = 0; r < topN; r++)
            {
                wsChart.Cells[r+2,1].Value = rows[r][nameCol]?.ToString();
                wsChart.Cells[r+2,2].Value = rows[r][spentCol] is DBNull ? 0 : (double)Convert.ToDecimal(rows[r][spentCol]);
                wsChart.Cells[r+2,2].Style.Numberformat.Format = "#,##0.00";
                if (ordersCol >= 0)
                    wsChart.Cells[r+2,3].Value = rows[r][ordersCol] is DBNull ? 0 : Convert.ToInt32(rows[r][ordersCol]);
            }

            var chart = wsChart.Drawings.AddChart("TopSpendersChart", eChartType.ColumnClustered);
            chart.Title.Text = "Top 10 Customers by Total Spending (₱)";
            chart.SetPosition(topN + 3, 0, 0, 0);
            chart.SetSize(680, 380);
            var s1 = chart.Series.Add(wsChart.Cells[2,2,topN+1,2], wsChart.Cells[2,1,topN+1,1]);
            s1.Header = "Total Spent (₱)";
            chart.YAxis.Title.Text = "Amount (₱)";
            chart.XAxis.Title.Text = "Customer";

            // Role distribution pie
            var roleCount = new Dictionary<string,int>();
            int roleColIdx = dt.Columns.IndexOf("Role");
            if (roleColIdx >= 0)
                foreach (DataRow row in dt.Rows)
                {
                    string role = row[roleColIdx]?.ToString() ?? "user";
                    roleCount.TryGetValue(role, out int cnt); roleCount[role] = cnt + 1;
                }
            wsChart.Cells[1,5].Value = "Role"; wsChart.Cells[1,5].Style.Font.Bold = true;
            wsChart.Cells[1,6].Value = "Count"; wsChart.Cells[1,6].Style.Font.Bold = true;
            int ri = 2;
            foreach (var kv in roleCount) { wsChart.Cells[ri,5].Value=kv.Key; wsChart.Cells[ri,6].Value=kv.Value; ri++; }
            if (ri > 2)
            {
                var chart2 = wsChart.Drawings.AddChart("RoleDistChart", eChartType.Pie);
                chart2.Title.Text = "User Role Distribution";
                chart2.SetPosition(topN + 28, 0, 0, 0);
                chart2.SetSize(400, 280);
                chart2.Series.Add(wsChart.Cells[2,6,ri-1,6], wsChart.Cells[2,5,ri-1,5]);
                chart2.Series[0].Header = "Users";
            }
        }

        // ── Utilities ─────────────────────────────────────────
        private void BindTableToGrid(DataGridView dgv, DataTable dt)
        {
            dgv.Columns.Clear();
            dgv.Rows.Clear();
            foreach (DataColumn dc in dt.Columns)
            {
                dgv.Columns.Add(dc.ColumnName, dc.ColumnName);
            }
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30,58,95);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI",9.5f,FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            foreach (DataRow row in dt.Rows)
                dgv.Rows.Add(row.ItemArray);
        }

        private int GetColumnIndex(DataTable dt, string name)
        {
            for (int i = 0; i < dt.Columns.Count; i++)
                if (dt.Columns[i].ColumnName == name) return i + 1;
            return -1;
        }

        private string ChooseSavePath(string prefix)
        {
            using var dlg = new SaveFileDialog
            {
                Title      = "Export Report to Excel",
                Filter     = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName   = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                DefaultExt = "xlsx"
            };
            return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : null;
        }

        private static void TryOpenFile(string path)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); }
            catch { }
        }

        // Factory helpers
        private Label FL(string t, int x, int y) =>
            new Label { Text=t, Location=new Point(x,y), AutoSize=true, Font=new Font("Segoe UI",9f), ForeColor=Color.FromArgb(60,70,80) };
        private Button Btn(string t, int x, int y, int w, Color bg, Color fg)
        {
            var b = new Button { Text=t, Location=new Point(x,y), Size=new Size(w,32),
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
