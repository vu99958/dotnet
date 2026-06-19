using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class FormDashboard : Form
    {
        // Nhận token từ Form1 truyền sang
        private string userToken; 

        // Các biến giao diện
        private Panel pnlDashboard = null!;
        private Panel pnlProfile = null!;
        private Label lblProName = null!;
        private Label lblProRole = null!;
        private Label lblProDate = null!;

        // Các biến dùng cho tính năng Chỉnh sửa thông tin
        private TextBox txtEditEmail = null!;
        private TextBox txtEditPhone = null!;
        private TextBox txtEditAddress = null!;
        private Button btnEditProfile = null!;
        
        // 👉 CHÚ THÍCH: 2 biến này dành cho Màn hình Quản lý nhân viên
        private Panel pnlManageContent = null!;
        private DataGridView dgvEmployees = null!;
        
        private bool isEditMode = false; // Theo dõi trạng thái bật/tắt sửa

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
        }

        private void VeGiaoDienDashboard()
        {
            Color primaryBlue = Color.FromArgb(0, 102, 204);
            Color primaryGreen = Color.FromArgb(32, 161, 68);
            Color primaryOrange = Color.FromArgb(255, 140, 0);
            Color lightGray = Color.FromArgb(245, 247, 250);
            Color darkGray = Color.FromArgb(80, 80, 80);

            int startX = 40;
            int width = 400;

            // ==========================================
            // 1. KHUNG BẢNG ĐIỀU KHIỂN (DASHBOARD)
            // ==========================================
            pnlDashboard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Visible = true };
            
            Label lblDashTitle = new Label { Text = "BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 24F, FontStyle.Bold), ForeColor = primaryBlue, Location = new Point(startX, 40), Width = width, Height = 80, TextAlign = ContentAlignment.MiddleCenter };

            Button btnViewProfile = new Button { Text = "👤 HỒ SƠ CỦA TÔI", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Location = new Point(startX, 150), Width = width, Height = 60, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnViewProfile.FlatAppearance.BorderSize = 0;
            btnViewProfile.Click += async (s, e) => { 
                pnlDashboard.Visible = false; 
                pnlProfile.Visible = true; 
                await LoadMyProfileAsync(); 
            };

            Button btnManageEmp = new Button { Text = "👥 QUẢN LÝ NHÂN VIÊN", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryOrange, Location = new Point(startX, 230), Width = width, Height = 60, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnManageEmp.FlatAppearance.BorderSize = 0;
            // 👉 CHÚ THÍCH: Gắn sự kiện khi bấm nút Quản lý sẽ mở Panel Manage và gọi API lấy danh sách
            btnManageEmp.Click += async (s, e) => {
                pnlDashboard.Visible = false;
                pnlManageContent.Visible = true;
                await LoadEmployeeListAsync();
            };

            Button btnLogoutDash = new Button { Text = "ĐĂNG XUẤT", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Gray, Location = new Point(startX, 530), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnLogoutDash.FlatAppearance.BorderSize = 0;
            btnLogoutDash.Click += (s, e) => { Application.Restart(); };

            pnlDashboard.Controls.AddRange(new Control[] { lblDashTitle, btnViewProfile, btnManageEmp, btnLogoutDash });

            // ==========================================
            // 2. KHUNG HỒ SƠ CÁ NHÂN (PROFILE CARD)
            // ==========================================
            pnlProfile = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false };
            
            Label lblProfileTitle = new Label { Text = "THẺ NHÂN VIÊN", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = primaryGreen, Location = new Point(0, 30), Width = 500, Height = 50, TextAlign = ContentAlignment.MiddleCenter };

            Panel pnlCard = new Panel { Width = 400, Height = 480, BackColor = Color.White, Location = new Point(startX, 90), BorderStyle = BorderStyle.FixedSingle };
            
            Label lblAvatar = new Label { Text = "👤", Font = new Font("Segoe UI Emoji", 60F), AutoSize = false, Width = 120, Height = 120, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(140, 15), ForeColor = primaryBlue };
            lblProName = new Label { Text = "Đang tải...", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = darkGray, AutoSize = false, Width = 400, Height = 40, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 140) };
            lblProRole = new Label { Text = "ROLE", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryOrange, AutoSize = false, Width = 120, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(140, 190) };
            
            Label lblEmailTitle = new Label { Text = "✉️ Email:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(20, 240), AutoSize = true };
            txtEditEmail = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(110, 238), Width = 260, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White };

            Label lblPhoneTitle = new Label { Text = "📞 SĐT:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(20, 280), AutoSize = true };
            txtEditPhone = new TextBox { Text = "Chưa cập nhật", Font = new Font("Segoe UI", 11F), Location = new Point(110, 278), Width = 260, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White };

            Label lblAddressTitle = new Label { Text = "📍 Địa chỉ:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(20, 320), AutoSize = true };
            txtEditAddress = new TextBox { Text = "Chưa cập nhật", Font = new Font("Segoe UI", 11F), Location = new Point(110, 318), Width = 260, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White };

            lblProDate = new Label { Text = "Tham gia: ...", Font = new Font("Segoe UI", 10F, FontStyle.Italic), ForeColor = Color.Gray, AutoSize = false, Width = 400, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 370) };

            btnEditProfile = new Button { Text = "✍️ CHỈNH SỬA HỒ SƠ", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Location = new Point(100, 415), Width = 200, Height = 40, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnEditProfile.FlatAppearance.BorderSize = 0;
            btnEditProfile.Click += BtnEditProfile_Click; 

            pnlCard.Controls.AddRange(new Control[] { 
                lblAvatar, lblProName, lblProRole, 
                lblEmailTitle, txtEditEmail, 
                lblPhoneTitle, txtEditPhone, 
                lblAddressTitle, txtEditAddress, 
                lblProDate, btnEditProfile 
            });

            Button btnBackDash2 = new Button { Text = "QUAY LẠI BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryBlue, Location = new Point(startX, 600), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackDash2.FlatAppearance.BorderSize = 0;
            btnBackDash2.Click += (s, e) => { pnlProfile.Visible = false; pnlDashboard.Visible = true; };

            pnlProfile.Controls.AddRange(new Control[] { lblProfileTitle, pnlCard, btnBackDash2 });

            // ==========================================
            // 👉 CHÚ THÍCH: 3. KHU VỰC QUẢN LÝ NHÂN SỰ (THÊM MỚI)
            // ==========================================
            pnlManageContent = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false };
            
            Label lblManageTitle = new Label { Text = "DANH SÁCH NHÂN VIÊN", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = primaryOrange, Location = new Point(0, 30), Width = 500, Height = 50, TextAlign = ContentAlignment.MiddleCenter };

            // Cấu hình Bảng dữ liệu (DataGridView)
            dgvEmployees = new DataGridView
            {
                Location = new Point(startX, 100),
                Size = new Size(width, 380), // Kích thước bằng với thẻ hồ sơ
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false, // Không cho người dùng gõ thêm dòng trống
                ReadOnly = true, // Chỉ đọc, không cho sửa trực tiếp trên bảng
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, // Bấm vào 1 ô là chọn cả hàng
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, // Tự động dãn cột cho vừa bảng
                RowTemplate = { Height = 35 },
                AllowUserToResizeRows = false
            };

            // Định nghĩa các cột hiển thị
            dgvEmployees.Columns.Add("Id", "ID");
            dgvEmployees.Columns["Id"].Visible = false; // Ẩn ID (chỉ dùng ngầm bên dưới)
            dgvEmployees.Columns.Add("UserName", "TÀI KHOẢN");
            dgvEmployees.Columns.Add("Email", "EMAIL");
            dgvEmployees.Columns.Add("PhoneNumber", "SĐT");

            // Nút quay lại dành riêng cho màn hình quản lý
            Button btnBackDash3 = new Button { Text = "QUAY LẠI BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryBlue, Location = new Point(startX, 600), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackDash3.FlatAppearance.BorderSize = 0;
            btnBackDash3.Click += (s, e) => { pnlManageContent.Visible = false; pnlDashboard.Visible = true; };

            pnlManageContent.Controls.AddRange(new Control[] { lblManageTitle, dgvEmployees, btnBackDash3 });

            // 👉 CHÚ THÍCH: Nạp cả 3 màn hình vào Form chính
            this.Controls.Add(pnlManageContent);
            this.Controls.Add(pnlProfile);
            this.Controls.Add(pnlDashboard);
        }

        // ==========================================
        // CÁC HÀM XỬ LÝ LOGIC
        // ==========================================

        private void BtnEditProfile_Click(object? sender, EventArgs e)
        {
            isEditMode = !isEditMode;

            if (isEditMode)
            {
                txtEditEmail.ReadOnly = false; txtEditEmail.BorderStyle = BorderStyle.FixedSingle;
                txtEditPhone.ReadOnly = false; txtEditPhone.BorderStyle = BorderStyle.FixedSingle;
                txtEditAddress.ReadOnly = false; txtEditAddress.BorderStyle = BorderStyle.FixedSingle;
                
                btnEditProfile.Text = "💾 LƯU THÔNG TIN";
                btnEditProfile.BackColor = Color.OrangeRed;
            }
            else
            {
                txtEditEmail.ReadOnly = true; txtEditEmail.BorderStyle = BorderStyle.None;
                txtEditPhone.ReadOnly = true; txtEditPhone.BorderStyle = BorderStyle.None;
                txtEditAddress.ReadOnly = true; txtEditAddress.BorderStyle = BorderStyle.None;
                
                btnEditProfile.Text = "✍️ CHỈNH SỬA HỒ SƠ";
                btnEditProfile.BackColor = Color.FromArgb(32, 161, 68);

                MessageBox.Show("Dữ liệu đã được ghi nhận trên giao diện!\n(Sẽ cần thiết lập thêm API Backend để lưu vĩnh viễn vào SQL Server)", "Lưu thành công");
            }
        }

        private async Task LoadMyProfileAsync()
        {
            lblProName.Text = "Đang tải dữ liệu...";
            lblProRole.Text = "...";
            try
            {
                if (string.IsNullOrEmpty(userToken)) return;

                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

                    HttpResponseMessage response = await client.GetAsync("https://localhost:44387/api/app/my-profile/my-profile");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                        
                        lblProName.Text = responseData.GetProperty("userName").GetString()?.ToUpper();
                        txtEditEmail.Text = responseData.GetProperty("email").GetString();
                        
                        string roleStr = responseData.GetProperty("roles").GetString() ?? "USER";
                        lblProRole.Text = string.IsNullOrEmpty(roleStr) ? "USER" : roleStr.ToUpper();
                        
                        DateTime creationTime = responseData.GetProperty("creationTime").GetDateTime();
                        lblProDate.Text = "Thành viên từ: " + creationTime.ToString("dd/MM/yyyy");
                    }
                    else
                    {
                        lblProName.Text = "Lỗi tải thông tin!";
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        // 👉 CHÚ THÍCH: HÀM MỚI - GỌI API LẤY DANH SÁCH NHÂN VIÊN
        private async Task LoadEmployeeListAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(userToken))
                {
                    MessageBox.Show("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.", "Lỗi bảo mật");
                    return;
                }
                
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                    // Chìa khóa token để chứng minh quyền Admin
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

                    // Chọc vào Backend API
                    HttpResponseMessage response = await client.GetAsync("https://localhost:44387/api/app/employee/employee");

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var dataList = JsonSerializer.Deserialize<JsonElement>(jsonString);

                        // Reset bảng: Xóa hết các dòng cũ trước khi tải dòng mới
                        dgvEmployees.Rows.Clear();

                        // Lặp qua từng nhân viên trong cục dữ liệu JSON và nhét vào bảng
                        foreach (var emp in dataList.EnumerateArray())
                        {
                            string id = emp.GetProperty("id").GetString() ?? "";
                            string username = emp.GetProperty("userName").GetString() ?? "";
                            string email = emp.GetProperty("email").GetString() ?? "";
                            string phone = emp.GetProperty("phoneNumber").GetString() ?? "";
                            
                            // Thêm dòng dữ liệu vào DataGridView theo đúng thứ tự cột
                            dgvEmployees.Rows.Add(id, username, email, phone);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Bạn không có quyền xem danh sách này! (Mã lỗi: {response.StatusCode})", "Từ chối truy cập");
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi máy chủ: " + ex.Message); }
        }
    }
}