using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuanLyNhanSu.DesktopClient.Services;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class Form1 : Form
    {
        // 1. Thêm cụm '= null!' để dập tắt hoàn toàn gạch sóng vàng (Cảnh báo Nullable)
        private Panel pnlLogin = null!;
        private Panel pnlRegister = null!;
        private Panel pnlCreateKey = null!;
        private Panel pnlKeyLogin = null!;

        private TextBox txtLogUsername = null!;
        private TextBox txtLogPassword = null!;
        private Label lblLogMessage = null!;
        private Button btnLogin = null!;

        private TextBox txtRegUsername = null!;
        private TextBox txtRegEmail = null!;
        private TextBox txtRegPassword = null!;
        private Label lblRegMessage = null!;
        private Button btnRegister = null!;

        private ComboBox cmbRole = null!;
        private TextBox txtKeyDesc = null!;
        private Label lblCreateKeyMessage = null!;
        private Button btnCreateKey = null!;
        private Label lblKeyDisplay = null!;

        private TextBox txtLoginKey = null!;
        private Label lblKeyLoginMessage = null!;
        private Button btnKeyLogin = null!;

        private string userToken = "";
        private string keyFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuanLyNhanSu", "userkey.txt");

        public Form1()
        {
            InitializeComponent();
            VeGiaoDienXinXo();
            LoadSavedKey();
        }

     private void VeGiaoDienXinXo()
        {
            // TẮT TÍNH NĂNG TỰ ĐỘNG ZOOM CỦA WINDOWS ĐỂ CHỐNG VỠ LAYOUT
            this.AutoScaleMode = AutoScaleMode.None;

            // CẤU HÌNH FORM GỐC
            this.Text = "Hệ thống Quản Lý Nhân Sự (Bản Premium)";
            this.Size = new Size(500, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 11F);

            Color primaryBlue = Color.FromArgb(0, 102, 204);
            Color primaryGreen = Color.FromArgb(32, 161, 68);
            Color primaryOrange = Color.FromArgb(255, 140, 0);
            Color lightGray = Color.FromArgb(245, 247, 250);
            Color darkGray = Color.FromArgb(80, 80, 80);

            int startX = 40;  // Căn lề trái
            int width = 400;  // Chiều rộng chuẩn

            // ==========================================
            // 1. KHUNG ĐĂNG NHẬP
            // ==========================================
            pnlLogin = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            
            Label lblLogTitle = new Label { Text = "ĐĂNG NHẬP", Font = new Font("Segoe UI", 24F, FontStyle.Bold), ForeColor = primaryBlue, Location = new Point(startX, 40), Width = width, Height = 80, TextAlign = ContentAlignment.MiddleCenter };
            
            Label lblLogU = new Label { Text = "Tài khoản:", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(startX, 130), AutoSize = true };
            txtLogUsername = new TextBox { Font = new Font("Segoe UI", 14F), Location = new Point(startX, 160), Width = width, Height = 40, AutoSize = false, BackColor = lightGray, BorderStyle = BorderStyle.FixedSingle };
            
            Label lblLogP = new Label { Text = "Mật khẩu:", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(startX, 230), AutoSize = true };
            txtLogPassword = new TextBox { Font = new Font("Segoe UI", 14F), Location = new Point(startX, 260), Width = width, Height = 40, AutoSize = false, PasswordChar = '•', BackColor = lightGray, BorderStyle = BorderStyle.FixedSingle };
            
            btnLogin = new Button { Text = "ĐĂNG NHẬP", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryBlue, Location = new Point(startX, 340), Width = width, Height = 55, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += async (s, e) => await XuLyDangNhapAsync();

            lblLogMessage = new Label { Location = new Point(startX, 410), Width = width, Height = 50, TextAlign = ContentAlignment.TopCenter, ForeColor = Color.Red, Font = new Font("Segoe UI", 10F) };

            Label lblToRegister = new Label { Text = "Chưa có tài khoản? Đăng ký ngay", Location = new Point(startX, 480), Width = width, Height = 40, TextAlign = ContentAlignment.MiddleCenter, ForeColor = primaryBlue, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 11F, FontStyle.Underline) };
            lblToRegister.Click += (s, e) => SwitchPanel(pnlRegister);

            Label lblToKeyLogin = new Label { Text = "Dùng Key bảo mật để đăng nhập", Location = new Point(startX, 530), Width = width, Height = 40, TextAlign = ContentAlignment.MiddleCenter, ForeColor = primaryOrange, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 11F, FontStyle.Underline) };
            lblToKeyLogin.Click += (s, e) => SwitchPanel(pnlKeyLogin);

            pnlLogin.Controls.AddRange(new Control[] { lblLogTitle, lblLogU, txtLogUsername, lblLogP, txtLogPassword, btnLogin, lblLogMessage, lblToRegister, lblToKeyLogin });

            // ==========================================
            // 2. KHUNG ĐĂNG KÝ
            // ==========================================
            pnlRegister = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Visible = false };

            Label lblRegTitle = new Label { Text = "ĐĂNG KÝ MỚI", Font = new Font("Segoe UI", 24F, FontStyle.Bold), ForeColor = primaryGreen, Location = new Point(startX, 30), Width = width, Height = 80, TextAlign = ContentAlignment.MiddleCenter };
            
            Label lblRegU = new Label { Text = "Tên đăng nhập:", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(startX, 110), AutoSize = true };
            txtRegUsername = new TextBox { Font = new Font("Segoe UI", 14F), Location = new Point(startX, 140), Width = width, Height = 40, AutoSize = false, BackColor = lightGray, BorderStyle = BorderStyle.FixedSingle };
            
            Label lblRegE = new Label { Text = "Email:", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(startX, 200), AutoSize = true };
            txtRegEmail = new TextBox { Font = new Font("Segoe UI", 14F), Location = new Point(startX, 230), Width = width, Height = 40, AutoSize = false, BackColor = lightGray, BorderStyle = BorderStyle.FixedSingle };
            
            Label lblRegP = new Label { Text = "Mật khẩu:", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(startX, 290), AutoSize = true };
            txtRegPassword = new TextBox { Font = new Font("Segoe UI", 14F), Location = new Point(startX, 320), Width = width, Height = 40, AutoSize = false, PasswordChar = '•', BackColor = lightGray, BorderStyle = BorderStyle.FixedSingle };
            
            btnRegister = new Button { Text = "TẠO TÀI KHOẢN", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Location = new Point(startX, 400), Width = width, Height = 55, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += async (s, e) => await XuLyDangKyAsync();

            lblRegMessage = new Label { Location = new Point(startX, 470), Width = width, Height = 50, TextAlign = ContentAlignment.TopCenter, ForeColor = Color.Red, Font = new Font("Segoe UI", 10F) };

            Label lblToLogin = new Label { Text = "Đã có tài khoản? Quay lại đăng nhập", Location = new Point(startX, 540), Width = width, Height = 40, TextAlign = ContentAlignment.MiddleCenter, ForeColor = primaryBlue, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 11F, FontStyle.Underline) };
            lblToLogin.Click += (s, e) => SwitchPanel(pnlLogin);

            pnlRegister.Controls.AddRange(new Control[] { lblRegTitle, lblRegU, txtRegUsername, lblRegE, txtRegEmail, lblRegP, txtRegPassword, btnRegister, lblRegMessage, lblToLogin });

            // ==========================================
            // 3. KHUNG TẠO KEY
            // ==========================================
            pnlCreateKey = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Visible = false };

            Label lblCreateKeyTitle = new Label { Text = "TẠO KEY ROLE", Font = new Font("Segoe UI", 24F, FontStyle.Bold), ForeColor = primaryGreen, Location = new Point(startX, 30), Width = width, Height = 80, TextAlign = ContentAlignment.MiddleCenter };
            
            Label lblRole = new Label { Text = "Chọn Vai trò:", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(startX, 110), AutoSize = true };
            cmbRole = new ComboBox { Font = new Font("Segoe UI", 14F), Location = new Point(startX, 140), Width = width, Height = 40, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = lightGray };
            cmbRole.Items.AddRange(new string[] { "user", "admin", "super_admin" });
            cmbRole.SelectedIndex = 0;

            Label lblDesc = new Label { Text = "Mô tả (Tuỳ chọn):", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(startX, 200), AutoSize = true };
            txtKeyDesc = new TextBox { Font = new Font("Segoe UI", 12F), Location = new Point(startX, 230), Width = width, Height = 70, AutoSize = false, Multiline = true, BackColor = lightGray, BorderStyle = BorderStyle.FixedSingle };

            btnCreateKey = new Button { Text = "TẠO KEY", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Location = new Point(startX, 330), Width = width, Height = 55, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCreateKey.FlatAppearance.BorderSize = 0;
            btnCreateKey.Click += async (s, e) => await XuLyTaoKeyAsync();

            lblCreateKeyMessage = new Label { Location = new Point(startX, 400), Width = width, Height = 40, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Red, Font = new Font("Segoe UI", 10F) };

            lblKeyDisplay = new Label { Location = new Point(startX, 440), Width = width, Height = 50, TextAlign = ContentAlignment.MiddleCenter, ForeColor = primaryGreen, Font = new Font("Consolas", 12F, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(240, 250, 240) };

            Button btnCopyKey = new Button { Text = "COPY KEY", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.CornflowerBlue, Location = new Point(startX, 510), Width = 190, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCopyKey.FlatAppearance.BorderSize = 0;
            btnCopyKey.Click += (s, e) => { if (!string.IsNullOrEmpty(lblKeyDisplay.Text)) { Clipboard.SetText(lblKeyDisplay.Text); MessageBox.Show("Đã copy key vào clipboard!", "Thành công"); } };

            Button btnBackToLog1 = new Button { Text = "QUAY LẠI", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Gray, Location = new Point(startX + 210, 510), Width = 190, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackToLog1.FlatAppearance.BorderSize = 0;
            btnBackToLog1.Click += (s, e) => SwitchPanel(pnlLogin);

            pnlCreateKey.Controls.AddRange(new Control[] { lblCreateKeyTitle, lblRole, cmbRole, lblDesc, txtKeyDesc, btnCreateKey, lblCreateKeyMessage, lblKeyDisplay, btnCopyKey, btnBackToLog1 });

            // ==========================================
            // 4. KHUNG ĐĂNG NHẬP BẰNG KEY
            // ==========================================
            pnlKeyLogin = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Visible = false };

            Label lblKeyLoginTitle = new Label { Text = "ĐĂNG NHẬP KEY", Font = new Font("Segoe UI", 24F, FontStyle.Bold), ForeColor = primaryOrange, Location = new Point(startX, 80), Width = width, Height = 80, TextAlign = ContentAlignment.MiddleCenter };
            
            Label lblKey = new Label { Text = "Nhập/Dán Key:", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(startX, 180), AutoSize = true };
            txtLoginKey = new TextBox { Font = new Font("Segoe UI", 14F), Location = new Point(startX, 210), Width = width, Height = 40, AutoSize = false, BackColor = lightGray, BorderStyle = BorderStyle.FixedSingle };

            btnKeyLogin = new Button { Text = "ĐĂNG NHẬP VỚI KEY", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryOrange, Location = new Point(startX, 280), Width = width, Height = 55, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnKeyLogin.FlatAppearance.BorderSize = 0;
            btnKeyLogin.Click += async (s, e) => await XuLyDangNhapBangKeyAsync();

            lblKeyLoginMessage = new Label { Location = new Point(startX, 350), Width = width, Height = 50, TextAlign = ContentAlignment.TopCenter, ForeColor = Color.Red, Font = new Font("Segoe UI", 10F) };

            Label lblBackToLogin2 = new Label { Text = "Quay lại đăng nhập thông thường", Location = new Point(startX, 430), Width = width, Height = 40, TextAlign = ContentAlignment.MiddleCenter, ForeColor = primaryBlue, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 11F, FontStyle.Underline) };
            lblBackToLogin2.Click += (s, e) => SwitchPanel(pnlLogin);

            pnlKeyLogin.Controls.AddRange(new Control[] { lblKeyLoginTitle, lblKey, txtLoginKey, btnKeyLogin, lblKeyLoginMessage, lblBackToLogin2 });

            // Thêm tất cả Panel vào Form chính
            this.Controls.Add(pnlLogin);
            this.Controls.Add(pnlRegister);
            this.Controls.Add(pnlCreateKey);
            this.Controls.Add(pnlKeyLogin);
        }
        /// <summary>
        /// Helper method để chuyển đổi giữa các panel
        /// </summary>
        private void SwitchPanel(Panel targetPanel)
        {
            pnlLogin.Visible = false;
            pnlRegister.Visible = false;
            pnlCreateKey.Visible = false;
            pnlKeyLogin.Visible = false;
            targetPanel.Visible = true;
        }

        /// <summary>
        /// Tải key đã lưu vào text box
        /// </summary>
        private void LoadSavedKey()
        {
            var savedKey = LoadKeyFromFile();
            if (!string.IsNullOrEmpty(savedKey))
                txtLoginKey.Text = savedKey;
        }

      private async Task XuLyDangNhapAsync()
        {
            lblLogMessage.Text = "Đang kiểm tra CSDL..."; lblLogMessage.ForeColor = Color.Blue; btnLogin.Enabled = false;
            try
            {
                // LƯỢT 1: LẤY TOKEN ĐỂ CHỨNG MINH ĐĂNG NHẬP THÀNH CÔNG
                var parameters = new Dictionary<string, string> { 
                     { "grant_type", "password" }, 
                     { "username", txtLogUsername.Text }, 
                     { "password", txtLogPassword.Text }, 
                     { "client_id", "QuanLyNhanSu_Swagger" },
                     { "scope", "QuanLyNhanSu offline_access profile roles email" } 
                };
                var content = new FormUrlEncodedContent(parameters);
                
                HttpResponseMessage response = await ApiClient.PostAsync("connect/token", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                    userToken = data.GetProperty("access_token").GetString() ?? "";
                    
                    lblLogMessage.Text = "Xác thực thành công! Đang kiểm tra phân quyền..."; 

                    // LƯỢT 2: CHỐT CHẶN LOGIC - KIỂM TRA ĐÃ CÓ KEY CHƯA
                    HttpResponseMessage keyResponse = await ApiClient.GetAsync("api/app/user-key/user-keys", userToken);
                            
                            if (keyResponse.IsSuccessStatusCode)
                            {
                                var keyData = JsonSerializer.Deserialize<JsonElement>(await keyResponse.Content.ReadAsStringAsync());
                                
                                // Nếu mảng JSON trả về có độ dài = 0 (tức là CHƯA CÓ KEY)
                                if (keyData.ValueKind == JsonValueKind.Array && keyData.GetArrayLength() == 0)
                                {
                                    MessageBox.Show("Tài khoản mới bắt buộc phải tạo Key định danh để hệ thống phân quyền!", "Yêu cầu từ hệ thống");
                                    
                                    // Bẻ lái sang trang tạo Key
                                    pnlLogin.Visible = false;
                                    pnlCreateKey.Visible = true;
                                    lblCreateKeyMessage.Text = "";
                                    lblKeyDisplay.Text = "";
                                    cmbRole.SelectedIndex = 0;
                                    txtKeyDesc.Clear();
                                }
                                else
                                {
                                    // ĐÃ CÓ KEY -> Cho qua trạm, vào thẳng Dashboard
                                    this.Hide(); 
                                    FormDashboard dashboard = new FormDashboard(userToken); 
                                    dashboard.Show();
                                }
                            }
                            else
                            {
                                lblLogMessage.Text = "Lỗi khi kiểm tra phân quyền!"; lblLogMessage.ForeColor = Color.Red;
                            }
                    }
                    else
                    {
                        string errorResponse = await response.Content.ReadAsStringAsync();
                        lblLogMessage.Text = $"Từ chối ({response.StatusCode})! Sai tài khoản/mật khẩu."; 
                        lblLogMessage.ForeColor = Color.Red;
                    }
            }
            catch (Exception ex) { 
                lblLogMessage.Text = "Lỗi kết nối tới Server."; lblLogMessage.ForeColor = Color.Red; 
                MessageBox.Show("Chi tiết:\n" + ex.Message, "Bắt mạch lỗi");
            }
            finally { btnLogin.Enabled = true; }
        }

        private async Task XuLyDangKyAsync()
        {
            lblRegMessage.Text = "Đang ghi dữ liệu vào SQL..."; lblRegMessage.ForeColor = Color.Blue; btnRegister.Enabled = false;
            try
            {
                string apiUrl = "api/account/register";
                
                var payload = new {
                    appName = "QuanLyNhanSu_Web",
                    userName = txtRegUsername.Text,
                    emailAddress = txtRegEmail.Text,
                    password = txtRegPassword.Text
                };
                
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await ApiClient.PostAsync(apiUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    lblRegMessage.Text = "Đăng ký thành công! Đã Commit."; lblRegMessage.ForeColor = Color.Green;
                    MessageBox.Show("Tạo tài khoản thành công!\nMời bạn quay lại màn hình Đăng Nhập.", "Hệ thống");
                    
                    txtLogUsername.Text = txtRegUsername.Text;
                    pnlRegister.Visible = false; pnlLogin.Visible = true; 
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    lblRegMessage.Text = $"Rollback ({response.StatusCode})! Xem chi tiết ở hộp thoại."; 
                    lblRegMessage.ForeColor = Color.Red;
                    MessageBox.Show("Nguyên nhân Backend Rollback:\n" + errorResponse, "Bắt mạch lỗi");
                }
            }
            catch (Exception ex) { 
                lblRegMessage.Text = "Lỗi Exception! Đảm bảo Backend đang chạy."; lblRegMessage.ForeColor = Color.Red;
                MessageBox.Show("Lỗi kết nối cụ thể:\n" + ex.Message, "Bắt mạch lỗi");
            }
            finally { btnRegister.Enabled = true; }
        }

        /// <summary>
        /// Tạo User Key mới (Xử lý Async + Rollback)
        /// </summary>
        private async Task XuLyTaoKeyAsync()
        {
            lblCreateKeyMessage.Text = "Đang tạo Key..."; lblCreateKeyMessage.ForeColor = Color.Blue; btnCreateKey.Enabled = false;
            try
            {
                var payload = new {
                    role = cmbRole.SelectedItem?.ToString() ?? "user",
                    description = string.IsNullOrEmpty(txtKeyDesc.Text) ? null : txtKeyDesc.Text,
                    expirationDate = (DateTime?)null
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await ApiClient.PostAsync("api/UserKey/create", content, userToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                    
                    if (responseData.GetProperty("success").GetBoolean())
                    {
                        var keyData = responseData.GetProperty("data");
                        string generatedKey = keyData.GetProperty("key").GetString() ?? "";
                        
                        lblCreateKeyMessage.Text = "✓ Tạo Key thành công! Hãy lưu Key này ở chỗ an toàn."; 
                        lblCreateKeyMessage.ForeColor = Color.Green;
                        
                        lblKeyDisplay.Text = generatedKey;
                        lblKeyDisplay.ForeColor = Color.Green;
                        
                        // Lưu key vào file
                        SaveKeyToFile(generatedKey);
                        
                        MessageBox.Show($"Key của bạn:\n{generatedKey}\n\nHãy lưu Key này để đăng nhập lần tới.", "Key tạo thành công");
                    }
                    else
                    {
                        lblCreateKeyMessage.Text = $"✗ Lỗi: {responseData.GetProperty("error").GetString()}";
                        lblCreateKeyMessage.ForeColor = Color.Red;
                    }
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    lblCreateKeyMessage.Text = $"✗ Rollback ({response.StatusCode})! Xem chi tiết ở hộp thoại."; 
                    lblCreateKeyMessage.ForeColor = Color.Red;
                    MessageBox.Show("Chi tiết lỗi từ máy chủ:\n" + errorResponse, "Bắt mạch lỗi");
                }
            }
            catch (Exception ex)
            {
                lblCreateKeyMessage.Text = "✗ Lỗi Exception! Đảm bảo Backend đang chạy."; 
                lblCreateKeyMessage.ForeColor = Color.Red;
                MessageBox.Show("Lỗi kết nối cụ thể:\n" + ex.Message, "Bắt mạch lỗi");
            }
            finally { btnCreateKey.Enabled = true; }
        }

        /// <summary>
        /// Đăng nhập bằng Key
        /// </summary>
        private async Task XuLyDangNhapBangKeyAsync()
        {
            lblKeyLoginMessage.Text = "Đang xác minh Key..."; lblKeyLoginMessage.ForeColor = Color.Blue; btnKeyLogin.Enabled = false;
            try
            {
                var payload = new { key = txtLoginKey.Text };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                
                HttpResponseMessage response = await ApiClient.PostAsync("api/userkey/verify", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                    
                    if (responseData.GetProperty("success").GetBoolean())
                    {
                        var keyData = responseData.GetProperty("data");
                        string role = keyData.GetProperty("role").GetString() ?? "user";
                        Guid userId = Guid.Parse(keyData.GetProperty("userId").GetString() ?? "");
                        
                        lblKeyLoginMessage.Text = "✓ Key hợp lệ! Đăng nhập thành công!"; 
                        lblKeyLoginMessage.ForeColor = Color.Green;
                        
                        // Lưu key vào file
                        SaveKeyToFile(txtLoginKey.Text);
                        
                        MessageBox.Show($"Đăng nhập thành công!\nVai trò: {role}\nUser ID: {userId}", "Hệ thống");
                        
                        // Có thể chuyển sang Dashboard hoặc screen khác
                        // MessageBox.Show("Chuyển hướng tới Dashboard...");
                        // Ẩn Form đăng nhập hiện tại
                        this.Hide(); 

                    // Tạo và mở Form Dashboard, nhớ truyền token (hoặc key) qua
                        FormDashboard dashboard = new FormDashboard(userToken); 
                            dashboard.Show();
                    }
                    else
                    {
                        lblKeyLoginMessage.Text = $"✗ Key không hợp lệ hoặc đã hết hạn!"; 
                        lblKeyLoginMessage.ForeColor = Color.Red;
                    }
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    lblKeyLoginMessage.Text = $"✗ Lỗi ({response.StatusCode})! Xem chi tiết ở hộp thoại."; 
                    lblKeyLoginMessage.ForeColor = Color.Red;
                    MessageBox.Show("Chi tiết lỗi từ máy chủ:\n" + errorResponse, "Bắt mạch lỗi");
                }
            }
            catch (Exception ex)
            {
                lblKeyLoginMessage.Text = "✗ Lỗi Exception! Đảm bảo Backend đang chạy."; 
                lblKeyLoginMessage.ForeColor = Color.Red;
                MessageBox.Show("Lỗi kết nối cụ thể:\n" + ex.Message, "Bắt mạch lỗi");
            }
            finally { btnKeyLogin.Enabled = true; }
        }

        /// <summary>
        /// Lưu Key vào file
        /// </summary>
        private void SaveKeyToFile(string key)
        {
            try
            {
                string directory = Path.GetDirectoryName(keyFilePath) ?? "";
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(keyFilePath, key);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cảnh báo: Không thể lưu Key vào file. {ex.Message}", "Cảnh báo");
            }
        }

        /// <summary>
        /// Tải Key từ file
        /// </summary>
        private string? LoadKeyFromFile()
        {
            try
            {
                if (File.Exists(keyFilePath))
                    return File.ReadAllText(keyFilePath).Trim();
            }
            catch { }
            return null;
        }
    }
}