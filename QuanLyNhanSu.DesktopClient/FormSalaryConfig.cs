using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuanLyNhanSu.DesktopClient
{
    /// <summary>
    /// Form quản lý cấu hình lương cho từng nhân viên.
    /// Admin có thể thiết lập Chức vụ, Lương cơ bản, Phụ cấp cho từng người.
    /// </summary>
    public class FormSalaryConfig : Form
    {
        private string _userToken;
        private DataGridView dgvProfiles;

        public FormSalaryConfig(string token)
        {
            _userToken = token;
            InitializeUI();
            this.Load += FormSalaryConfig_Load;
        }

        private void InitializeUI()
        {
            this.Text = "⚙️ Cấu Hình Lương Nhân Viên";
            this.Size = new Size(850, 550);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Hướng dẫn
            Label lblInfo = new Label
            {
                Text = "💡 Double-click vào 1 nhân viên để thiết lập Chức vụ & Mức lương riêng.",
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(20, 15),
                Width = 800,
                Height = 25
            };

            dgvProfiles = new DataGridView
            {
                Location = new Point(20, 50),
                Size = new Size(790, 440),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowTemplate = { Height = 35 }
            };

            // Cột ẩn chứa UserId
            dgvProfiles.Columns.Add("userId", "UserId");
            dgvProfiles.Columns["userId"].Visible = false;

            dgvProfiles.Columns.Add("userName", "Nhân viên");
            dgvProfiles.Columns.Add("position", "Chức vụ");
            dgvProfiles.Columns.Add("baseSalary", "Lương cơ bản (VNĐ)");
            dgvProfiles.Columns.Add("allowance", "Phụ cấp (VNĐ)");

            // Format tiền tệ
            dgvProfiles.Columns["baseSalary"].DefaultCellStyle.Format = "N0";
            dgvProfiles.Columns["allowance"].DefaultCellStyle.Format = "N0";

            // Sự kiện double-click mở dialog chỉnh sửa
            dgvProfiles.CellDoubleClick += DgvProfiles_CellDoubleClick;

            // Đánh màu cho hàng chưa thiết lập lương
            dgvProfiles.CellFormatting += (s, e) =>
            {
                if (dgvProfiles.Columns[e.ColumnIndex].Name == "position" && e.Value != null)
                {
                    if (e.Value.ToString() == "Chưa thiết lập")
                    {
                        e.CellStyle.ForeColor = Color.Red;
                        e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
                    }
                }
            };

            this.Controls.Add(lblInfo);
            this.Controls.Add(dgvProfiles);
        }

        private async void FormSalaryConfig_Load(object? sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// Tải danh sách tất cả nhân viên kèm cấu hình lương
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (s, cert, chain, ssl) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _userToken);
                    HttpResponseMessage response = await client.GetAsync("https://localhost:44387/api/app/salary-profile");

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(jsonString))
                        {
                            var root = doc.RootElement;
                            dgvProfiles.Rows.Clear();

                            // Xử lý cả 2 dạng trả về: mảng trực tiếp hoặc object có key "items"
                            JsonElement items = root;
                            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out JsonElement itemsProp))
                            {
                                items = itemsProp;
                            }

                            if (items.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in items.EnumerateArray())
                                {
                                    dgvProfiles.Rows.Add(
                                        item.GetProperty("userId").GetString(),
                                        item.GetProperty("userName").GetString(),
                                        item.GetProperty("position").GetString(),
                                        item.GetProperty("baseSalary").GetDecimal(),
                                        item.GetProperty("allowance").GetDecimal()
                                    );
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Lỗi tải dữ liệu: " + await response.Content.ReadAsStringAsync(), "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Khi Admin double-click vào 1 nhân viên → Mở dialog nhập Chức vụ, Lương, Phụ cấp
        /// </summary>
        private async void DgvProfiles_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvProfiles.Rows[e.RowIndex];
            string userId = row.Cells["userId"].Value?.ToString() ?? "";
            string userName = row.Cells["userName"].Value?.ToString() ?? "";
            string currentPosition = row.Cells["position"].Value?.ToString() ?? "";
            decimal currentBaseSalary = Convert.ToDecimal(row.Cells["baseSalary"].Value ?? 0);
            decimal currentAllowance = Convert.ToDecimal(row.Cells["allowance"].Value ?? 0);

            // Tạo dialog nhập liệu
            Form dialog = new Form
            {
                Text = $"Cấu hình lương: {userName}",
                Size = new Size(420, 320),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F)
            };

            Label lblName = new Label { Text = $"Nhân viên: {userName}", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.DarkBlue, Location = new Point(20, 15), Width = 360, Height = 30 };

            Label lblPos = new Label { Text = "Chức vụ:", Location = new Point(20, 60), Width = 100 };
            ComboBox cbPosition = new ComboBox
            {
                Location = new Point(130, 57), Width = 240,
                DropDownStyle = ComboBoxStyle.DropDown,
                Text = currentPosition == "Chưa thiết lập" ? "" : currentPosition
            };
            // Các chức vụ gợi ý
            cbPosition.Items.AddRange(new string[] { "Giám đốc", "Phó giám đốc", "Trưởng phòng", "Phó phòng", "Nhân viên", "Thực tập sinh" });

            Label lblSalary = new Label { Text = "Lương cơ bản:", Location = new Point(20, 105), Width = 100 };
            TextBox txtBaseSalary = new TextBox { Text = currentBaseSalary.ToString("N0"), Location = new Point(130, 102), Width = 240 };

            Label lblAllowance = new Label { Text = "Phụ cấp:", Location = new Point(20, 150), Width = 100 };
            TextBox txtAllowance = new TextBox { Text = currentAllowance.ToString("N0"), Location = new Point(130, 147), Width = 240 };

            Button btnSave = new Button { Text = "💾 LƯU", BackColor = Color.Green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(130, 210), Width = 120, Height = 40, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            Button btnCancel = new Button { Text = "❌ HỦY", BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(260, 210), Width = 110, Height = 40, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };

            btnCancel.Click += (s, ev) => dialog.Close();
            btnSave.Click += async (s, ev) =>
            {
                // Kiểm tra đầu vào
                if (string.IsNullOrWhiteSpace(cbPosition.Text))
                {
                    MessageBox.Show("Vui lòng chọn hoặc nhập chức vụ!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Loại bỏ dấu phẩy phân cách hàng nghìn trước khi parse
                string salaryText = txtBaseSalary.Text.Replace(",", "").Replace(".", "");
                string allowanceText = txtAllowance.Text.Replace(",", "").Replace(".", "");

                if (!decimal.TryParse(salaryText, out decimal baseSalary) || baseSalary < 0)
                {
                    MessageBox.Show("Lương cơ bản không hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!decimal.TryParse(allowanceText, out decimal allowance) || allowance < 0)
                {
                    MessageBox.Show("Phụ cấp không hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Gọi API lưu cấu hình
                try
                {
                    HttpClientHandler handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (ss, cert, chain, ssl) => true;
                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _userToken);

                        var payload = new
                        {
                            userId = userId,
                            position = cbPosition.Text,
                            baseSalary = baseSalary,
                            allowance = allowance
                        };
                        var json = JsonSerializer.Serialize(payload);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync("https://localhost:44387/api/app/salary-profile/or-update", content);

                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show($"Đã lưu cấu hình lương cho {userName}!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            dialog.Close();
                            await LoadDataAsync(); // Reload danh sách
                        }
                        else
                        {
                            MessageBox.Show("Lỗi: " + await response.Content.ReadAsStringAsync(), "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dialog.Controls.AddRange(new Control[] { lblName, lblPos, cbPosition, lblSalary, txtBaseSalary, lblAllowance, txtAllowance, btnSave, btnCancel });
            dialog.ShowDialog();
        }
    }
}
