using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class FormDashboard : Form
    {
        private string userToken; 

        // Giao diện chính
        private Panel pnlDashboard = null!;
        private Panel pnlProfile = null!;
        
        // Các biến cho Thẻ nhân viên
        private Label lblProName = null!;
        private Label lblProRole = null!;
        private Label lblProDate = null!;
        
        // Avatar
        private PictureBox picAvatar = null!;
        private Button btnChangeAvatar = null!;
        
        // Các trường thông tin (Cho phép chỉnh sửa)
        private TextBox txtEditEmail = null!;
        private TextBox txtEditPhone = null!;
        private TextBox txtEditAddress = null!;
        private Button btnEditProfile = null!;
        
        private bool isEditMode = false; // Biến theo dõi trạng thái đang xem hay đang sửa

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

            Button btnLogoutDash = new Button { Text = "ĐĂNG XUẤT", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Gray, Location = new Point(startX, 530), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnLogoutDash.FlatAppearance.BorderSize = 0;
            btnLogoutDash.Click += (s, e) => { Application.Restart(); };

            pnlDashboard.Controls.AddRange(new Control[] { lblDashTitle, btnViewProfile, btnManageEmp, btnLogoutDash });

            // ==========================================
            // 2. KHUNG HỒ SƠ CÁ NHÂN (PROFILE CARD PREMIUM)
            // ==========================================
            pnlProfile = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false };
            
            Label lblProfileTitle = new Label { Text = "HỒ SƠ CÁ NHÂN", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = primaryGreen, Location = new Point(0, 20), Width = 500, Height = 40, TextAlign = ContentAlignment.MiddleCenter };

            // Mở rộng Card để chứa nhiều thông tin hơn
            Panel pnlCard = new Panel { Width = 400, Height = 500, BackColor = Color.White, Location = new Point(startX, 70), BorderStyle = BorderStyle.FixedSingle };
            
            // --- KHU VỰC AVATAR ---
            picAvatar = new PictureBox { Width = 100, Height = 100, Location = new Point(150, 15), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = lightGray };
            
            btnChangeAvatar = new Button { Text = "📷 Đổi ảnh", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, BackColor = darkGray, Location = new Point(150, 120), Width = 100, Height = 25, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnChangeAvatar.FlatAppearance.BorderSize = 0;
            btnChangeAvatar.Click += BtnChangeAvatar_Click; // Gắn sự kiện đổi ảnh

            // --- KHU VỰC TÊN & QUYỀN (Tăng Height để không bị lẹm chữ) ---
            lblProName = new Label { Text = "Đang tải...", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = darkGray, AutoSize = false, Width = 400, Height = 45, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 155) };
            lblProRole = new Label { Text = "ROLE", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryOrange, AutoSize = false, Width = 100, Height = 25, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(150, 200) };
            
            // --- KHU VỰC CHỈNH SỬA THÔNG TIN ---
            Label lblEmailTitle = new Label { Text = "✉️ Email:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(30, 245), AutoSize = true };
            txtEditEmail = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(130, 240), Width = 230, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.None };

            Label lblPhoneTitle = new Label { Text = "📞 SĐT:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(30, 285), AutoSize = true };
            txtEditPhone = new TextBox { Text = "Chưa cập nhật", Font = new Font("Segoe UI", 11F), Location = new Point(130, 280), Width = 230, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.None };

            Label lblAddressTitle = new Label { Text = "📍 Địa chỉ:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(30, 325), AutoSize = true };
            txtEditAddress = new TextBox { Text = "Chưa cập nhật", Font = new Font("Segoe UI", 11F), Location = new Point(130, 320), Width = 230, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.None };

            lblProDate = new Label { Text = "Tham gia: ...", Font = new Font("Segoe UI", 10F, FontStyle.Italic), ForeColor = Color.Gray, AutoSize = false, Width = 400, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 370) };

            // Nút Kích hoạt chế độ chỉnh sửa
            btnEditProfile = new Button { Text = "✍️ CHỈNH SỬA HỒ SƠ", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.MediumSeaGreen, Location = new Point(100, 420), Width = 200, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnEditProfile.FlatAppearance.BorderSize = 0;
            btnEditProfile.Click += BtnEditProfile_Click; // Gắn sự kiện bật/tắt sửa

            pnlCard.Controls.AddRange(new Control[] { 
                picAvatar, btnChangeAvatar, lblProName, lblProRole, 
                lblEmailTitle, txtEditEmail, lblPhoneTitle, txtEditPhone, lblAddressTitle, txtEditAddress, 
                lblProDate, btnEditProfile 
            });

            Button btnBackDash2 = new Button { Text = "QUAY LẠI", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryBlue, Location = new Point(startX, 590), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackDash2.FlatAppearance.BorderSize = 0;
            btnBackDash2.Click += (s, e) => { 
                if(isEditMode) { MessageBox.Show("Vui lòng lưu thông tin trước khi thoát!", "Nhắc nhở"); return; }
                pnlProfile.Visible = false; pnlDashboard.Visible = true; 
            };

            pnlProfile.Controls.AddRange(new Control[] { lblProfileTitle, pnlCard, btnBackDash2 });

            this.Controls.Add(pnlProfile);
            this.Controls.Add(pnlDashboard);
        }

        // ==========================================
        // CÁC HÀM XỬ LÝ SỰ KIỆN GIAO DIỆN
        // ==========================================

        private void BtnChangeAvatar_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn ảnh đại diện";
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // Tải ảnh lên giao diện
                    picAvatar.Image = Image.FromFile(ofd.FileName);
                    MessageBox.Show("Đã đổi ảnh trên giao diện! (Sẽ lưu lên server sau khi có API)", "Thành công");
                }
            }
        }

        private void BtnEditProfile_Click(object? sender, EventArgs e)
        {
            isEditMode = !isEditMode; // Đảo trạng thái

            if (isEditMode)
            {
                // BẬT CHẾ ĐỘ SỬA: Hiển thị khung viền, cho phép gõ phím
                txtEditEmail.ReadOnly = false; txtEditEmail.BorderStyle = BorderStyle.FixedSingle;
                txtEditPhone.ReadOnly = false; txtEditPhone.BorderStyle = BorderStyle.FixedSingle;
                txtEditAddress.ReadOnly = false; txtEditAddress.BorderStyle = BorderStyle.FixedSingle;
                
                btnEditProfile.Text = "💾 LƯU THÔNG TIN";
                btnEditProfile.BackColor = Color.OrangeRed;
            }
            else
            {
                // TẮT CHẾ ĐỘ SỬA (LƯU): Xóa khung viền, khóa gõ phím
                txtEditEmail.ReadOnly = true; txtEditEmail.BorderStyle = BorderStyle.None;
                txtEditPhone.ReadOnly = true; txtEditPhone.BorderStyle = BorderStyle.None;
                txtEditAddress.ReadOnly = true; txtEditAddress.BorderStyle = BorderStyle.None;
                
                btnEditProfile.Text = "✍️ CHỈNH SỬA HỒ SƠ";
                btnEditProfile.BackColor = Color.MediumSeaGreen;

                // TODO: Gọi API Backend để lưu các thông tin này vào CSDL
                MessageBox.Show("Dữ liệu đã được ghi nhận trên giao diện!\n(Cần cập nhật Backend để lưu vĩnh viễn)", "Lưu thành công");
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
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }
    }
}