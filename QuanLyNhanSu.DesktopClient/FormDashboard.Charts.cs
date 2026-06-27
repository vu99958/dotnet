using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class FormDashboard
    {
        // ── BIẾN CHART ──────────────────────────────────
        private Panel pnlCharts = null!;
        private Chart chartAttendance = null!;
        private Chart chartSalary = null!;

        /// <summary>
        /// Vẽ khu vực biểu đồ trên Dashboard (gọi trong constructor).
        /// Mặc định ẩn, chỉ hiện khi Admin đăng nhập.
        /// </summary>
        private void VeGiaoDienBieuDo()
        {
            // ── MÀU SẮC ─────────────────────────────────
            Color bgPanel = Color.FromArgb(248, 249, 252);

            // ── PANEL CHỨA 2 BIỂU ĐỒ ───────────────────
            pnlCharts = new Panel
            {
                BackColor = bgPanel,
                Visible = false  // Ẩn mặc định, Admin mới hiện
            };

            // ══════════════════════════════════════════════
            //  1. PIE CHART — CHẤM CÔNG HÔM NAY
            // ══════════════════════════════════════════════
            chartAttendance = new Chart
            {
                BackColor = Color.White,
                BorderlineDashStyle = ChartDashStyle.Solid,
                BorderlineColor = Color.FromArgb(230, 232, 236),
                BorderlineWidth = 1
            };

            var areaAttendance = new ChartArea("AreaAttendance")
            {
                BackColor = Color.White,
                Position = new ElementPosition(0, 12, 100, 88)
            };
            areaAttendance.Area3DStyle.Enable3D = true;
            areaAttendance.Area3DStyle.Inclination = 45;
            areaAttendance.Area3DStyle.IsRightAngleAxes = false;
            areaAttendance.Area3DStyle.LightStyle = LightStyle.Realistic;
            chartAttendance.ChartAreas.Add(areaAttendance);

            var seriesAttendance = new Series("Attendance")
            {
                ChartType = SeriesChartType.Pie,
                ChartArea = "AreaAttendance",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                IsValueShownAsLabel = true,
                LabelFormat = "#0",
                BorderWidth = 2,
                BorderColor = Color.White
            };
            seriesAttendance["PieLabelStyle"] = "Outside";
            seriesAttendance["PieDrawingStyle"] = "SoftEdge";
            chartAttendance.Series.Add(seriesAttendance);

            // Tiêu đề
            var titleAtt = new Title
            {
                Text = "📊 Chấm Công Hôm Nay",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 70),
                Docking = Docking.Top,
                Alignment = ContentAlignment.MiddleCenter
            };
            chartAttendance.Titles.Add(titleAtt);

            // Legend
            var legendAtt = new Legend
            {
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(80, 80, 80),
                BackColor = Color.Transparent,
                BorderColor = Color.Transparent
            };
            chartAttendance.Legends.Add(legendAtt);

            // ══════════════════════════════════════════════
            //  2. COLUMN CHART — QUỸ LƯƠNG NĂM NAY
            // ══════════════════════════════════════════════
            chartSalary = new Chart
            {
                BackColor = Color.White,
                BorderlineDashStyle = ChartDashStyle.Solid,
                BorderlineColor = Color.FromArgb(230, 232, 236),
                BorderlineWidth = 1
            };

            var areaSalary = new ChartArea("AreaSalary")
            {
                BackColor = Color.White,
                Position = new ElementPosition(2, 14, 96, 82)
            };
            areaSalary.AxisX.MajorGrid.Enabled = false;
            areaSalary.AxisX.LabelStyle.Font = new Font("Segoe UI", 7.5F);
            areaSalary.AxisX.LabelStyle.ForeColor = Color.FromArgb(100, 100, 110);
            areaSalary.AxisX.LineColor = Color.FromArgb(210, 210, 215);
            areaSalary.AxisX.Interval = 1;

            areaSalary.AxisY.MajorGrid.LineColor = Color.FromArgb(235, 237, 240);
            areaSalary.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            areaSalary.AxisY.LabelStyle.Font = new Font("Segoe UI", 7F);
            areaSalary.AxisY.LabelStyle.ForeColor = Color.FromArgb(100, 100, 110);
            areaSalary.AxisY.LabelStyle.Format = "#,0";
            areaSalary.AxisY.LineColor = Color.FromArgb(210, 210, 215);
            areaSalary.AxisY.Title = "VNĐ";
            areaSalary.AxisY.TitleFont = new Font("Segoe UI", 7.5F, FontStyle.Italic);
            areaSalary.AxisY.TitleForeColor = Color.Gray;

            chartSalary.ChartAreas.Add(areaSalary);

            var seriesSalary = new Series("Salary")
            {
                ChartType = SeriesChartType.Column,
                ChartArea = "AreaSalary",
                Font = new Font("Segoe UI", 7F),
                IsValueShownAsLabel = false,
                BorderWidth = 0
            };
            seriesSalary["DrawingStyle"] = "Cylinder";
            seriesSalary["PointWidth"] = "0.6";
            chartSalary.Series.Add(seriesSalary);

            // Tiêu đề
            var titleSal = new Title
            {
                Text = $"💰 Quỹ Lương Công Ty Năm {DateTime.Now.Year}",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 70),
                Docking = Docking.Top,
                Alignment = ContentAlignment.MiddleCenter
            };
            chartSalary.Titles.Add(titleSal);

            // ── THÊM VÀO PANEL ──────────────────────────
            pnlCharts.Controls.Add(chartAttendance);
            pnlCharts.Controls.Add(chartSalary);
            pnlDashboard.Controls.Add(pnlCharts);
        }

        /// <summary>
        /// Gọi 2 API thống kê và đổ dữ liệu vào biểu đồ.
        /// Chỉ được gọi khi Admin đăng nhập thành công.
        /// </summary>
        private async Task LoadDashboardChartsAsync()
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (s, c, ch, ssl) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri("https://localhost:44387/");
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", userToken);

                    // ─── 1. GỌI API CHẤM CÔNG HÔM NAY ──────────
                    await LoadAttendancePieChartAsync(client);

                    // ─── 2. GỌI API QUỸ LƯƠNG NĂM NAY ──────────
                    await LoadSalaryColumnChartAsync(client);
                }
            }
            catch (Exception ex)
            {
                // Không crash form, chỉ log lỗi lên console
                System.Diagnostics.Debug.WriteLine($"[Dashboard Charts] Lỗi: {ex.Message}");
            }
        }

        /// <summary>Pie Chart: Đúng giờ vs Đi trễ/Về sớm.</summary>
        private async Task LoadAttendancePieChartAsync(HttpClient client)
        {
            try
            {
                var response = await client.GetAsync("api/app/dashboard/today-attendance-stats");

                if (!response.IsSuccessStatusCode) 
                {
                    MessageBox.Show($"Pie Chart API Failed: {response.StatusCode}\n{await response.Content.ReadAsStringAsync()}", "Lỗi API");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var data = doc.RootElement;
                    int onTime = data.GetProperty("onTimeCount").GetInt32();
                    int lateOrEarly = data.GetProperty("lateOrEarlyCount").GetInt32();

                    var series = chartAttendance.Series["Attendance"];
                    series.Points.Clear();

                    if (onTime == 0 && lateOrEarly == 0)
                    {
                        // Chưa có dữ liệu chấm công
                        series.Points.AddXY("Chưa có dữ liệu", 1);
                        series.Points[0].Color = Color.FromArgb(200, 200, 205);
                        series.Points[0].LegendText = "Chưa có dữ liệu";
                        series.Points[0].Label = "0";
                        return;
                    }

                    // Đúng giờ — Xanh lá tươi
                    var ptOnTime = series.Points[series.Points.AddXY("Đúng giờ", onTime)];
                    ptOnTime.Color = Color.FromArgb(46, 204, 113);
                    ptOnTime.BackSecondaryColor = Color.FromArgb(39, 174, 96);
                    ptOnTime.BackGradientStyle = GradientStyle.DiagonalLeft;
                    ptOnTime.LegendText = $"Đúng giờ ({onTime})";
                    ptOnTime.Label = onTime.ToString();

                    // Đi trễ / Về sớm — Cam đỏ
                    var ptLate = series.Points[series.Points.AddXY("Đi trễ / Về sớm", lateOrEarly)];
                    ptLate.Color = Color.FromArgb(231, 76, 60);
                    ptLate.BackSecondaryColor = Color.FromArgb(192, 57, 43);
                    ptLate.BackGradientStyle = GradientStyle.DiagonalLeft;
                    ptLate.LegendText = $"Trễ / Sớm ({lateOrEarly})";
                    ptLate.Label = lateOrEarly.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[PieChart] Exception: {ex.Message}\n{ex.StackTrace}", "Lỗi Exception");
            }
        }

        /// <summary>Column Chart: Tổng lương Net 12 tháng.</summary>
        private async Task LoadSalaryColumnChartAsync(HttpClient client)
        {
            try
            {
                var response = await client.GetAsync("api/app/dashboard/monthly-salary-stats");

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Column Chart API Failed: {response.StatusCode}\n{await response.Content.ReadAsStringAsync()}", "Lỗi API");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var dataArray = doc.RootElement;
                    var series = chartSalary.Series["Salary"];
                    series.Points.Clear();

                    // Gradient màu cột: Xanh dương → Tím (mỗi tháng đậm hơn)
                    Color[] columnColors = new Color[]
                    {
                        Color.FromArgb(52, 152, 219),   // T1  — Xanh dương
                        Color.FromArgb(55, 141, 216),   // T2
                        Color.FromArgb(63, 130, 210),   // T3
                        Color.FromArgb(75, 119, 204),   // T4
                        Color.FromArgb(88, 108, 198),   // T5
                        Color.FromArgb(101, 97, 192),   // T6  — Xanh tím
                        Color.FromArgb(114, 86, 186),   // T7
                        Color.FromArgb(127, 75, 180),   // T8
                        Color.FromArgb(140, 64, 174),   // T9
                        Color.FromArgb(142, 68, 173),   // T10 — Tím
                        Color.FromArgb(155, 55, 165),   // T11
                        Color.FromArgb(165, 45, 158)    // T12 — Tím đậm
                    };

                    int index = 0;
                    foreach (JsonElement item in dataArray.EnumerateArray())
                    {
                        int month = item.GetProperty("month").GetInt32();
                        decimal totalNet = item.GetProperty("totalNetSalary").GetDecimal();

                        string label = $"T{month}";
                        var pt = series.Points[series.Points.AddXY(label, (double)totalNet)];

                        // Áp màu gradient cho từng cột
                        pt.Color = columnColors[index % columnColors.Length];
                        pt.BackSecondaryColor = Color.FromArgb(
                            Math.Max(0, pt.Color.R - 30),
                            Math.Max(0, pt.Color.G - 30),
                            Math.Max(0, pt.Color.B - 20));
                        pt.BackGradientStyle = GradientStyle.TopBottom;

                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ColumnChart] Exception: {ex.Message}\n{ex.StackTrace}", "Lỗi Exception");
            }
        }
    }
}
