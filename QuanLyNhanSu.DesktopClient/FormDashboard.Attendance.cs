using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class FormDashboard
    {
        // Các biến UI cho phần Chấm công
        private Panel pnlAttendance = null!;
        private Button btnCheckIn = null!, btnCheckOut = null!;
        private Label lblAttendanceStatus = null!;

        // Hàm này dùng để vẽ bảng điểm danh
    // Hàm này dùng để vẽ bảng điểm danh
        private void VeGiaoDienChamCong()
        {
            // 1. Tạo Panel chuẩn DockStyle.Fill giống hệt pnlProfile của bạn
            pnlAttendance = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250), // Màu nền xám nhạt (lightGray)
                Visible = false              
            };

            // 2. Tiêu đề chính
            Label lblTitle = new Label
            {
                Text = "CHẤM CÔNG HÀNG NGÀY",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(23, 162, 184), // Màu xanh ngọc
                Location = new Point(0, 30),
                Width = 500,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 3. Khung trắng chứa nội dung (Card)
            Panel pnlCard = new Panel { Width = 400, Height = 450, BackColor = Color.White, Location = new Point(40, 90), BorderStyle = BorderStyle.FixedSingle };

            // Trạng thái
            lblAttendanceStatus = new Label
            {
                Text = "Trạng thái hôm nay:\nChưa điểm danh",
                Font = new Font("Segoe UI", 12F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = false,
                Width = 400,
                Height = 80,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 40)
            };

            // NÚT CHECK-IN (Xanh lá)
            btnCheckIn = new Button
            {
                Text = "📍 ĐIỂM DANH LÀM VIỆC",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 167, 69), 
                ForeColor = Color.White,
                Size = new Size(300, 60),
                Location = new Point(50, 150),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCheckIn.FlatAppearance.BorderSize = 0;
            btnCheckIn.Click += async (s, e) => await ThucHienChamCongAsync("check-in");

            // NÚT CHECK-OUT (Đỏ)
            btnCheckOut = new Button
            {
                Text = "🏃 XÁC NHẬN TAN LÀM",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 53, 69), 
                ForeColor = Color.White,
                Size = new Size(300, 60),
                Location = new Point(50, 240),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCheckOut.FlatAppearance.BorderSize = 0;
            btnCheckOut.Click += async (s, e) => await ThucHienChamCongAsync("check-out");

            pnlCard.Controls.AddRange(new Control[] { lblAttendanceStatus, btnCheckIn, btnCheckOut });

            // 4. Nút Quay Lại (Quan trọng để không bị kẹt)
            Button btnBackDash = new Button 
            { 
                Text = "QUAY LẠI BẢNG ĐIỀU KHIỂN", 
                Font = new Font("Segoe UI", 11F, FontStyle.Bold), 
                ForeColor = Color.White, 
                BackColor = Color.FromArgb(0, 102, 204), // Xanh dương
                Location = new Point(40, 600), 
                Width = 400, 
                Height = 50, 
                FlatStyle = FlatStyle.Flat, 
                Cursor = Cursors.Hand 
            };
            btnBackDash.FlatAppearance.BorderSize = 0; 
            btnBackDash.Click += (s, e) => { SwitchPanel(pnlDashboard); };

            // Gom tất cả vào Panel chính
            pnlAttendance.Controls.AddRange(new Control[] { lblTitle, pnlCard, btnBackDash });
            this.Controls.Add(pnlAttendance);
        }

        // ==========================================
        // HÀM KẾT NỐI API XUỐNG BACKEND
        // ==========================================
        private async Task ThucHienChamCongAsync(string actionEndpoint)
        {
            try
            {
                // Tạm khóa nút để tránh người dùng spam click (bấm liên tục 2 lần)
                btnCheckIn.Enabled = false;
                btnCheckOut.Enabled = false;
                lblAttendanceStatus.Text = "Đang kết nối đến Server...";
                lblAttendanceStatus.ForeColor = Color.DarkOrange;

              // 👉 THÊM 2 DÒNG NÀY ĐỂ VƯỢT QUA TƯỜNG LỬA SSL LOCALHOST
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                // 👉 ĐƯA HANDLER VÀO HTTPCLIENT
                using (var client = new HttpClient(handler))
                {
                    // LƯU Ý: Đảm bảo Port Server của bạn vẫn là 44387
                    client.BaseAddress = new Uri("https://localhost:44387/");
                    
                    // Gắn Thẻ ra vào (Token) để chứng minh thân phận
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                    // Gọi API (ABP tự động map CheckInAsync thành /api/app/attendance/check-in)
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