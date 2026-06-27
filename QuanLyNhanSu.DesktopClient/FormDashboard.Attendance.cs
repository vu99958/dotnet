using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class FormDashboard
    {
        private Panel pnlAttendance = null!;
        private Label lblClock = null!;
        private System.Windows.Forms.Timer timerClock = null!;
        
        // CÁC BIẾN MỚI CHO GIAO DIỆN QUẢN LÝ HR
        private DataGridView dgvAttendance = null!;
        private DateTimePicker dtpFilterDate = null!;
        private Button btnRefreshData = null!;
        private Button btnCheckIn = null!;
        private Button btnCheckOut = null!;

      private void VeGiaoDienChamCong()
        {
            Color primaryBlue = Color.FromArgb(0, 102, 204);
            Color lightGray = Color.FromArgb(245, 247, 250);
            Color darkGray = Color.FromArgb(80, 80, 80);

            // 1. Panel chính
            pnlAttendance = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false };

            // 2. Tiêu đề và Đồng hồ (Tự động canh giữa)
            Label lblTitle = new Label { Text = "BẢNG KÊ CHẤM CÔNG NHÂN SỰ", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = primaryBlue, Location = new Point(0, 20), Width = pnlAttendance.Width, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleCenter };
            lblClock = new Label { Text = "00:00:00", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(0, 60), Width = pnlAttendance.Width, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleCenter };

            timerClock = new System.Windows.Forms.Timer { Interval = 1000 };
            timerClock.Tick += (s, e) => { lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy - HH:mm:ss"); };
            timerClock.Start();

            // 3. Thanh công cụ lọc dữ liệu (Chỉnh lại tọa độ chống đè nhau)
            Panel pnlToolbar = new Panel { Location = new Point(20, 110), Width = pnlAttendance.Width - 40, Height = 40, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            
            Label lblFilter = new Label { Text = "Chọn ngày xem:", Location = new Point(0, 8), AutoSize = true, Font = new Font("Segoe UI", 10F) };
            dtpFilterDate = new DateTimePicker { Location = new Point(110, 5), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 10F), Width = 120 };
            
            btnRefreshData = new Button { Text = "🔄 Tải Dữ Liệu", Location = new Point(240, 4), Width = 120, Height = 30, BackColor = primaryBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRefreshData.FlatAppearance.BorderSize = 0;
            btnRefreshData.Click += async (s, e) => await LoadAttendanceDataAsync(dtpFilterDate.Value);

            btnCheckIn = new Button { Text = "🌞 CHECK-IN", Location = new Point(370, 4), Width = 120, Height = 30, BackColor = Color.FromArgb(32, 161, 68), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCheckIn.FlatAppearance.BorderSize = 0;
            btnCheckIn.Click += async (s, e) => await PerformCheckInOutAsync("check-in");

            btnCheckOut = new Button { Text = "🌙 CHECK-OUT", Location = new Point(500, 4), Width = 120, Height = 30, BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCheckOut.FlatAppearance.BorderSize = 0;
            btnCheckOut.Click += async (s, e) => await PerformCheckInOutAsync("check-out");

            pnlToolbar.Controls.AddRange(new Control[] { lblFilter, dtpFilterDate, btnRefreshData, btnCheckIn, btnCheckOut });

            // 4. BẢNG DỮ LIỆU CHẤM CÔNG (Tự co giãn 4 phía)
            dgvAttendance = new DataGridView
            {
                Location = new Point(20, 160),
                Width = pnlAttendance.Width - 40,
                Height = pnlAttendance.Height - 240,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10F)
            };

            dgvAttendance.Columns.Add("EmpCode", "Mã NV");
            dgvAttendance.Columns.Add("FullName", "Họ Tên");
            dgvAttendance.Columns.Add("CheckIn", "Giờ Vào");
            dgvAttendance.Columns.Add("CheckOut", "Giờ Ra");
            dgvAttendance.Columns.Add("Late", "Đi Trễ (Phút)");
            dgvAttendance.Columns.Add("Early", "Về Sớm (Phút)");

            dgvAttendance.CellFormatting += DgvAttendance_CellFormatting;

            // 5. Nút Quay Lại (Ghim xuống đáy)
            Button btnBackDash = new Button { Text = "QUAY LẠI BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = darkGray, Location = new Point(20, pnlAttendance.Height - 60), Width = 250, Height = 40, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackDash.FlatAppearance.BorderSize = 0;
            btnBackDash.Click += (s, e) => { SwitchPanel(pnlDashboard); };

            pnlAttendance.Controls.AddRange(new Control[] { lblTitle, lblClock, pnlToolbar, dgvAttendance, btnBackDash });
            this.Controls.Add(pnlAttendance);
        }
        private void DgvAttendance_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && (e.ColumnIndex == 4 || e.ColumnIndex == 5)) // Áp dụng cho cột Đi Trễ (4) và Về Sớm (5)
            {
                // Nếu giá trị phút > 0 thì đổi màu chữ thành Đỏ và in Đậm
                if (e.Value != null && int.TryParse(e.Value.ToString(), out int minutes) && minutes > 0)
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.CellStyle.Font = new Font(e.CellStyle.Font ?? new Font("Segoe UI", 10F), FontStyle.Bold);
                }
            }
        }
        // ==========================================
        // HÀM KẾT NỐI API LẤY BÁO CÁO (GET)
        // ==========================================
        private async Task LoadAttendanceDataAsync(DateTime targetDate)
        {
            try
            {
                btnRefreshData.Text = "Đang tải...";
                btnRefreshData.Enabled = false;
                dgvAttendance.Rows.Clear();

                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri("https://localhost:44387/");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                    // Gọi API lấy danh sách theo ngày (Cần viết thêm API này ở Backend)
                    string formattedDate = targetDate.ToString("yyyy-MM-dd");
                    var response = await client.GetAsync($"/api/app/attendance/daily-report?date={formattedDate}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(jsonString))
                        {
                            foreach (JsonElement row in doc.RootElement.EnumerateArray())
                            {
                                dgvAttendance.Rows.Add(
                                    row.GetProperty("employeeCode").GetString() ?? "",
                                    row.GetProperty("employeeName").GetString() ?? "Không rõ",
                                    row.GetProperty("checkInTime").GetString() ?? "--:--",
                                    row.GetProperty("checkOutTime").GetString() ?? "--:--",
                                    row.GetProperty("lateMinutes").GetInt32().ToString(),
                                    row.GetProperty("earlyLeaveMinutes").GetInt32().ToString()
                                );
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Lỗi lấy dữ liệu: " + response.StatusCode, "Lỗi Server", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Mất kết nối Server: " + ex.Message, "Lỗi Mạng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefreshData.Text = "🔄 Tải Dữ Liệu";
                btnRefreshData.Enabled = true;
            }
        }

        private async Task PerformCheckInOutAsync(string action)
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri("https://localhost:44387/");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

                    // Call the corresponding API
                    var response = await client.PostAsync($"/api/app/attendance/{action}", null);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var resultString = await response.Content.ReadAsStringAsync();
                        MessageBox.Show("Thành công: " + resultString, "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Tải lại dữ liệu ngày hôm nay cho bảng Grid
                        dtpFilterDate.Value = DateTime.Now.Date;
                        await LoadAttendanceDataAsync(DateTime.Now.Date);

                        // Tải lại dữ liệu cho Biểu đồ ngoài Dashboard
                        await LoadDashboardChartsAsync();
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Thất bại: Mặc dù đã click nhưng không thành công.\n(Status {response.StatusCode})\n{errorContent}", "Lỗi Chấm Công", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối Server: " + ex.Message, "Lỗi Mạng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}