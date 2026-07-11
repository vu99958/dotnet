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
    public class FormLeaveRequest : Form
    {
        private string _userToken;
        private bool _isAdmin = false;
        private DataGridView dgvLeaveRequests;
        private Button btnAdd, btnEdit, btnDelete, btnApprove, btnReject;

        public FormLeaveRequest(string token)
        {
            _userToken = token;
            InitializeUI();
            this.Load += FormLeaveRequest_Load;
        }

        private void InitializeUI()
        {
            this.Text = "Quản Lý Nghỉ Phép";
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            btnAdd = new Button { Text = "Thêm đơn", Location = new Point(20, 20), Width = 100, Height = 35, BackColor = Color.Green, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnEdit = new Button { Text = "Sửa", Location = new Point(130, 20), Width = 100, Height = 35, BackColor = Color.Orange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDelete = new Button { Text = "Xóa", Location = new Point(240, 20), Width = 100, Height = 35, BackColor = Color.Red, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnApprove = new Button { Text = "Duyệt đơn", Location = new Point(350, 20), Width = 100, Height = 35, BackColor = Color.Blue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnReject = new Button { Text = "Từ chối", Location = new Point(460, 20), Width = 100, Height = 35, BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            // Đăng ký sự kiện Click cho các nút
            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnApprove.Click += BtnApprove_Click;
            btnReject.Click += BtnReject_Click;

            dgvLeaveRequests = new DataGridView
            {
                Location = new Point(20, 70),
                Size = new Size(740, 370),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            dgvLeaveRequests.Columns.Add("id", "ID");
            dgvLeaveRequests.Columns["id"].Visible = false;
            dgvLeaveRequests.Columns.Add("userName", "Người xin nghỉ"); // Thêm cột UserName theo quy tắc 4
            dgvLeaveRequests.Columns.Add("startDate", "Từ ngày");
            dgvLeaveRequests.Columns.Add("endDate", "Đến ngày");
            dgvLeaveRequests.Columns.Add("reason", "Lý do");
            dgvLeaveRequests.Columns.Add("status", "Trạng thái");
            
            // Xếp cột Người xin nghỉ lên đầu tiên
            dgvLeaveRequests.Columns["userName"].DisplayIndex = 0;

            // Đăng ký sự kiện thay đổi dòng đang chọn
            dgvLeaveRequests.SelectionChanged += DgvLeaveRequests_SelectionChanged;

            // Xử lý hiển thị trạng thái bằng tiếng Việt thay vì Pending/Approved/Rejected
            dgvLeaveRequests.CellFormatting += (s, e) => {
                if (dgvLeaveRequests.Columns[e.ColumnIndex].Name == "status" && e.Value != null)
                {
                    string val = e.Value.ToString() ?? "";
                    if (val == "Pending") e.Value = "Đang chờ";
                    else if (val == "Approved") e.Value = "Đã phê duyệt";
                    else if (val == "Rejected") e.Value = "Không phê duyệt";
                    e.FormattingApplied = true;
                }
            };

            this.Controls.Add(btnAdd);
            this.Controls.Add(btnEdit);
            this.Controls.Add(btnDelete);
            this.Controls.Add(btnApprove);
            this.Controls.Add(btnReject);
            this.Controls.Add(dgvLeaveRequests);
        }

        private async void FormLeaveRequest_Load(object? sender, EventArgs e)
        {
            // Phân quyền giao diện: Ẩn nút Duyệt đơn nếu không phải Admin
            try
            {
                HttpResponseMessage response = await ApiClient.GetAsync("api/app/my-profile/my-profile", _userToken);
                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                    string myRole = data.GetProperty("roles").GetString()?.ToLower() ?? "user";
                    if (myRole == "admin" || myRole == "superadmin")
                    {
                        _isAdmin = true;
                    }
                    else
                    {
                        _isAdmin = false;
                        btnApprove.Visible = false;
                        btnReject.Visible = false;
                    }
                }
            }
            catch { }

            await LoadDataAsync();
        }

        private void DgvLeaveRequests_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvLeaveRequests.SelectedRows.Count > 0)
            {
                var row = dgvLeaveRequests.SelectedRows[0];
                string status = row.Cells["status"].Value?.ToString() ?? "";

                if (_isAdmin)
                {
                    // Admin không thể sửa đơn của người khác (ẩn luôn hoặc vô hiệu hóa)
                    btnEdit.Enabled = false;
                    
                    // Admin CHỈ ĐƯỢC XÓA khi đơn ĐÃ XỬ LÝ (khác Pending)
                    btnDelete.Enabled = (status != "Pending");
                }
                else
                {
                    // User thường: CHỈ ĐƯỢC SỬA/XÓA khi đang Pending
                    if (status != "Pending")
                    {
                        btnEdit.Enabled = false;
                        btnDelete.Enabled = false;
                    }
                    else
                    {
                        btnEdit.Enabled = true;
                        btnDelete.Enabled = true;
                    }
                }
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var response = await ApiClient.GetAsync("api/app/leave-request?maxResultCount=1000", _userToken);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("items", out JsonElement items))
                        {
                            dgvLeaveRequests.Rows.Clear();
                            foreach (var item in items.EnumerateArray())
                            {
                                dgvLeaveRequests.Rows.Add(
                                    item.GetProperty("id").GetString(),
                                    item.GetProperty("userName").GetString(),
                                    item.GetProperty("startDate").GetDateTime().ToString("dd/MM/yyyy"),
                                    item.GetProperty("endDate").GetDateTime().ToString("dd/MM/yyyy"),
                                    item.GetProperty("reason").GetString(),
                                    item.GetProperty("status").GetString()
                                );
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Lỗi tải dữ liệu: " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message);
            }
        }

        private async void BtnAdd_Click(object? sender, EventArgs e)
        {
            FormLeaveDialog dialog = new FormLeaveDialog(_userToken);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                await LoadDataAsync();
            }
        }

        private async void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (dgvLeaveRequests.SelectedRows.Count > 0)
            {
                var row = dgvLeaveRequests.SelectedRows[0];
                string id = row.Cells["id"].Value?.ToString() ?? "";
                string startStr = row.Cells["startDate"].Value?.ToString() ?? "";
                string endStr = row.Cells["endDate"].Value?.ToString() ?? "";
                string reason = row.Cells["reason"].Value?.ToString() ?? "";

                DateTime start = DateTime.ParseExact(startStr, "dd/MM/yyyy", null);
                DateTime end = DateTime.ParseExact(endStr, "dd/MM/yyyy", null);

                FormLeaveDialog dialog = new FormLeaveDialog(_userToken, id, start, end, reason);
                dialog.Text = "Sửa Đơn Nghỉ Phép";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    await LoadDataAsync();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một đơn để sửa!");
            }
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            // Kiểm tra lấy ID dòng đang chọn
            if (dgvLeaveRequests.SelectedRows.Count > 0)
            {
                string id = dgvLeaveRequests.SelectedRows[0].Cells["id"].Value?.ToString() ?? "";
                
                if (MessageBox.Show("Bạn có chắc chắn muốn xóa đơn này?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        HttpResponseMessage response = await ApiClient.DeleteAsync($"api/app/leave-request/{id}", _userToken);
                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Xóa thành công!");
                            await LoadDataAsync();
                        }
                        else
                        {
                            MessageBox.Show("Lỗi: " + await response.Content.ReadAsStringAsync());
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi: " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một đơn để xóa!");
            }
        }

        private async void BtnApprove_Click(object? sender, EventArgs e)
        {
            // Kiểm tra lấy ID dòng đang chọn
            if (dgvLeaveRequests.SelectedRows.Count > 0)
            {
                string id = dgvLeaveRequests.SelectedRows[0].Cells["id"].Value?.ToString() ?? "";
                
                if (MessageBox.Show("Xác nhận duyệt đơn này?", "Duyệt đơn", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        var content = new StringContent("", Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await ApiClient.PostAsync($"api/app/leave-request/{id}/change-status?newStatus=Approved", content, _userToken);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Đã duyệt đơn!");
                            await LoadDataAsync();
                        }
                        else
                        {
                            MessageBox.Show("Lỗi: " + await response.Content.ReadAsStringAsync());
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi: " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một đơn để duyệt!");
            }
        }

        private async void BtnReject_Click(object? sender, EventArgs e)
        {
            if (dgvLeaveRequests.SelectedRows.Count > 0)
            {
                string id = dgvLeaveRequests.SelectedRows[0].Cells["id"].Value?.ToString() ?? "";
                
                if (MessageBox.Show("Xác nhận KHÔNG PHÊ DUYỆT đơn này?", "Từ chối", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        var content = new StringContent("", Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await ApiClient.PostAsync($"api/app/leave-request/{id}/change-status?newStatus=Rejected", content, _userToken);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Đã từ chối đơn!");
                            await LoadDataAsync();
                        }
                        else
                        {
                            var errorString = await response.Content.ReadAsStringAsync();
                            MessageBox.Show("Lỗi: " + errorString);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi: " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một đơn để từ chối!");
            }
        }
    }
}
