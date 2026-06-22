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
        private void VeGiaoDienChamCong()
        {
            // 1. Tạo Panel chứa toàn bộ nút
            pnlAttendance = new Panel
            {
                Location = new Point(0, 80), // Chỉnh lại tọa độ Y cho khớp với thanh Menu của bạn
                Size = new Size(500, 670),   // Chiếm phần còn lại của Form
                BackColor = Color.White,
                Visible = false              // Ẩn đi, chỉ hiện khi bấm vào Menu Chấm công
            };

            // Tiêu đề
            Label lblTitle = new Label
            {
                Text = "📍 CHẤM CÔNG HÀNG NGÀY",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204), // Xanh dương đậm
                AutoSize = true,
                Location = new Point(100, 30)
            };

            // Trạng thái (hiện kết quả check-in/out)
            lblAttendanceStatus = new Label
            {
                Text = "Trạng thái hôm nay: Đang chờ...",
                Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(50, 100),
                MaximumSize = new Size(400, 0)
            };

            // NÚT CHECK-IN (Xanh lá)
            btnCheckIn = new Button
            {
                Text = "📍 ĐIỂM DANH (CHECK-IN)",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 167, 69), 
                ForeColor = Color.White,
                Size = new Size(300, 60),
                Location = new Point(100, 170),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCheckIn.FlatAppearance.BorderSize = 0;
            btnCheckIn.Click += async (s, e) => await ThucHienChamCongAsync("check-in");

            // NÚT CHECK-OUT (Đỏ)
            btnCheckOut = new Button
            {
                Text = "🏃 TAN LÀM (CHECK-OUT)",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 53, 69), 
                ForeColor = Color.White,
                Size = new Size(300, 60),
                Location = new Point(100, 260),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCheckOut.FlatAppearance.BorderSize = 0;
            btnCheckOut.Click += async (s, e) => await ThucHienChamCongAsync("check-out");

            // Gom tất cả vào Panel
            pnlAttendance.Controls.Add(lblTitle);
            pnlAttendance.Controls.Add(lblAttendanceStatus);
            pnlAttendance.Controls.Add(btnCheckIn);
            pnlAttendance.Controls.Add(btnCheckOut);

            // Thêm Panel này vào Form chính
            this.Controls.Add(pnlAttendance);
        }

        // ==========================================
        // HÀM KẾT NỐI API XUỐNG BACKEND
        // ==========================================
        private async Task ThucHienChamCongAsync(string actionEndpoint)
        {
            try
            {
                // Tạm khóa nút để tránh user spam bấm 2 lần
                btnCheckIn.Enabled = false;
                btnCheckOut.Enabled = false;
                lblAttendanceStatus.Text = "Đang kết nối đến Server...";
                lblAttendanceStatus.ForeColor = Color.DarkOrange;

                using (var client = new HttpClient())
                {
                    // LƯU Ý: Nếu Port Server của bạn khác 44387, hãy sửa lại số ở đây nhé!
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
                        lblAttendanceStatus.ForeColor = Color.Green;
                    }
                    else
                    {
                        MessageBox.Show("Từ chối: " + responseString, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        lblAttendanceStatus.Text = "Lỗi: Hành động không được phép.";
                        lblAttendanceStatus.ForeColor = Color.Red;
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
                btnCheckIn.Enabled = true;
                btnCheckOut.Enabled = true;
            }
        }
    }
}