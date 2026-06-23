using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuanLyNhanSu.DesktopClient
{
    // Bắt buộc dùng partial class để nối với file FormDashboard.cs
    public partial class FormDashboard
    {
        // CÁC BIẾN CHỈ DÀNH CHO MODULE CHẤM CÔNG SẼ ĐƯỢC ĐẶT Ở ĐÂY
        private Panel pnlAttendance = null!;
        private Label lblClock = null!, lblAttendanceStatus = null!;
        private Button btnCheckIn = null!, btnCheckOut = null!;
        private System.Windows.Forms.Timer timerClock = null!;

        private void VeGiaoDienChamCong()
        {
            Color primaryBlue = Color.FromArgb(0, 102, 204), primaryGreen = Color.FromArgb(32, 161, 68);
            Color dangerRed = Color.FromArgb(220, 53, 69);
            Color lightGray = Color.FromArgb(245, 247, 250), darkGray = Color.FromArgb(80, 80, 80);
            int startX = 50, width = 400; // Tinh chỉnh lại tọa độ để nằm giữa đẹp hơn

            // 1. Tạo Panel chuẩn DockStyle.Fill
            pnlAttendance = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false };
            
            // 2. Tiêu đề (Đã thu nhỏ Font xuống 18F để tránh tràn)
            Label lblTitle = new Label { Text = "CHẤM CÔNG LÀM VIỆC", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = primaryBlue, Location = new Point(0, 30), Width = 500, Height = 40, TextAlign = ContentAlignment.MiddleCenter };

            // 3. ĐỒNG HỒ THỜI GIAN THỰC (Đã thu nhỏ Font xuống 32F và tăng Height)
            lblClock = new Label { Text = "00:00:00", Font = new Font("Segoe UI", 32F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(0, 70), Width = 500, Height = 80, TextAlign = ContentAlignment.MiddleCenter };
            
            timerClock = new System.Windows.Forms.Timer { Interval = 1000 };
            timerClock.Tick += (s, e) => { lblClock.Text = DateTime.Now.ToString("HH:mm:ss"); };
            timerClock.Start();

            // 4. Khung trắng chứa nội dung (Card)
            Panel pnlCard = new Panel { Width = 400, Height = 320, BackColor = Color.White, Location = new Point(50, 160), BorderStyle = BorderStyle.FixedSingle };

            // Trạng thái
            lblAttendanceStatus = new Label
            {
                Text = "Trạng thái hôm nay:\nChưa điểm danh",
                Font = new Font("Segoe UI", 12F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = false,
                Width = 400,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 20)
            };

            // NÚT CHECK-IN (Xanh lá - Font 13F)
            btnCheckIn = new Button
            {
                Text = "📍 ĐIỂM DANH VÀO CA",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                BackColor = primaryGreen, 
                ForeColor = Color.White,
                Size = new Size(320, 60),
                Location = new Point(40, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCheckIn.FlatAppearance.BorderSize = 0;
            btnCheckIn.Click += async (s, e) => await ThucHienChamCongAsync("check-in");

            // NÚT CHECK-OUT (Đỏ - Đổi tên và Font 13F)
            btnCheckOut = new Button
            {
                Text = "🏃 ĐIỂM DANH TAN CA", // 👉 Đã sửa theo đúng ý bạn
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                BackColor = dangerRed, 
                ForeColor = Color.White,
                Size = new Size(320, 60),
                Location = new Point(40, 180),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCheckOut.FlatAppearance.BorderSize = 0;
            btnCheckOut.Click += async (s, e) => await ThucHienChamCongAsync("check-out");

            pnlCard.Controls.AddRange(new Control[] { lblAttendanceStatus, btnCheckIn, btnCheckOut });

            // 5. Nút Quay Lại
            Button btnBackDash = new Button { Text = "QUAY LẠI BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryBlue, Location = new Point(50, 600), Width = 400, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackDash.FlatAppearance.BorderSize = 0;
            btnBackDash.Click += (s, e) => { SwitchPanel(pnlDashboard); };

            // Gom tất cả vào Panel chính
            pnlAttendance.Controls.AddRange(new Control[] { lblTitle, lblClock, pnlCard, btnBackDash });
            this.Controls.Add(pnlAttendance);
        }

        // ==========================================
        // HÀM KẾT NỐI API XUỐNG BACKEND
        // ==========================================
        private async Task ThucHienChamCongAsync(string actionEndpoint)
        {
            try
            {
                // Khóa nút để tránh spam
                btnCheckIn.Enabled = false;
                btnCheckOut.Enabled = false;
                lblAttendanceStatus.Text = "Đang kết nối đến Server...";
                lblAttendanceStatus.ForeColor = Color.DarkOrange;

                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri("https://localhost:44387/");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                    var response = await client.PostAsync($"/api/app/attendance/{actionEndpoint}", null);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        lblAttendanceStatus.Text = $"Cập nhật mới nhất ({DateTime.Now:HH:mm}):\n" + responseString;
                        lblAttendanceStatus.ForeColor = Color.FromArgb(32, 161, 68); // Xanh lá
                    }
                    else
                    {
                        MessageBox.Show("Từ chối: " + responseString, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        lblAttendanceStatus.Text = "Lỗi: " + responseString;
                        lblAttendanceStatus.ForeColor = Color.FromArgb(220, 53, 69); // Đỏ
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể kết nối đến Server: " + ex.Message, "Lỗi Mạng", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblAttendanceStatus.Text = "Lỗi kết nối Server!";
            }
            finally
            {
                // Mở khóa lại nút bấm
                btnCheckIn.Enabled = true;
                btnCheckOut.Enabled = true;
            }
        }
    }
}