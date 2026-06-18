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
        private Label lblProEmail = null!;
        private Label lblProRole = null!;
        private Label lblProDate = null!;

        public FormDashboard(string token)
        {
            // Nhận và lưu trữ token
            userToken = token; 
            
            // Tắt tự động thu phóng và thiết lập Form
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Bảng Điều Khiển - Premium";
            this.Size = new Size(500, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 11F);

            // Xử lý sự kiện khi đóng Form Dashboard thì thoát luôn chương trình
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
            btnLogoutDash.Click += (s, e) => {
                // Khởi động lại ứng dụng để quay về màn hình đăng nhập
                Application.Restart();
            };

            pnlDashboard.Controls.AddRange(new Control[] { lblDashTitle, btnViewProfile, btnManageEmp, btnLogoutDash });

            // ==========================================
            // 2. KHUNG HỒ SƠ CÁ NHÂN (PROFILE CARD)
            // ==========================================
            pnlProfile = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false };
            
            Label lblProfileTitle = new Label { Text = "THẺ NHÂN VIÊN", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = primaryGreen, Location = new Point(0, 30), Width = 500, Height = 50, TextAlign = ContentAlignment.MiddleCenter };

            Panel pnlCard = new Panel { Width = 400, Height = 360, BackColor = Color.White, Location = new Point(startX, 100), BorderStyle = BorderStyle.FixedSingle };
            Label lblAvatar = new Label { Text = "👤", Font = new Font("Segoe UI Emoji", 60F), AutoSize = false, Width = 120, Height = 120, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(140, 20), ForeColor = primaryBlue };
            lblProName = new Label { Text = "Đang tải...", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = darkGray, AutoSize = false, Width = 400, Height = 40, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 150) };
            lblProRole = new Label { Text = "ROLE", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryOrange, AutoSize = false, Width = 120, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(140, 200) };
            lblProEmail = new Label { Text = "email@domain.com", Font = new Font("Segoe UI", 12F), ForeColor = Color.Gray, AutoSize = false, Width = 400, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 250) };
            lblProDate = new Label { Text = "Tham gia: ...", Font = new Font("Segoe UI", 10F, FontStyle.Italic), ForeColor = Color.Gray, AutoSize = false, Width = 400, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 300) };

            pnlCard.Controls.AddRange(new Control[] { lblAvatar, lblProName, lblProRole, lblProEmail, lblProDate });

            Button btnBackDash2 = new Button { Text = "QUAY LẠI", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryBlue, Location = new Point(startX, 490), Width = width, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackDash2.FlatAppearance.BorderSize = 0;
            btnBackDash2.Click += (s, e) => { pnlProfile.Visible = false; pnlDashboard.Visible = true; };

            pnlProfile.Controls.AddRange(new Control[] { lblProfileTitle, pnlCard, btnBackDash2 });

            this.Controls.Add(pnlProfile);
            this.Controls.Add(pnlDashboard);
        }

        private async Task LoadMyProfileAsync()
        {
            lblProName.Text = "Đang tải dữ liệu...";
            lblProRole.Text = "...";
            try
            {
                if (string.IsNullOrEmpty(userToken))
                {
                    lblProName.Text = "Lỗi xác thực Token!";
                    return;
                }

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
                        lblProEmail.Text = responseData.GetProperty("email").GetString();
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