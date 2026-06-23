using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class FormDashboard : Form
    {
        private string userToken; 
        private string myCurrentRole = "user";
        private string? currentEditUserId = null; 

        // CÁC BIẾN UI CHUNG
        private Panel pnlDashboard = null!, pnlProfile = null!, pnlManageContent = null!, pnlAddEditEmployee = null!;
        private Button btnManageEmp = null!;
        private Button btnMenuAttendance = null!;
        private TextBox txtEmpUserName = null!, txtEmpEmail = null!, txtEmpPhone = null!, txtEmpPassword = null!;
        private TextBox txtSearchEmp = null!;
        private Label lblEmpPassword = null!, lblAddEditTitle = null!;
        private ComboBox cbEmpRole = null!;
        private Button btnSaveEmp = null!, btnDeleteEmp = null!, btnIssueKey = null!;
        
        private Label lblProName = null!, lblProRole = null!, lblProDate = null!;
        private TextBox txtEditEmail = null!, txtEditPhone = null!, txtEditAddress = null!;
        private Button btnEditProfile = null!;
        private DataGridView dgvEmployees = null!;
        private bool isEditMode = false;

        // CÁC BIẾN CHO THỐNG KÊ DASHBOARD
        private Label lblTotalEmp = null!, lblTotalAdmin = null!, lblTotalUser = null!;
        private Panel pnlStat1 = null!, pnlStat2 = null!, pnlStat3 = null!;

        public FormDashboard(string token)
        {
            userToken = token; 
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Bảng Điều Khiển - Premium";
            this.Size = new Size(520, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 11F);

            this.FormClosed += (s, e) => Application.Exit();
            
            // Hàm vẽ Dashboard nằm trong file này
            VeGiaoDienDashboard();
            // Hàm vẽ Chấm Công sẽ được gọi từ file FormDashboard.Attendance.cs
            VeGiaoDienChamCong();
            
            this.Load += FormDashboard_Load; 
        }

        private void VeGiaoDienDashboard()
        {
            Color primaryBlue = Color.FromArgb(0, 102, 204), primaryGreen = Color.FromArgb(32, 161, 68);
            Color primaryOrange = Color.FromArgb(255, 140, 0), warningYellow = Color.FromArgb(255, 193, 7);
            Color lightGray = Color.FromArgb(245, 247, 250), darkGray = Color.FromArgb(80, 80, 80), dangerRed = Color.FromArgb(220, 53, 69);
            int startX = 40, width = 400;

            // 1. DASHBOARD
            pnlDashboard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Visible = true };
            Label lblDashTitle = new Label { Text = "BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = primaryBlue, Location = new Point(startX, 30), Width = width, Height = 80, TextAlign = ContentAlignment.MiddleCenter };
            
            pnlStat1 = new Panel { BackColor = primaryBlue, Width = 110, Height = 80, Location = new Point(startX, 120), BorderStyle = BorderStyle.FixedSingle, Visible = false };
            pnlStat1.Controls.Add(new Label { Text = "TỔNG SỐ", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 10) });
            lblTotalEmp = new Label { Text = "-", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 30), AutoSize = true };
            pnlStat1.Controls.Add(lblTotalEmp);

            pnlStat2 = new Panel { BackColor = warningYellow, Width = 110, Height = 80, Location = new Point(startX + 125, 120), BorderStyle = BorderStyle.FixedSingle, Visible = false };
            pnlStat2.Controls.Add(new Label { Text = "QUẢN TRỊ", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 10) });
            lblTotalAdmin = new Label { Text = "-", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 30), AutoSize = true };
            pnlStat2.Controls.Add(lblTotalAdmin);

            pnlStat3 = new Panel { BackColor = primaryGreen, Width = 110, Height = 80, Location = new Point(startX + 250, 120), BorderStyle = BorderStyle.FixedSingle, Visible = false };
            pnlStat3.Controls.Add(new Label { Text = "NHÂN VIÊN", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 10) });
            lblTotalUser = new Label { Text = "-", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 30), AutoSize = true };
            pnlStat3.Controls.Add(lblTotalUser);

            EventHandler clickTongSo = async (s, e) => { SwitchPanel(pnlManageContent); txtSearchEmp.Text = ""; await LoadEmployeeListAsync(); };
            pnlStat1.Cursor = Cursors.Hand; pnlStat1.Click += clickTongSo; foreach (Control c in pnlStat1.Controls) { c.Cursor = Cursors.Hand; c.Click += clickTongSo; }

            EventHandler clickQuanTri = async (s, e) => { SwitchPanel(pnlManageContent); await LoadEmployeeListAsync(); txtSearchEmp.Text = "admin"; };
            pnlStat2.Cursor = Cursors.Hand; pnlStat2.Click += clickQuanTri; foreach (Control c in pnlStat2.Controls) { c.Cursor = Cursors.Hand; c.Click += clickQuanTri; }

            EventHandler clickNhanVien = async (s, e) => { SwitchPanel(pnlManageContent); await LoadEmployeeListAsync(); txtSearchEmp.Text = "user"; };
            pnlStat3.Cursor = Cursors.Hand; pnlStat3.Click += clickNhanVien; foreach (Control c in pnlStat3.Controls) { c.Cursor = Cursors.Hand; c.Click += clickNhanVien; }

            Button btnViewProfile = new Button { Text = "👤 HỒ SƠ CỦA TÔI", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Location = new Point(startX, 230), Width = width, Height = 60, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnViewProfile.FlatAppearance.BorderSize = 0;
            btnViewProfile.Click += async (s, e) => { SwitchPanel(pnlProfile); await LoadMyProfileAsync(); };

            btnMenuAttendance = new Button { Text = "⏰ CHẤM CÔNG HÀNG NGÀY", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(23, 162, 184), Location = new Point(startX, 300), Width = width, Height = 60, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnMenuAttendance.FlatAppearance.BorderSize = 0;
            btnMenuAttendance.Click += (s, e) => { SwitchPanel(pnlAttendance); };

            btnManageEmp = new Button { Text = "👥 QUẢN LÝ NHÂN VIÊN", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryOrange, Location = new Point(startX, 370), Width = width, Height = 60, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnManageEmp.FlatAppearance.BorderSize = 0; btnManageEmp.Visible = false; 
            btnManageEmp.Click += async (s, e) => { SwitchPanel(pnlManageContent); await LoadEmployeeListAsync(); };

            Button btnLogoutDash = new Button { Text = "ĐĂNG XUẤT", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Gray, Location = new Point(startX, 530), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnLogoutDash.FlatAppearance.BorderSize = 0;
            btnLogoutDash.Click += (s, e) => { Application.Restart(); };

            pnlDashboard.Controls.AddRange(new Control[] { lblDashTitle, pnlStat1, pnlStat2, pnlStat3, btnViewProfile, btnMenuAttendance, btnManageEmp, btnLogoutDash });

            // 2. PROFILE CARD
            pnlProfile = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false };
            Label lblProfileTitle = new Label { Text = "THẺ NHÂN VIÊN", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = primaryGreen, Location = new Point(0, 30), Width = 500, Height = 50, TextAlign = ContentAlignment.MiddleCenter };
            Panel pnlCard = new Panel { Width = 400, Height = 480, BackColor = Color.White, Location = new Point(startX, 90), BorderStyle = BorderStyle.FixedSingle };
            lblProName = new Label { Text = "Đang tải...", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = darkGray, AutoSize = false, Width = 400, Height = 40, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 140) };
            lblProRole = new Label { Text = "ROLE", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryOrange, AutoSize = false, Width = 120, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(140, 190) };
            pnlCard.Controls.Add(new Label { Text = "✉️ Email:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(20, 240), AutoSize = true });
            txtEditEmail = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(110, 238), Width = 260, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White };
            pnlCard.Controls.Add(new Label { Text = "📞 SĐT:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(20, 280), AutoSize = true });
            txtEditPhone = new TextBox { Text = "Chưa cập nhật", Font = new Font("Segoe UI", 11F), Location = new Point(110, 278), Width = 260, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White };
            pnlCard.Controls.Add(new Label { Text = "📍 Địa chỉ:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(20, 320), AutoSize = true });
            txtEditAddress = new TextBox { Text = "Chưa cập nhật", Font = new Font("Segoe UI", 11F), Location = new Point(110, 318), Width = 260, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White };
            lblProDate = new Label { Text = "Tham gia: ...", Font = new Font("Segoe UI", 10F, FontStyle.Italic), ForeColor = Color.Gray, AutoSize = false, Width = 400, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 370) };
            btnEditProfile = new Button { Text = "✍️ CHỈNH SỬA HỒ SƠ", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Location = new Point(100, 415), Width = 200, Height = 40, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnEditProfile.FlatAppearance.BorderSize = 0; btnEditProfile.Click += BtnEditProfile_Click; 
            pnlCard.Controls.AddRange(new Control[] { lblProName, lblProRole, txtEditEmail, txtEditPhone, txtEditAddress, lblProDate, btnEditProfile });
            Button btnBackDash2 = new Button { Text = "QUAY LẠI BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryBlue, Location = new Point(startX, 600), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackDash2.FlatAppearance.BorderSize = 0; btnBackDash2.Click += (s, e) => { SwitchPanel(pnlDashboard); };
            pnlProfile.Controls.AddRange(new Control[] { lblProfileTitle, pnlCard, btnBackDash2 });

            // 3. QUẢN LÝ NHÂN SỰ
            pnlManageContent = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false };
            Label lblManageTitle = new Label { Text = "DANH SÁCH NHÂN VIÊN", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = primaryOrange, Location = new Point(0, 30), Width = 500, Height = 50, TextAlign = ContentAlignment.MiddleCenter };
            
            txtSearchEmp = new TextBox { Location = new Point(startX, 90), Width = width, Font = new Font("Segoe UI", 12F), PlaceholderText = "🔍 Nhập Tên, Email hoặc SĐT để tìm nhanh..." };
            txtSearchEmp.TextChanged += TxtSearchEmp_TextChanged;

            dgvEmployees = new DataGridView { Location = new Point(startX, 135), Size = new Size(width, 335), BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowTemplate = { Height = 35 } };
            dgvEmployees.Columns.Add("Id", "ID"); dgvEmployees.Columns["Id"].Visible = false; 
            dgvEmployees.Columns.Add("UserName", "TÀI KHOẢN"); dgvEmployees.Columns.Add("Email", "EMAIL"); dgvEmployees.Columns.Add("PhoneNumber", "SĐT");
            dgvEmployees.CellDoubleClick += DgvEmployees_CellDoubleClick; 
            
            Button btnAddEmp = new Button { Text = "➕ THÊM NHÂN VIÊN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Location = new Point(startX, 490), Width = width, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnAddEmp.FlatAppearance.BorderSize = 0; btnAddEmp.Click += BtnAddEmp_Click; 
            Button btnBackDash3 = new Button { Text = "QUAY LẠI BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryBlue, Location = new Point(startX, 600), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackDash3.FlatAppearance.BorderSize = 0; btnBackDash3.Click += (s, e) => { SwitchPanel(pnlDashboard); };
            
            pnlManageContent.Controls.AddRange(new Control[] { lblManageTitle, txtSearchEmp, dgvEmployees, btnAddEmp, btnBackDash3 });

            // 4. KHUNG THÊM / SỬA NHÂN VIÊN
            pnlAddEditEmployee = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false };
            lblAddEditTitle = new Label { Text = "THÔNG TIN CHI TIẾT", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = primaryOrange, Location = new Point(0, 30), Width = 500, Height = 50, TextAlign = ContentAlignment.MiddleCenter };
            Panel pnlFormEdit = new Panel { Width = 400, Height = 280, BackColor = Color.White, Location = new Point(startX, 90), BorderStyle = BorderStyle.FixedSingle };
            int formY = 15;
            pnlFormEdit.Controls.Add(new Label { Text = "Tài khoản:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(15, formY), AutoSize = true });
            txtEmpUserName = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(115, formY - 2), Width = 265, BorderStyle = BorderStyle.FixedSingle };
            pnlFormEdit.Controls.Add(new Label { Text = "Email:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(15, formY += 45), AutoSize = true });
            txtEmpEmail = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(115, formY - 2), Width = 265, BorderStyle = BorderStyle.FixedSingle };
            pnlFormEdit.Controls.Add(new Label { Text = "Số ĐT:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(15, formY += 45), AutoSize = true });
            txtEmpPhone = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(115, formY - 2), Width = 265, BorderStyle = BorderStyle.FixedSingle };
            lblEmpPassword = new Label { Text = "Mật khẩu:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(15, formY += 45), AutoSize = true };
            txtEmpPassword = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(115, formY - 2), Width = 265, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };
            pnlFormEdit.Controls.Add(new Label { Text = "Quyền:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(15, formY += 45), AutoSize = true });
            cbEmpRole = new ComboBox { Font = new Font("Segoe UI", 11F), Location = new Point(115, formY - 2), Width = 265, DropDownStyle = ComboBoxStyle.DropDownList };
            Label lblHint = new Label { Text = "*Mật khẩu phải có hoa, thường, số, ký tự đặc biệt", Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = Color.Gray, Location = new Point(115, formY + 30), Width = 265, Height= 20 };
            pnlFormEdit.Controls.AddRange(new Control[] { txtEmpUserName, txtEmpEmail, txtEmpPhone, lblEmpPassword, txtEmpPassword, cbEmpRole, lblHint });

            btnSaveEmp = new Button { Text = "💾 LƯU THÔNG TIN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Location = new Point(startX, 390), Width = width, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSaveEmp.FlatAppearance.BorderSize = 0; btnSaveEmp.Click += BtnSaveEmp_Click;
            btnIssueKey = new Button { Text = "🔑 CẤP LẠI KEY", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = warningYellow, Location = new Point(startX, 450), Width = 190, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnIssueKey.FlatAppearance.BorderSize = 0; btnIssueKey.Click += BtnIssueKey_Click;
            btnDeleteEmp = new Button { Text = "🗑️ XÓA", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = dangerRed, Location = new Point(startX + 210, 450), Width = 190, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnDeleteEmp.FlatAppearance.BorderSize = 0; btnDeleteEmp.Click += BtnDeleteEmp_Click;
            Button btnCancelEdit = new Button { Text = "❌ HỦY BỎ / QUAY LẠI", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = darkGray, BackColor = Color.LightGray, Location = new Point(startX, 600), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancelEdit.FlatAppearance.BorderSize = 0; btnCancelEdit.Click += (s, e) => { SwitchPanel(pnlManageContent); };
            pnlAddEditEmployee.Controls.AddRange(new Control[] { lblAddEditTitle, pnlFormEdit, btnSaveEmp, btnIssueKey, btnDeleteEmp, btnCancelEdit });

            this.Controls.Add(pnlAddEditEmployee); 
            this.Controls.Add(pnlManageContent); 
            this.Controls.Add(pnlProfile); 
            this.Controls.Add(pnlDashboard);
        }

        private void SwitchPanel(Panel target)
        {
            pnlDashboard.Visible = false; pnlProfile.Visible = false; pnlManageContent.Visible = false; pnlAddEditEmployee.Visible = false;
            if(pnlAttendance != null) pnlAttendance.Visible = false; 
            target.Visible = true;
            target.BringToFront();
        }

        private async void FormDashboard_Load(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(userToken)) return;
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (s, c, ch, ssl) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
                    
                    HttpResponseMessage response = await client.GetAsync("https://localhost:44387/api/app/my-profile/my-profile");
                    if (response.IsSuccessStatusCode)
                    {
                        var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                        myCurrentRole = data.GetProperty("roles").GetString()?.ToLower() ?? "user";
                        
                        if (myCurrentRole == "admin" || myCurrentRole == "superadmin") 
                        {
                            btnManageEmp.Visible = true;
                            pnlStat1.Visible = true;
                            pnlStat2.Visible = true;
                            pnlStat3.Visible = true;
                        }  
                    }

                    HttpResponseMessage statsRes = await client.GetAsync("https://localhost:44387/api/app/employee/dashboard-stats");
                    if (statsRes.IsSuccessStatusCode)
                    {
                        var statsData = JsonSerializer.Deserialize<JsonElement>(await statsRes.Content.ReadAsStringAsync());
                        lblTotalEmp.Text = statsData.GetProperty("totalEmployees").GetInt32().ToString();
                        lblTotalAdmin.Text = statsData.GetProperty("totalAdmins").GetInt32().ToString();
                        lblTotalUser.Text = statsData.GetProperty("totalUsers").GetInt32().ToString();
                    }
                }
            } catch { }
        }
    }
}