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
        private Button btnDeleteAttendance = null!;

        private void VeGiaoDienChamCong()
        {
            Color primaryBlue = Color.FromArgb(0, 102, 204);
            Color lightGray = Color.FromArgb(245, 247, 250);
            Color darkGray = Color.FromArgb(80, 80, 80);

            // 1. Panel chính
            pnlAttendance = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false, Padding = new Padding(20) };

            // Panel Top
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 140 };
            
            Label lblTitle = new Label { Text = "BẢNG KÊ CHẤM CÔNG NHÂN SỰ", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = primaryBlue, Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter };
            lblClock = new Label { Text = "00:00:00", Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = darkGray, Dock = DockStyle.Top, Height = 50, TextAlign = ContentAlignment.MiddleCenter };

            timerClock = new System.Windows.Forms.Timer { Interval = 1000 };
            timerClock.Tick += (s, e) => { lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy - HH:mm:ss"); };
            timerClock.Start();

            // Thanh công cụ
            Panel pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 40 };
            
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

            btnDeleteAttendance = new Button { Text = "🗑️ HỦY CHẤM CÔNG", Location = new Point(630, 4), Width = 150, Height = 30, BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnDeleteAttendance.FlatAppearance.BorderSize = 0;
            btnDeleteAttendance.Click += async (s, e) => {
                if (myCurrentRole != "admin" && myCurrentRole != "superadmin")
                {
                    MessageBox.Show("Chỉ Quản trị viên mới được quyền hủy chấm công!", "Lỗi Phân Quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (dgvAttendance.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn một dòng chấm công để hủy!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var empCode = dgvAttendance.SelectedRows[0].Cells["EmpCode"].Value?.ToString();
                var empName = dgvAttendance.SelectedRows[0].Cells["FullName"].Value?.ToString();
                if (string.IsNullOrEmpty(empCode)) return;

                var confirm = MessageBox.Show($"Bạn có chắc chắn muốn HỦY LỊCH SỬ CHẤM CÔNG ngày {dtpFilterDate.Value:dd/MM/yyyy} của nhân viên {empName} ({empCode}) không?\n\nHành động này không thể hoàn tác!", "Xác Nhận Hủy Chấm Công", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                
                if (confirm == DialogResult.Yes)
                {
                    await DeleteAttendanceAsync(empCode, dtpFilterDate.Value);
                }
            };

            pnlToolbar.Controls.AddRange(new Control[] { lblFilter, dtpFilterDate, btnRefreshData, btnCheckIn, btnCheckOut, btnDeleteAttendance });
            pnlTop.Controls.Add(pnlToolbar);
            pnlTop.Controls.Add(lblClock);
            pnlTop.Controls.Add(lblTitle);

            // BẢNG DỮ LIỆU
            dgvAttendance = new DataGridView
            {
                Dock = DockStyle.Fill,
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
            dgvAttendance.Columns.Add("BranchName", "Điểm danh tại");
            dgvAttendance.Columns.Add("CheckIn", "Giờ Vào");
            dgvAttendance.Columns.Add("CheckOut", "Giờ Ra");
            dgvAttendance.Columns.Add("Late", "Đi Trễ (Phút)");
            dgvAttendance.Columns.Add("Early", "Về Sớm (Phút)");

            dgvAttendance.CellFormatting += DgvAttendance_CellFormatting;

            // Panel Bottom
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(0, 15, 0, 0) };
            Button btnBackDash = new Button { Text = "QUAY LẠI BẢNG ĐIỀU KHIỂN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = darkGray, Dock = DockStyle.Left, Width = 250, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBackDash.FlatAppearance.BorderSize = 0;
            btnBackDash.Click += (s, e) => { SwitchPanel(pnlDashboard); };
            pnlBottom.Controls.Add(btnBackDash);

            pnlAttendance.Controls.Add(dgvAttendance);
            pnlAttendance.Controls.Add(pnlBottom);
            pnlAttendance.Controls.Add(pnlTop);

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
                                    row.TryGetProperty("branchName", out var bn) && bn.ValueKind == JsonValueKind.String ? bn.GetString() : "Không xác định",
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
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show("Lỗi máy chủ: " + error, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối mạng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefreshData.Text = "🔄 Tải Dữ Liệu";
                btnRefreshData.Enabled = true;
            }
        }

        private async Task DeleteAttendanceAsync(string userName, DateTime date)
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
                    string formattedDate = date.ToString("yyyy-MM-dd");
                    var response = await client.DeleteAsync($"https://localhost:44387/api/app/attendance/daily-attendance?userName={userName}&date={formattedDate}");

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Đã hủy chấm công thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadAttendanceDataAsync(date);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show("Lỗi khi hủy chấm công: " + error, "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    // Khi check-in, gửi kèm tọa độ giả lập (gần công ty Vĩnh Long)
                    HttpResponseMessage response;
                    if (action == "check-in")
                    {
                        double userLat = 10.2540;
                        double userLng = 105.9720;
                        
                        try
                        {
                            var accessStatus = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();
                            if (accessStatus == Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed)
                            {
                                var geolocator = new Windows.Devices.Geolocation.Geolocator { DesiredAccuracy = Windows.Devices.Geolocation.PositionAccuracy.High };
                                var pos = await geolocator.GetGeopositionAsync();
                                userLat = pos.Coordinate.Point.Position.Latitude;
                                userLng = pos.Coordinate.Point.Position.Longitude;
                            }
                            else
                            {
                                MessageBox.Show("Không có quyền truy cập GPS. Hệ thống sẽ không thể lấy vị trí hiện tại!", "Cảnh Báo GPS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi lấy tọa độ GPS: " + ex.Message, "Cảnh Báo GPS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        string latStr = userLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string lngStr = userLng.ToString(System.Globalization.CultureInfo.InvariantCulture);

                        response = await client.PostAsync(
                            $"/api/app/attendance/check-in?userLat={latStr}&userLng={lngStr}", null);
                    }
                    else
                    {
                        response = await client.PostAsync("/api/app/attendance/check-out", null);
                    }
                    
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