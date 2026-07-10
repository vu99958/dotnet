using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using QuanLyNhanSu.DesktopClient.Services;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class FormDashboard
    {
        // ==========================================
        // BIẾN UI CHO MÀN HÌNH BÁO CÁO TỔNG HỢP THÁNG
        // ==========================================
        private Panel pnlMonthlyReport = null!;
        private DataGridView dgvMonthlyReport = null!;
        private DateTimePicker dtpFromDate = null!;
        private DateTimePicker dtpToDate = null!;
        private Button btnLoadMonthlyReport = null!;

        // ==========================================
        // HÀM VẼ GIAO DIỆN BÁO CÁO TỔNG HỢP THÁNG
        // ==========================================
        private void VeGiaoDienBaoCaoThang()
        {
            Color primaryBlue = Color.FromArgb(0, 102, 204);
            Color lightGray = Color.FromArgb(245, 247, 250);
            Color darkGray = Color.FromArgb(80, 80, 80);
            Color accentPurple = Color.FromArgb(111, 66, 193);

            // 1. PANEL CHÍNH (ẩn mặc định, hiện khi bấm menu)
            pnlMonthlyReport = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = lightGray, 
                Visible = false, 
                Padding = new Padding(20) 
            };

            // ==========================================
            // VÙNG TOP: Tiêu đề + Toolbar chọn ngày
            // ==========================================
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 130, BackColor = lightGray };

            // Tiêu đề
            Label lblTitle = new Label
            {
                Text = "📊 BÁO CÁO TỔNG HỢP CHẤM CÔNG THEO THÁNG",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = accentPurple,
                Dock = DockStyle.Top,
                Height = 45,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblSubTitle = new Label
            {
                Text = "Dùng để chốt lương cuối tháng — Chỉ Admin/SuperAdmin mới có quyền xem",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.Gray,
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Thanh công cụ chọn khoảng ngày
            Panel pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(0, 5, 0, 5) };

            Label lblFrom = new Label 
            { 
                Text = "Từ ngày:", 
                Location = new Point(20, 12), 
                AutoSize = true, 
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), 
                ForeColor = darkGray 
            };

            dtpFromDate = new DateTimePicker
            {
                Location = new Point(95, 8),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10F),
                Width = 130,
                // Mặc định: Ngày 1 của tháng hiện tại
                Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
            };

            Label lblTo = new Label 
            { 
                Text = "Đến ngày:", 
                Location = new Point(240, 12), 
                AutoSize = true, 
                Font = new Font("Segoe UI", 10F, FontStyle.Bold), 
                ForeColor = darkGray 
            };

            dtpToDate = new DateTimePicker
            {
                Location = new Point(325, 8),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10F),
                Width = 130,
                // Mặc định: Ngày hiện tại
                Value = DateTime.Now.Date
            };

            btnLoadMonthlyReport = new Button
            {
                Text = "📊 Xuất Báo Cáo",
                Location = new Point(475, 5),
                Width = 160,
                Height = 35,
                BackColor = accentPurple,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLoadMonthlyReport.FlatAppearance.BorderSize = 0;
            btnLoadMonthlyReport.Click += async (s, e) => await LoadMonthlyReportAsync();

            pnlToolbar.Controls.AddRange(new Control[] { lblFrom, dtpFromDate, lblTo, dtpToDate, btnLoadMonthlyReport });

            // Thứ tự Dock: Add ngược (cái nào add trước sẽ nằm dưới)
            pnlTop.Controls.Add(pnlToolbar);   // Thanh toolbar ở dưới
            pnlTop.Controls.Add(lblSubTitle);   // Phụ đề ở giữa
            pnlTop.Controls.Add(lblTitle);      // Tiêu đề ở trên cùng

            // ==========================================
            // VÙNG GIỮA: BẢNG DỮ LIỆU (DataGridView)
            // ==========================================
            dgvMonthlyReport = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(230, 230, 230),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Padding = new Padding(5, 3, 5, 3)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(41, 53, 65),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Padding = new Padding(5)
                },
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40
            };

            // Định nghĩa các cột (mapping với MonthlyAttendanceDto)
            dgvMonthlyReport.Columns.Add("FullName", "Tên Nhân Viên");
            dgvMonthlyReport.Columns.Add("BranchName", "Chi Nhánh");
            dgvMonthlyReport.Columns.Add("TotalWorkDays", "Số Ngày Công");
            dgvMonthlyReport.Columns.Add("TotalLateMinutes", "Tổng Phút Trễ");
            dgvMonthlyReport.Columns.Add("TotalEarlyLeaveMinutes", "Tổng Phút Về Sớm");
            dgvMonthlyReport.Columns.Add("TotalAbsentDays", "Vắng Không Phép");
            dgvMonthlyReport.Columns.Add("TotalLeaveDays", "Nghỉ Có Phép");
            dgvMonthlyReport.Columns.Add("TotalMissingCheckOuts", "Quên Check-out");

            // Ẩn cột UserId (thêm nhưng ẩn)
            dgvMonthlyReport.Columns.Add("UserId", "UserId");
            dgvMonthlyReport.Columns["UserId"].Visible = false;

            // Căn giữa các cột số liệu
            for (int i = 2; i <= 7; i++)
            {
                dgvMonthlyReport.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Gắn sự kiện tô màu tự động
            dgvMonthlyReport.CellFormatting += DgvMonthlyReport_CellFormatting;

            // ==========================================
            // VÙNG BOTTOM: Nút quay lại
            // ==========================================
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(0, 15, 0, 0) };

            Button btnBackDash = new Button
            {
                Text = "⬅ QUAY LẠI BẢNG ĐIỀU KHIỂN",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = darkGray,
                Dock = DockStyle.Left,
                Width = 280,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBackDash.FlatAppearance.BorderSize = 0;
            btnBackDash.Click += (s, e) => { SwitchPanel(pnlDashboard); };
            pnlBottom.Controls.Add(btnBackDash);

            // ==========================================
            // GẮN TẤT CẢ VÀO PANEL CHÍNH
            // ==========================================
            pnlMonthlyReport.Controls.Add(dgvMonthlyReport);   // Fill (ở giữa)
            pnlMonthlyReport.Controls.Add(pnlBottom);          // Bottom
            pnlMonthlyReport.Controls.Add(pnlTop);             // Top

            this.Controls.Add(pnlMonthlyReport);
        }

        // ==========================================
        // SỰ KIỆN: TÔ MÀU TỰ ĐỘNG CHO CÁC Ô RỦI RO
        // ==========================================
        // Logic: Nếu Vắng không phép > 0 hoặc Quên Check-out > 0
        //        → Tô nền cam nhạt/đỏ nhạt + chữ đỏ đậm để HR chú ý
        // ==========================================
        private void DgvMonthlyReport_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;

            string colName = dgvMonthlyReport.Columns[e.ColumnIndex].Name;

            // Cột "Vắng Không Phép" (index 5) — tô đỏ nhạt nếu > 0
            if (colName == "TotalAbsentDays")
            {
                if (int.TryParse(e.Value.ToString(), out int absentDays) && absentDays > 0)
                {
                    e.CellStyle.BackColor = Color.LightCoral;       // Đỏ nhạt nền
                    e.CellStyle.ForeColor = Color.DarkRed;          // Đỏ đậm chữ
                    e.CellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
            }

            // Cột "Quên Check-out" (index 7) — tô cam nhạt nếu > 0
            if (colName == "TotalMissingCheckOuts")
            {
                if (int.TryParse(e.Value.ToString(), out int missingCO) && missingCO > 0)
                {
                    e.CellStyle.BackColor = Color.LightSalmon;      // Cam nhạt nền
                    e.CellStyle.ForeColor = Color.DarkRed;          // Đỏ đậm chữ
                    e.CellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
            }

            // Cột "Tổng Phút Trễ" — tô vàng nhạt nếu > 0
            if (colName == "TotalLateMinutes")
            {
                if (int.TryParse(e.Value.ToString(), out int lateMins) && lateMins > 0)
                {
                    e.CellStyle.ForeColor = Color.OrangeRed;
                    e.CellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
            }

            // Cột "Tổng Phút Về Sớm" — tô vàng nhạt nếu > 0
            if (colName == "TotalEarlyLeaveMinutes")
            {
                if (int.TryParse(e.Value.ToString(), out int earlyMins) && earlyMins > 0)
                {
                    e.CellStyle.ForeColor = Color.OrangeRed;
                    e.CellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
            }
        }

        // ==========================================
        // HÀM GỌI API LẤY BÁO CÁO TỔNG HỢP THÁNG (GET)
        // ==========================================
        private async Task LoadMonthlyReportAsync()
        {
            try
            {
                // Khóa nút để tránh bấm trùng
                btnLoadMonthlyReport.Text = "⏳ Đang tải...";
                btnLoadMonthlyReport.Enabled = false;
                dgvMonthlyReport.Rows.Clear();

                // Lấy giá trị ngày từ 2 DateTimePicker
                string fromDate = dtpFromDate.Value.ToString("yyyy-MM-dd");
                string toDate = dtpToDate.Value.ToString("yyyy-MM-dd");

                // Gọi API Backend: /api/app/attendance/monthly-report
                var response = await ApiClient.GetAsync($"api/app/attendance/monthly-report?fromDate={fromDate}&toDate={toDate}", userToken);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        foreach (JsonElement row in doc.RootElement.EnumerateArray())
                        {
                            dgvMonthlyReport.Rows.Add(
                                row.GetProperty("fullName").GetString() ?? "Không rõ",
                                row.TryGetProperty("branchName", out var bn) && bn.ValueKind == JsonValueKind.String ? bn.GetString() : "Không xác định",
                                row.GetProperty("totalWorkDays").GetInt32().ToString(),
                                row.GetProperty("totalLateMinutes").GetInt32().ToString(),
                                row.GetProperty("totalEarlyLeaveMinutes").GetInt32().ToString(),
                                row.GetProperty("totalAbsentDays").GetInt32().ToString(),
                                row.GetProperty("totalLeaveDays").GetInt32().ToString(),
                                row.GetProperty("totalMissingCheckOuts").GetInt32().ToString(),
                                row.GetProperty("userId").GetString() ?? ""
                            );
                        }
                    }

                    // Thông báo kết quả
                    if (dgvMonthlyReport.Rows.Count == 0)
                    {
                        MessageBox.Show("Không có dữ liệu trong khoảng thời gian này.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show("Lỗi máy chủ: " + error, "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối mạng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Luôn mở khóa nút dù thành công hay thất bại
                btnLoadMonthlyReport.Text = "📊 Xuất Báo Cáo";
                btnLoadMonthlyReport.Enabled = true;
            }
        }
    }
}
