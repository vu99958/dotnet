using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using QuanLyNhanSu.DesktopClient.Services;

namespace QuanLyNhanSu.DesktopClient
{
    public class FormManageComplaints : Form
    {
        private string _userToken;
        private DataGridView dgvComplaints;
        private Button btnResolve;
        private Button btnReject;
        private TextBox txtReply;

        public FormManageComplaints(string token)
        {
            _userToken = token;
            InitializeUI();
            this.Load += FormManageComplaints_Load;
        }

        private void InitializeUI()
        {
            this.Text = "Quản Lý Khiếu Nại (Admin)";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            dgvComplaints = new DataGridView
            {
                Location = new Point(20, 20),
                Size = new Size(840, 350),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            dgvComplaints.Columns.Add("id", "ID");
            dgvComplaints.Columns["id"].Visible = false;
            dgvComplaints.Columns.Add("userName", "Nhân viên");
            dgvComplaints.Columns.Add("month", "Tháng");
            dgvComplaints.Columns.Add("year", "Năm");
            dgvComplaints.Columns.Add("reason", "Lý do khiếu nại");
            dgvComplaints.Columns.Add("status", "Trạng thái");
            dgvComplaints.Columns.Add("creationTime", "Ngày gửi");

            Label lblReply = new Label { Text = "Phản hồi của Admin:", Location = new Point(20, 390), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            txtReply = new TextBox { Location = new Point(20, 420), Size = new Size(600, 100), Multiline = true, Font = new Font("Segoe UI", 11F) };

            btnResolve = new Button { Text = "✔️ Đã Giải Quyết", Location = new Point(650, 420), Size = new Size(210, 45), BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            btnResolve.Click += async (s, e) => await ProcessComplaint("Resolved");

            btnReject = new Button { Text = "❌ Từ Chối", Location = new Point(650, 475), Size = new Size(210, 45), BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            btnReject.Click += async (s, e) => await ProcessComplaint("Rejected");

            this.Controls.Add(dgvComplaints);
            this.Controls.Add(lblReply);
            this.Controls.Add(txtReply);
            this.Controls.Add(btnResolve);
            this.Controls.Add(btnReject);
        }

        private async void FormManageComplaints_Load(object? sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                HttpResponseMessage response = await ApiClient.GetAsync("api/app/payslip-complaint/pending-list", _userToken);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        var root = doc.RootElement;
                        dgvComplaints.Rows.Clear();
                        
                        JsonElement items;
                        if (root.ValueKind == JsonValueKind.Array) items = root;
                        else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var i)) items = i;
                        else return;

                        foreach (var item in items.EnumerateArray())
                        {
                            dgvComplaints.Rows.Add(
                                item.GetProperty("id").GetString(),
                                item.GetProperty("userName").GetString(),
                                item.GetProperty("month").GetInt32(),
                                item.GetProperty("year").GetInt32(),
                                item.GetProperty("reason").GetString(),
                                item.GetProperty("status").GetString(),
                                item.GetProperty("creationTime").GetDateTime().ToString("dd/MM/yyyy HH:mm")
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        private async Task ProcessComplaint(string newStatus)
        {
            if (dgvComplaints.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn 1 khiếu nại!");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtReply.Text))
            {
                MessageBox.Show("Vui lòng nhập phản hồi!");
                return;
            }

            var row = dgvComplaints.SelectedRows[0];
            string id = row.Cells["id"].Value.ToString();

            try
            {
                var payload = new
                {
                    status = newStatus,
                    adminReply = txtReply.Text
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Chú ý: url put có dạng /api/app/payslip-complaint/{id}/resolve
                HttpResponseMessage response = await ApiClient.PutAsync($"api/app/payslip-complaint/{id}/resolve", content, _userToken);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xử lý thành công!");
                    txtReply.Text = "";
                    await LoadDataAsync(); // Tải lại danh sách
                }
                else
                {
                    MessageBox.Show("Lỗi xử lý: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }
    }
}
