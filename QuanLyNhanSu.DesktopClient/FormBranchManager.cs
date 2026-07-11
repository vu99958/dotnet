using System;
using System.Drawing;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuanLyNhanSu.DesktopClient
{
    /// <summary>
    /// Form quản lý chi nhánh (CRUD) — Admin dùng để cấu hình tọa độ Geofencing.
    /// Thiết kế phẳng chuẩn theo FormSalaryConfig.
    /// </summary>
    public class FormBranchManager : Form
    {
        private string _userToken;
        private DataGridView dgvBranches;

        // Các ô nhập liệu
        private TextBox txtName, txtLatitude, txtLongitude, txtRadius, txtAddress;
        private Button btnAdd, btnUpdate, btnDelete;
        private Label lblStatus;

        // Lưu ID chi nhánh đang chọn để Sửa/Xóa
        private string? _selectedBranchId = null;

        // Màu sắc chuẩn hệ thống
        private static readonly Color PrimaryBlue = Color.FromArgb(0, 102, 204);
        private static readonly Color PrimaryGreen = Color.FromArgb(32, 161, 68);
        private static readonly Color DangerRed = Color.FromArgb(220, 53, 69);
        private static readonly Color WarningOrange = Color.FromArgb(255, 140, 0);
        private static readonly Color LightGray = Color.FromArgb(245, 247, 250);
        private static readonly Color DarkGray = Color.FromArgb(80, 80, 80);

        public FormBranchManager(string token)
        {
            _userToken = token;
            InitializeUI();
            this.Load += async (s, e) => await LoadBranchListAsync();
        }

        private void InitializeUI()
        {
            // ========== CẤU HÌNH FORM ==========
            this.Text = "🏢 Quản Lý Chi Nhánh — Geofencing";
            this.Size = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = LightGray;
            this.Font = new Font("Segoe UI", 10F);
            this.MaximizeBox = false;

            // ========== TIÊU ĐỀ ==========
            Label lblTitle = new Label
            {
                Text = "🏢 QUẢN LÝ CHI NHÁNH — CẤU HÌNH GEOFENCING",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = PrimaryBlue,
                Location = new Point(20, 15),
                AutoSize = true
            };

            Label lblHint = new Label
            {
                Text = "💡 Click vào 1 chi nhánh trong bảng để chỉnh sửa. Điền thông tin bên dưới rồi bấm Thêm/Cập nhật.",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(20, 45),
                Width = 850,
                Height = 20
            };

            // ========== BẢNG DỮ LIỆU ==========
            dgvBranches = new DataGridView
            {
                Location = new Point(20, 75),
                Size = new Size(845, 250),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowTemplate = { Height = 35 },
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10F)
            };

            // Cột ẩn chứa ID
            dgvBranches.Columns.Add("Id", "ID");
            dgvBranches.Columns["Id"].Visible = false;

            dgvBranches.Columns.Add("Name", "TÊN CHI NHÁNH");
            dgvBranches.Columns.Add("Latitude", "VĨ ĐỘ (Lat)");
            dgvBranches.Columns.Add("Longitude", "KINH ĐỘ (Lng)");
            dgvBranches.Columns.Add("Radius", "BÁN KÍNH (m)");

            // Khi click vào dòng → đổ dữ liệu vào form
            dgvBranches.CellClick += DgvBranches_CellClick;

            // ========== FORM NHẬP LIỆU ==========
            Panel pnlForm = new Panel
            {
                Location = new Point(20, 340),
                Size = new Size(845, 175),
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            int col1 = 15, col2 = 200, col3 = 420, col4 = 640;
            int row1 = 15, row2 = 55;

            // Hàng 1: Tên + Vĩ độ
            pnlForm.Controls.Add(new Label { Text = "Tên chi nhánh:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = DarkGray, Location = new Point(col1, row1), AutoSize = true });
            txtName = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(col1, row2), Width = 180, BorderStyle = BorderStyle.FixedSingle };
            pnlForm.Controls.Add(txtName);

            pnlForm.Controls.Add(new Label { Text = "Vĩ độ (Lat):", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = DarkGray, Location = new Point(col2, row1), AutoSize = true });
            txtLatitude = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(col2, row2), Width = 140, BorderStyle = BorderStyle.FixedSingle };
            pnlForm.Controls.Add(txtLatitude);

            pnlForm.Controls.Add(new Label { Text = "Kinh độ (Lng):", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = DarkGray, Location = new Point(col3 + 10, row1), AutoSize = true });
            txtLongitude = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(col3 + 10, row2), Width = 140, BorderStyle = BorderStyle.FixedSingle };
            pnlForm.Controls.Add(txtLongitude);

            pnlForm.Controls.Add(new Label { Text = "Bán kính (m):", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = DarkGray, Location = new Point(col4 + 30, row1), AutoSize = true });
            txtRadius = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(col4 + 30, row2), Width = 120, BorderStyle = BorderStyle.FixedSingle };
            pnlForm.Controls.Add(txtRadius);

            Button btnAutoLocation = new Button
            {
                Text = "📍 Lấy vị trí hiện tại",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = PrimaryBlue,
                Location = new Point(col2, row2 + 35),
                Size = new Size(180, 30),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAutoLocation.FlatAppearance.BorderSize = 0;
            btnAutoLocation.Click += async (s, e) => {
                await AutoGetLocationAsync();
                await ReverseGeocodeAsync();
            };
            pnlForm.Controls.Add(btnAutoLocation);

            // Hàng 2: Địa chỉ
            int row3 = 95;
            pnlForm.Controls.Add(new Label { Text = "Địa chỉ thực tế:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = DarkGray, Location = new Point(col1, row3), AutoSize = true });
            
            txtAddress = new TextBox { Font = new Font("Segoe UI", 10.5F), Location = new Point(col1, row3 + 25), Width = 610, BorderStyle = BorderStyle.FixedSingle, ReadOnly = true, BackColor = Color.WhiteSmoke, Text = "Chưa có dữ liệu địa chỉ..." };
            pnlForm.Controls.Add(txtAddress);

            Button btnGeocode = new Button
            {
                Text = "🌍 Dịch Tọa Độ",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = PrimaryBlue,
                Location = new Point(col1 + 625, row3 + 25),
                Size = new Size(165, 28),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnGeocode.FlatAppearance.BorderSize = 0;
            btnGeocode.Click += async (s, e) => await ReverseGeocodeAsync();
            pnlForm.Controls.Add(btnGeocode);

            // ========== NÚT HÀNH ĐỘNG ==========
            btnAdd = new Button
            {
                Text = "➕ THÊM MỚI",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = PrimaryGreen,
                Location = new Point(20, 530),
                Size = new Size(200, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += async (s, e) => await CreateBranchAsync();

            btnUpdate = new Button
            {
                Text = "✏️ CẬP NHẬT",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = WarningOrange,
                Location = new Point(240, 530),
                Size = new Size(200, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += async (s, e) => await UpdateBranchAsync();

            btnDelete = new Button
            {
                Text = "🗑️ XÓA",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = DangerRed,
                Location = new Point(460, 530),
                Size = new Size(200, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += async (s, e) => await DeleteBranchAsync();

            Button btnClear = new Button
            {
                Text = "🔄 LÀM MỚI",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = DarkGray,
                BackColor = Color.LightGray,
                Location = new Point(680, 530),
                Size = new Size(185, 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => ClearForm();

            // Status bar
            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = PrimaryGreen,
                Location = new Point(20, 580),
                Width = 845,
                Height = 20
            };

            // ========== GẮN CONTROLS ==========
            this.Controls.AddRange(new Control[]
            {
                lblTitle, lblHint, dgvBranches, pnlForm,
                btnAdd, btnUpdate, btnDelete, btnClear, lblStatus
            });
        }

        // ==========================================
        // SỰ KIỆN: CLICK VÀO DÒNG TRONG BẢNG
        // ==========================================
        private void DgvBranches_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvBranches.Rows.Count) return;

            var row = dgvBranches.Rows[e.RowIndex];
            _selectedBranchId = row.Cells["Id"].Value?.ToString();
            txtName.Text = row.Cells["Name"].Value?.ToString() ?? "";
            txtLatitude.Text = row.Cells["Latitude"].Value?.ToString() ?? "";
            txtLongitude.Text = row.Cells["Longitude"].Value?.ToString() ?? "";
            txtRadius.Text = row.Cells["Radius"].Value?.ToString() ?? "";
            txtAddress.Text = "Nhấn [🌍 Dịch Tọa Độ] để xem địa chỉ chi tiết.";

            // Bật nút Cập nhật + Xóa khi đã chọn dòng
            btnUpdate.Enabled = true;
            btnDelete.Enabled = true;
            lblStatus.Text = $"📌 Đã chọn: {txtName.Text}";
            lblStatus.ForeColor = PrimaryBlue;
        }

        // ==========================================
        // LÀM MỚI FORM
        // ==========================================
        private void ClearForm()
        {
            txtRadius.Clear();
            txtAddress.Text = "Chưa có dữ liệu địa chỉ...";
            _selectedBranchId = null;
            txtName.Text = "";
            txtLatitude.Text = "";
            txtLongitude.Text = "";
            txtRadius.Text = "";
            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;
            lblStatus.Text = "Đã làm mới form.";
            lblStatus.ForeColor = Color.Gray;
        }

        // ==========================================
        // GỌI API: TỰ ĐỘNG LẤY TỌA ĐỘ
        // ==========================================
        private async Task AutoGetLocationAsync()
        {
            try
            {
                lblStatus.Text = "⏳ Đang kết nối vệ tinh GPS...";
                lblStatus.ForeColor = WarningOrange;

                bool hasRealGps = false;
                try
                {
                    // 1. Thử lấy Tọa độ GPS thời gian thực từ phần cứng Windows (Độ chuẩn xác rất cao)
                    var accessStatus = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();
                    if (accessStatus == Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed)
                    {
                        var geolocator = new Windows.Devices.Geolocation.Geolocator { DesiredAccuracy = Windows.Devices.Geolocation.PositionAccuracy.High };
                        var pos = await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(10));
                        txtLatitude.Text = pos.Coordinate.Point.Position.Latitude.ToString(CultureInfo.InvariantCulture);
                        txtLongitude.Text = pos.Coordinate.Point.Position.Longitude.ToString(CultureInfo.InvariantCulture);
                        lblStatus.Text = "✅ Định vị vệ tinh (GPS) thành công!";
                        lblStatus.ForeColor = PrimaryGreen;
                        hasRealGps = true;
                    }
                }
                catch
                {
                    // Bỏ qua lỗi nếu tính năng định vị trên Windows bị tắt hoặc không cấp quyền
                }

                if (hasRealGps) return;

                // 2. Dự phòng: Nếu thiết bị không có GPS hoặc bị tắt, chuyển sang quét bằng Mạng (IP)
                lblStatus.Text = "⚠️ GPS bị tắt. Đang quét vị trí qua Mạng (IP)...";
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("http://ip-api.com/json/");
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(jsonString))
                        {
                            var root = doc.RootElement;
                            if (root.TryGetProperty("lat", out var latProp) && root.TryGetProperty("lon", out var lonProp))
                            {
                                txtLatitude.Text = latProp.GetDouble().ToString(CultureInfo.InvariantCulture);
                                txtLongitude.Text = lonProp.GetDouble().ToString(CultureInfo.InvariantCulture);
                                lblStatus.Text = "✅ Đã lấy vị trí qua Mạng (Gần đúng).";
                                lblStatus.ForeColor = PrimaryGreen;
                            }
                            else
                            {
                                MessageBox.Show("Không thể trích xuất tọa độ từ dữ liệu trả về.", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Không thể kết nối đến dịch vụ định vị.", "Lỗi Mạng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lấy vị trí: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "❌ Lỗi định vị.";
                lblStatus.ForeColor = DangerRed;
            }
        }

        // ==========================================
        // GỌI API: LẤY DANH SÁCH CHI NHÁNH
        // ==========================================
        private async Task LoadBranchListAsync()
        {
            try
            {
                dgvBranches.Rows.Clear();

                var response = await Services.ApiClient.GetAsync("api/app/branch", _userToken);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        foreach (JsonElement item in doc.RootElement.EnumerateArray())
                        {
                            dgvBranches.Rows.Add(
                                item.GetProperty("id").GetString() ?? "",
                                item.GetProperty("name").GetString() ?? "",
                                item.GetProperty("latitude").GetDouble().ToString("F4"),
                                item.GetProperty("longitude").GetDouble().ToString("F4"),
                                item.GetProperty("radiusInMeters").GetInt32().ToString()
                            );
                        }
                    }
                    lblStatus.Text = $"✅ Đã tải {dgvBranches.Rows.Count} chi nhánh.";
                    lblStatus.ForeColor = PrimaryGreen;
                }
                else
                {
                    lblStatus.Text = "❌ Lỗi tải danh sách: " + response.StatusCode;
                    lblStatus.ForeColor = DangerRed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message, "Lỗi Mạng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // GỌI API: THÊM CHI NHÁNH MỚI
        // ==========================================
        private async Task CreateBranchAsync()
        {
            if (!ValidateInput()) return;

            try
            {
                var payload = BuildPayload();
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                
                var response = await Services.ApiClient.PostAsync("api/app/branch", content, _userToken);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Thêm chi nhánh thành công!", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearForm();
                    await LoadBranchListAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show("Lỗi: " + error, "Thất Bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message, "Lỗi Mạng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // GỌI API: CẬP NHẬT CHI NHÁNH
        // ==========================================
        private async Task UpdateBranchAsync()
        {
            if (string.IsNullOrEmpty(_selectedBranchId))
            {
                MessageBox.Show("Vui lòng chọn chi nhánh cần cập nhật!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateInput()) return;

            try
            {
                var payload = BuildPayload();
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                
                var response = await Services.ApiClient.PutAsync($"api/app/branch/{_selectedBranchId}", content, _userToken);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Cập nhật thành công!", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearForm();
                    await LoadBranchListAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show("Lỗi: " + error, "Thất Bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message, "Lỗi Mạng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // GỌI API: XÓA CHI NHÁNH
        // ==========================================
        private async Task DeleteBranchAsync()
        {
            if (string.IsNullOrEmpty(_selectedBranchId))
            {
                MessageBox.Show("Vui lòng chọn chi nhánh cần xóa!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn xóa chi nhánh '{txtName.Text}'?\nHành động này không thể hoàn tác!",
                "Xác Nhận Xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm != DialogResult.Yes) return;

            try
            {
                var response = await Services.ApiClient.DeleteAsync($"api/app/branch/{_selectedBranchId}", _userToken);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Đã xóa thành công!", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearForm();
                    await LoadBranchListAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show("Lỗi: " + error, "Thất Bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message, "Lỗi Mạng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // HÀM TIỆN ÍCH
        // ==========================================

        /// <summary>
        /// Kiểm tra dữ liệu nhập hợp lệ
        /// </summary>
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên chi nhánh!", "Thiếu Thông Tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }

            if (!double.TryParse(txtLatitude.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Vĩ độ (Latitude) phải là số thực!\nVí dụ: 10.2541", "Dữ Liệu Sai", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLatitude.Focus();
                return false;
            }

            if (!double.TryParse(txtLongitude.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Kinh độ (Longitude) phải là số thực!\nVí dụ: 105.9723", "Dữ Liệu Sai", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLongitude.Focus();
                return false;
            }

            if (!int.TryParse(txtRadius.Text, out int radius) || radius <= 0)
            {
                MessageBox.Show("Bán kính phải là số nguyên dương!\nVí dụ: 500", "Dữ Liệu Sai", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRadius.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tạo JSON payload từ các ô nhập liệu
        /// </summary>
        private string BuildPayload()
        {
            // Dùng InvariantCulture và Replace để đảm bảo dấu chấm thập phân (10.2541 thay vì 10,2541)
            double lat = double.Parse(txtLatitude.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            double lng = double.Parse(txtLongitude.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            int radius = int.Parse(txtRadius.Text);

            return JsonSerializer.Serialize(new
            {
                name = txtName.Text.Trim(),
                latitude = lat,
                longitude = lng,
                radiusInMeters = radius
            });
        }

        // ==========================================
        // GỌI API: DỊCH TỌA ĐỘ SANG ĐỊA CHỈ (REVERSE GEOCODING)
        // ==========================================
        private async Task ReverseGeocodeAsync()
        {
            if (!double.TryParse(txtLatitude.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) ||
                !double.TryParse(txtLongitude.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double lng))
            {
                return; // Không làm gì nếu chưa có tọa độ hợp lệ
            }

            try
            {
                txtAddress.Text = "🌍 Đang phân tích vị trí trên bản đồ...";
                
                using (var client = new HttpClient())
                {
                    // Nominatim yêu cầu phải có User-Agent
                    client.DefaultRequestHeaders.Add("User-Agent", "QuanLyNhanSuApp/1.0");
                    string url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat.ToString(CultureInfo.InvariantCulture)}&lon={lng.ToString(CultureInfo.InvariantCulture)}&zoom=18&addressdetails=1";
                    
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(jsonString))
                        {
                            var root = doc.RootElement;
                            if (root.TryGetProperty("display_name", out var addressProp))
                            {
                                txtAddress.Text = addressProp.GetString();
                            }
                            else
                            {
                                txtAddress.Text = "Không tìm thấy địa chỉ cho tọa độ này.";
                            }
                        }
                    }
                    else
                    {
                        txtAddress.Text = "Lỗi khi kết nối dịch vụ Bản đồ: " + response.StatusCode;
                    }
                }
            }
            catch (Exception ex)
            {
                txtAddress.Text = "Lỗi mạng khi dịch tọa độ: " + ex.Message;
            }
        }
    }
}
