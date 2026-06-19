using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class FormDashboard : Form
    {
        private string userToken; 

        // Các biến giao diện chính
        private Panel pnlDashboard = null!;
        private Panel pnlProfile = null!;
        private Panel pnlManageContent = null!;
        private Button btnManageEmp = null!; // 👉 Khai báo nút Quản lý
        
        // BIẾN CHO MÀN HÌNH THÊM/SỬA NHÂN VIÊN
        private Panel pnlAddEditEmployee = null!;
        private TextBox txtEmpUserName = null!;
        private TextBox txtEmpEmail = null!;
        private TextBox txtEmpPhone = null!;
        private TextBox txtEmpPassword = null!; 
        private Label lblEmpPassword = null!;
        private ComboBox cbEmpRole = null!;
        private Label lblAddEditTitle = null!;
        private Button btnSaveEmp = null!;
        private Button btnDeleteEmp = null!;
        private Button btnIssueKey = null!;
        private string? currentEditUserId = null; 

        // Các biến thẻ nhân viên
        private Label lblProName = null!;
        private Label lblProRole = null!;
        private Label lblProDate = null!;
        private TextBox txtEditEmail = null!;
        private TextBox txtEditPhone = null!;
        private TextBox txtEditAddress = null!;
        private Button btnEditProfile = null!;
        private DataGridView dgvEmployees = null!;
        private bool isEditMode = false;

        public FormDashboard(string token)
        {
            userToken = token; 
            
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Bảng Điều Khiển - Premium";
            this.Size = new Size(500, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 11F);

            this.FormClosed += (s, e) => Application.Exit();
            
            VeGiaoDienDashboard();
            
            // Kích hoạt sự kiện kiểm tra quyền khi mở Form
            this.Load += FormDashboard_Load; 
        }

        private void VeGiaoDienDashboard()
        {
            Color primaryBlue = Color.FromArgb(0, 102, 204);
            Color primaryGreen = Color.FromArgb(32, 161, 68);
            Color primaryOrange = Color.FromArgb(255, 140, 0);
            Color lightGray = Color.FromArgb(245, 247, 250);
            Color darkGray = Color.FromArgb(80, 80, 80);
            Color dangerRed = Color.FromArgb(220, 53, 69);
            Color warningYellow = Color.FromArgb(255, 193, 7);

            int startX = 40;
            int width = 400;

            // ==========================================
            // 1. DASHBOARD
            // ==========================================
            pnlDashboard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Visible = true };
            Label lblDashTitle = new Label { Text = "BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 24F, FontStyle.Bold), ForeColor = primaryBlue, Location = new Point(startX, 40), Width = width, Height = 80, TextAlign = ContentAlignment.MiddleCenter };
            
            Button btnViewProfile = new Button { Text = "👤 HỒ SƠ CỦA TÔI", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Location = new Point(startX, 150), Width = width, Height = 60, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnViewProfile.FlatAppearance.BorderSize = 0;
            btnViewProfile.Click += async (s, e) => { SwitchPanel(pnlProfile); await LoadMyProfileAsync(); };

            // 👉 NÚT QUẢN LÝ NHÂN VIÊN (BỊ ẨN MẶC ĐỊNH)
            btnManageEmp = new Button { Text = "👥 QUẢN LÝ NHÂN VIÊN", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryOrange, Location = new Point(startX, 230), Width = width, Height = 60, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnManageEmp.FlatAppearance.BorderSize = 0;
            btnManageEmp.Visible = false; // Ẩn mặc định
            btnManageEmp.Click += async (s, e) => { SwitchPanel(pnlManageContent); await LoadEmployeeListAsync(); };

            Button btnLogoutDash = new Button { Text = "ĐĂNG XUẤT", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Gray, Location = new Point(startX, 530), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnLogoutDash.FlatAppearance.BorderSize = 0;
            btnLogoutDash.Click += (s, e) => { Application.Restart(); };

            pnlDashboard.Controls.AddRange(new Control[] { lblDashTitle, btnViewProfile, btnManageEmp, btnLogoutDash });

            // ==========================================
            // 2. PROFILE CARD
            // ==========================================
            pnlProfile = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false };
            Label lblProfileTitle = new Label { Text = "THẺ NHÂN VIÊN", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = primaryGreen, Location = new Point(0, 30), Width = 500, Height = 50, TextAlign = ContentAlignment.MiddleCenter };

            Panel pnlCard = new Panel { Width = 400, Height = 480, BackColor = Color.White, Location = new Point(startX, 90), BorderStyle = BorderStyle.FixedSingle };
            Label lblAvatar = new Label { Text = "👤", Font = new Font("Segoe UI Emoji", 60F), AutoSize = false, Width = 120, Height = 120, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(140, 15), ForeColor = primaryBlue };
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
            btnEditProfile.FlatAppearance.BorderSize = 0;
            btnEditProfile.Click += BtnEditProfile_Click; 
            pnlCard.Controls.AddRange(new Control[] { lblAvatar, lblProName, lblProRole, txtEditEmail, txtEditPhone, txtEditAddress, lblProDate, btnEditProfile });

            Button btnBackDash2 = new Button { Text = "QUAY LẠI BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryBlue, Location = new Point(startX, 600), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackDash2.FlatAppearance.BorderSize = 0;
            btnBackDash2.Click += (s, e) => { SwitchPanel(pnlDashboard); };
            pnlProfile.Controls.AddRange(new Control[] { lblProfileTitle, pnlCard, btnBackDash2 });

            // ==========================================
            // 3. DANH SÁCH NHÂN SỰ
            // ==========================================
            pnlManageContent = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false };
            Label lblManageTitle = new Label { Text = "DANH SÁCH NHÂN VIÊN", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = primaryOrange, Location = new Point(0, 30), Width = 500, Height = 50, TextAlign = ContentAlignment.MiddleCenter };

            dgvEmployees = new DataGridView
            {
                Location = new Point(startX, 90), Size = new Size(width, 380), BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, AllowUserToAddRows = false, ReadOnly = true, 
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowTemplate = { Height = 35 }
            };
            dgvEmployees.Columns.Add("Id", "ID"); dgvEmployees.Columns["Id"].Visible = false; 
            dgvEmployees.Columns.Add("UserName", "TÀI KHOẢN"); dgvEmployees.Columns.Add("Email", "EMAIL");
            dgvEmployees.Columns.Add("PhoneNumber", "SĐT");
            dgvEmployees.CellDoubleClick += DgvEmployees_CellDoubleClick; 

            Button btnAddEmp = new Button { Text = "➕ THÊM NHÂN VIÊN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Location = new Point(startX, 490), Width = width, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnAddEmp.FlatAppearance.BorderSize = 0;
            btnAddEmp.Click += BtnAddEmp_Click; 

            Button btnBackDash3 = new Button { Text = "QUAY LẠI BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryBlue, Location = new Point(startX, 600), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackDash3.FlatAppearance.BorderSize = 0;
            btnBackDash3.Click += (s, e) => { SwitchPanel(pnlDashboard); };
            pnlManageContent.Controls.AddRange(new Control[] { lblManageTitle, dgvEmployees, btnAddEmp, btnBackDash3 });

            // ==========================================
            // 4. KHUNG THÊM / SỬA NHÂN VIÊN
            // ==========================================
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
            cbEmpRole.Items.AddRange(new string[] { "SuperAdmin", "Admin", "User" }); 
            cbEmpRole.SelectedIndex = 2; // Mặc định là User

            Label lblHint = new Label { Text = "*Mật khẩu phải có hoa, thường, số, ký tự đặc biệt", Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = Color.Gray, Location = new Point(115, formY + 30), Width = 265, Height= 20 };

            pnlFormEdit.Controls.AddRange(new Control[] { txtEmpUserName, txtEmpEmail, txtEmpPhone, lblEmpPassword, txtEmpPassword, cbEmpRole, lblHint });

            btnSaveEmp = new Button { Text = "💾 LƯU THÔNG TIN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Location = new Point(startX, 390), Width = width, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSaveEmp.FlatAppearance.BorderSize = 0;
            btnSaveEmp.Click += BtnSaveEmp_Click;

            btnIssueKey = new Button { Text = "🔑 CẤP LẠI KEY", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = warningYellow, Location = new Point(startX, 450), Width = 190, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnIssueKey.FlatAppearance.BorderSize = 0;
            btnIssueKey.Click += BtnIssueKey_Click;

            btnDeleteEmp = new Button { Text = "🗑️ XÓA", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = dangerRed, Location = new Point(startX + 210, 450), Width = 190, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnDeleteEmp.FlatAppearance.BorderSize = 0;
            btnDeleteEmp.Click += BtnDeleteEmp_Click;

            Button btnCancelEdit = new Button { Text = "❌ HỦY BỎ / QUAY LẠI", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = darkGray, BackColor = Color.LightGray, Location = new Point(startX, 600), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancelEdit.FlatAppearance.BorderSize = 0;
            btnCancelEdit.Click += (s, e) => { SwitchPanel(pnlManageContent); };

            pnlAddEditEmployee.Controls.AddRange(new Control[] { lblAddEditTitle, pnlFormEdit, btnSaveEmp, btnIssueKey, btnDeleteEmp, btnCancelEdit });

            this.Controls.Add(pnlAddEditEmployee);
            this.Controls.Add(pnlManageContent);
            this.Controls.Add(pnlProfile);
            this.Controls.Add(pnlDashboard);
        }

        // ==========================================
        // CÁC HÀM TIỆN ÍCH & CHUYỂN TRANG
        // ==========================================
        private void SwitchPanel(Panel target)
        {
            pnlDashboard.Visible = false;
            pnlProfile.Visible = false;
            pnlManageContent.Visible = false;
            pnlAddEditEmployee.Visible = false;
            target.Visible = true;
        }

        // ==========================================
        // 👉 HÀM KIỂM TRA QUYỀN KHI MỞ FORM (FORM LOAD)
        // ==========================================
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
                        string roleStr = data.GetProperty("roles").GetString()?.ToLower() ?? "user";

                        // Nếu là Quản trị, bật nút Quản lý lên
                        if (roleStr == "admin" || roleStr == "superadmin")
                        {
                            btnManageEmp.Visible = true;
                        }
                    }
                }
            }
            catch { /* Im lặng nếu lỗi mạng */ }
        }

        // ==========================================
        // LOGIC HỒ SƠ (PROFILE)
        // ==========================================
        private void BtnEditProfile_Click(object? sender, EventArgs e)
        {
            isEditMode = !isEditMode;
            if (isEditMode)
            {
                txtEditEmail.ReadOnly = false; txtEditEmail.BorderStyle = BorderStyle.FixedSingle;
                txtEditPhone.ReadOnly = false; txtEditPhone.BorderStyle = BorderStyle.FixedSingle;
                txtEditAddress.ReadOnly = false; txtEditAddress.BorderStyle = BorderStyle.FixedSingle;
                btnEditProfile.Text = "💾 LƯU THÔNG TIN"; btnEditProfile.BackColor = Color.OrangeRed;
            }
            else
            {
                txtEditEmail.ReadOnly = true; txtEditEmail.BorderStyle = BorderStyle.None;
                txtEditPhone.ReadOnly = true; txtEditPhone.BorderStyle = BorderStyle.None;
                txtEditAddress.ReadOnly = true; txtEditAddress.BorderStyle = BorderStyle.None;
                btnEditProfile.Text = "✍️ CHỈNH SỬA HỒ SƠ"; btnEditProfile.BackColor = Color.FromArgb(32, 161, 68);
                MessageBox.Show("Dữ liệu đã được ghi nhận trên giao diện!", "Lưu thành công");
            }
        }

        private async Task LoadMyProfileAsync()
        {
            lblProName.Text = "Đang tải dữ liệu...";
            try
            {
                if (string.IsNullOrEmpty(userToken)) return;
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
                    HttpResponseMessage response = await client.GetAsync("https://localhost:44387/api/app/my-profile/my-profile");
                    if (response.IsSuccessStatusCode)
                    {
                        var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                        lblProName.Text = data.GetProperty("userName").GetString()?.ToUpper();
                        txtEditEmail.Text = data.GetProperty("email").GetString();
                        string roleStr = data.GetProperty("roles").GetString() ?? "USER";
                        lblProRole.Text = string.IsNullOrEmpty(roleStr) ? "USER" : roleStr.ToUpper();
                        DateTime creationTime = data.GetProperty("creationTime").GetDateTime();
                        lblProDate.Text = "Thành viên từ: " + creationTime.ToString("dd/MM/yyyy");
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        // ==========================================
        // LOGIC QUẢN LÝ NHÂN SỰ
        // ==========================================
        private async Task LoadEmployeeListAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(userToken)) return;
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
                    HttpResponseMessage response = await client.GetAsync("https://localhost:44387/api/app/employee/employee");
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var dataList = JsonSerializer.Deserialize<JsonElement>(jsonString);
                        dgvEmployees.Rows.Clear();
                        foreach (var emp in dataList.EnumerateArray())
                        {
                            dgvEmployees.Rows.Add(
                                emp.GetProperty("id").GetString() ?? "",
                                emp.GetProperty("userName").GetString() ?? "",
                                emp.GetProperty("email").GetString() ?? "",
                                emp.GetProperty("phoneNumber").GetString() ?? ""
                            );
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi máy chủ: " + ex.Message); }
        }

        private void BtnAddEmp_Click(object? sender, EventArgs e)
        {
            currentEditUserId = null; 
            lblAddEditTitle.Text = "THÊM NHÂN VIÊN MỚI";
            
            txtEmpUserName.Text = ""; txtEmpEmail.Text = ""; txtEmpPhone.Text = ""; txtEmpPassword.Text = "";
            cbEmpRole.SelectedIndex = 2; 
            
            lblEmpPassword.Visible = true; txtEmpPassword.Visible = true;
            btnDeleteEmp.Visible = false; btnIssueKey.Visible = false;

            SwitchPanel(pnlAddEditEmployee);
        }

        private void DgvEmployees_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                currentEditUserId = dgvEmployees.Rows[e.RowIndex].Cells["Id"].Value?.ToString();
                lblAddEditTitle.Text = "CHI TIẾT NHÂN VIÊN";

                txtEmpUserName.Text = dgvEmployees.Rows[e.RowIndex].Cells["UserName"].Value?.ToString() ?? "";
                txtEmpEmail.Text = dgvEmployees.Rows[e.RowIndex].Cells["Email"].Value?.ToString() ?? "";
                txtEmpPhone.Text = dgvEmployees.Rows[e.RowIndex].Cells["PhoneNumber"].Value?.ToString() ?? "";
                
                lblEmpPassword.Visible = false; txtEmpPassword.Visible = false;
                btnDeleteEmp.Visible = true; btnIssueKey.Visible = true;

                SwitchPanel(pnlAddEditEmployee);
            }
        }

        private async void BtnSaveEmp_Click(object? sender, EventArgs e)
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

                    HttpResponseMessage response;

                    if (currentEditUserId == null) 
                    {
                        var newUser = new {
                            userName = txtEmpUserName.Text, email = txtEmpEmail.Text,
                            phoneNumber = txtEmpPhone.Text, password = txtEmpPassword.Text,
                            role = cbEmpRole.Text 
                        };
                        var content = new StringContent(JsonSerializer.Serialize(newUser), Encoding.UTF8, "application/json");
                        response = await client.PostAsync("https://localhost:44387/api/app/employee/account", content);
                    }
                    else
                    {
                        var updatedUser = new {
                            userName = txtEmpUserName.Text, email = txtEmpEmail.Text,
                            phoneNumber = txtEmpPhone.Text, role = cbEmpRole.Text 
                        };
                        var content = new StringContent(JsonSerializer.Serialize(updatedUser), Encoding.UTF8, "application/json");
                        response = await client.PutAsync($"https://localhost:44387/api/app/employee/{currentEditUserId}/account", content);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Lưu thông tin thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SwitchPanel(pnlManageContent);
                        await LoadEmployeeListAsync(); 
                    }
                    else
                    {
                        string rawResponse = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(rawResponse))
                        {
                            MessageBox.Show($"Lỗi API! Server không phản hồi JSON. Mã lỗi HTTP: {response.StatusCode}", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            try
                            {
                                var errorData = JsonSerializer.Deserialize<JsonElement>(rawResponse);
                                string errMsg = errorData.GetProperty("error").GetProperty("message").GetString() ?? "Lỗi không xác định";
                                MessageBox.Show($"Thất bại: {errMsg}", "Lỗi phân quyền", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            catch
                            {
                                MessageBox.Show($"Lỗi máy chủ ({response.StatusCode}):\n{rawResponse}", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi máy chủ: " + ex.Message); }
        }

        private async void BtnDeleteEmp_Click(object? sender, EventArgs e)
        {
            if (currentEditUserId == null) return;

            DialogResult result = MessageBox.Show($"Bạn có CHẮC CHẮN muốn xóa tài khoản [{txtEmpUserName.Text}]?", "Cảnh báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                try
                {
                    HttpClientHandler handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
                        HttpResponseMessage response = await client.DeleteAsync($"https://localhost:44387/api/app/employee/{currentEditUserId}/account");

                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Tiễn đưa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            SwitchPanel(pnlManageContent);
                            await LoadEmployeeListAsync();
                        }
                        else
                        {
                            string rawResponse = await response.Content.ReadAsStringAsync();
                            if (string.IsNullOrWhiteSpace(rawResponse))
                            {
                                MessageBox.Show($"Lỗi API! Server không phản hồi JSON. Mã lỗi HTTP: {response.StatusCode}", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                try
                                {
                                    var errorData = JsonSerializer.Deserialize<JsonElement>(rawResponse);
                                    string errMsg = errorData.GetProperty("error").GetProperty("message").GetString() ?? "Lỗi quyền";
                                    MessageBox.Show($"Lỗi: {errMsg}", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                catch
                                {
                                    MessageBox.Show($"Lỗi máy chủ ({response.StatusCode}):\n{rawResponse}", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Lỗi máy chủ: " + ex.Message); }
            }
        }

        private async void BtnIssueKey_Click(object? sender, EventArgs e)
        {
            if (currentEditUserId == null) return;

            DialogResult result = MessageBox.Show(
                $"Bạn có chắc chắn muốn HỦY Key cũ và CẤP KEY MỚI cho tài khoản [{txtEmpUserName.Text}]?\n(Nhân viên sẽ không thể dùng Key cũ để đăng nhập nữa)", 
                "Xác nhận Cấp Key", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    HttpClientHandler handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

                        var emptyContent = new StringContent("", Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await client.PostAsync($"https://localhost:44387/api/app/employee/{currentEditUserId}/reset-key", emptyContent);

                        string rawResponse = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            string newKey = rawResponse.Replace("\"", "");
                            MessageBox.Show($"ĐÃ CẤP LẠI KEY THÀNH CÔNG!\n\nTài khoản: {txtEmpUserName.Text}\nKey Mới: {newKey}\n\nHãy copy Key này và gửi cho nhân viên.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(rawResponse))
                            {
                                MessageBox.Show($"Lỗi API! Server không phản hồi. Mã lỗi: {response.StatusCode}", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                try
                                {
                                    var errorData = JsonSerializer.Deserialize<JsonElement>(rawResponse);
                                    string errMsg = errorData.GetProperty("error").GetProperty("message").GetString() ?? "Lỗi quyền";
                                    MessageBox.Show($"Thất bại: {errMsg}", "Lỗi phân quyền", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                catch
                                {
                                    MessageBox.Show($"Lỗi máy chủ ({response.StatusCode}):\n{rawResponse}", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Lỗi máy chủ: " + ex.Message); }
            }
        }
    }
}