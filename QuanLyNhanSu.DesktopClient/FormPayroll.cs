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
    public class FormPayroll : Form
    {
        private string _userToken;
        private DataGridView dgvPayslips;
        private Button btnGenerate;
        private Button btnConfig;
        private ComboBox cbMonth;
        private ComboBox cbYear;
        private Button btnComplaint;
        private Button btnManageComplaints;
        private string _role;

        public FormPayroll(string token, string role = "admin")
        {
            _userToken = token;
            _role = role;
            InitializeUI();
            this.Load += FormPayroll_Load;
        }

        private void InitializeUI()
        {
            this.Text = "Tính Lương (Payroll)";
            this.Size = new Size(1150, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lblMonth = new Label { Text = "Tháng:", Location = new Point(20, 25), Width = 50 };
            cbMonth = new ComboBox { Location = new Point(70, 20), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            for (int i = 1; i <= 12; i++) cbMonth.Items.Add(i);
            cbMonth.SelectedItem = DateTime.Now.Month;

            Label lblYear = new Label { Text = "Năm:", Location = new Point(170, 25), Width = 40 };
            cbYear = new ComboBox { Location = new Point(210, 20), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            for (int i = 2020; i <= 2030; i++) cbYear.Items.Add(i);
            cbYear.SelectedItem = DateTime.Now.Year;

            btnGenerate = new Button { Text = "Chốt Lương Tháng", Location = new Point(310, 18), Width = 150, Height = 35, BackColor = Color.DarkBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnGenerate.Click += BtnGenerate_Click;

            btnConfig = new Button { Text = "⚙️ Cấu Hình Lương", Location = new Point(470, 18), Width = 180, Height = 35, BackColor = Color.DarkOrange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnConfig.Click += BtnConfig_Click;

            btnComplaint = new Button { Text = "⚠️ Gửi Khiếu Nại", Location = new Point(660, 18), Width = 150, Height = 35, BackColor = Color.OrangeRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnComplaint.Click += BtnComplaint_Click;

            btnManageComplaints = new Button { Text = "📋 Quản Lý Khiếu Nại", Location = new Point(820, 18), Width = 180, Height = 35, BackColor = Color.Teal, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnManageComplaints.Click += BtnManageComplaints_Click;

            if (_role != "admin")
            {
                btnGenerate.Visible = false;
                btnConfig.Visible = false;
                btnManageComplaints.Visible = false;
                this.Text = "Phiếu Lương Của Tôi";
            }
            else
            {
                btnComplaint.Visible = false; // Admin không tự gửi khiếu nại ở đây
            }

            dgvPayslips = new DataGridView
            {
                Location = new Point(20, 70),
                Size = new Size(940, 470),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            dgvPayslips.Columns.Add("id", "ID");
            dgvPayslips.Columns["id"].Visible = false; // Ẩn cột ID đi

            dgvPayslips.Columns.Add("userName", "Nhân viên");
            if (_role != "admin") dgvPayslips.Columns["userName"].Visible = false; // Ẩn tên nếu là user

            dgvPayslips.Columns.Add("standardWorkDays", "Ngày chuẩn");
            dgvPayslips.Columns.Add("actualWorkDays", "Ngày làm");
            dgvPayslips.Columns.Add("approvedLeaveDays", "Ngày phép");
            dgvPayslips.Columns.Add("overtimeDays", "Tăng ca (ngày)");
            dgvPayslips.Columns.Add("overtimePay", "Tiền tăng ca");
            dgvPayslips.Columns.Add("totalPenalty", "Phạt (VNĐ)");
            dgvPayslips.Columns.Add("grossSalary", "Gross (VNĐ)");
            dgvPayslips.Columns.Add("netSalary", "Net (VNĐ)");

            // Format tiền tệ cho các cột tiền
            dgvPayslips.Columns["overtimePay"].DefaultCellStyle.Format = "N0";
            dgvPayslips.Columns["totalPenalty"].DefaultCellStyle.Format = "N0";
            dgvPayslips.Columns["grossSalary"].DefaultCellStyle.Format = "N0";
            dgvPayslips.Columns["netSalary"].DefaultCellStyle.Format = "N0";

            this.Controls.Add(lblMonth);
            this.Controls.Add(cbMonth);
            this.Controls.Add(lblYear);
            this.Controls.Add(cbYear);
            this.Controls.Add(btnGenerate);
            this.Controls.Add(btnConfig);
            this.Controls.Add(btnComplaint);
            this.Controls.Add(btnManageComplaints);
            this.Controls.Add(dgvPayslips);

            cbMonth.SelectedIndexChanged += async (s, e) => await LoadDataAsync();
            cbYear.SelectedIndexChanged += async (s, e) => await LoadDataAsync();
        }

        private async void FormPayroll_Load(object? sender, EventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            int month = (int)cbMonth.SelectedItem;
            int year = (int)cbYear.SelectedItem;

            try
            {
                var response = await ApiClient.GetAsync($"api/app/payslip?month={month}&year={year}", _userToken);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        var root = doc.RootElement;
                        
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            dgvPayslips.Rows.Clear();
                            foreach (var item in root.EnumerateArray())
                            {
                                dgvPayslips.Rows.Add(
                                    item.GetProperty("id").GetString(),
                                    item.GetProperty("userName").GetString(),
                                    item.GetProperty("standardWorkDays").GetInt32(),
                                    item.GetProperty("actualWorkDays").GetInt32(),
                                    item.GetProperty("approvedLeaveDays").GetInt32(),
                                    item.GetProperty("overtimeDays").GetInt32(),
                                    item.GetProperty("overtimePay").GetDecimal(),
                                    item.GetProperty("totalPenalty").GetDecimal(),
                                    item.GetProperty("grossSalary").GetDecimal(),
                                    item.GetProperty("netSalary").GetDecimal()
                                );
                            }
                        }
                        else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out JsonElement items))
                        {
                            dgvPayslips.Rows.Clear();
                            foreach (var item in items.EnumerateArray())
                            {
                                dgvPayslips.Rows.Add(
                                    item.GetProperty("id").GetString(),
                                    item.GetProperty("userName").GetString(),
                                    item.GetProperty("standardWorkDays").GetInt32(),
                                    item.GetProperty("actualWorkDays").GetInt32(),
                                    item.GetProperty("approvedLeaveDays").GetInt32(),
                                    item.GetProperty("overtimeDays").GetInt32(),
                                    item.GetProperty("overtimePay").GetDecimal(),
                                    item.GetProperty("totalPenalty").GetDecimal(),
                                    item.GetProperty("grossSalary").GetDecimal(),
                                    item.GetProperty("netSalary").GetDecimal()
                                );
                            }
                        }
                    }
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show("Lỗi tải dữ liệu: " + msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống khi tải dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnGenerate_Click(object? sender, EventArgs e)
        {
            int month = (int)cbMonth.SelectedItem;
            int year = (int)cbYear.SelectedItem;

            btnGenerate.Enabled = false;
            btnGenerate.Text = "Đang xử lý...";

            try
            {
                var content = new StringContent("", Encoding.UTF8, "application/json");
                HttpResponseMessage response = await ApiClient.PostAsync($"api/app/payslip/generate-monthly-payroll?month={month}&year={year}", content, _userToken);
                
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Đã chốt lương tháng {month}/{year} thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            finally
            {
                btnGenerate.Enabled = true;
                btnGenerate.Text = "Chốt Lương Tháng";
            }
        }

        /// <summary>
        /// Mở form quản lý cấu hình lương nhân viên
        /// </summary>
        private void BtnConfig_Click(object? sender, EventArgs e)
        {
            FormSalaryConfig frm = new FormSalaryConfig(_userToken);
            frm.ShowDialog();
        }

        private void BtnComplaint_Click(object? sender, EventArgs e)
        {
            if (dgvPayslips.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn 1 phiếu lương để khiếu nại!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgvPayslips.SelectedRows[0];
            string payslipId = row.Cells["id"].Value?.ToString() ?? "";
            
            if (string.IsNullOrEmpty(payslipId)) return;

            int month = (int)cbMonth.SelectedItem;
            int year = (int)cbYear.SelectedItem;

            FormComplaintSubmit frm = new FormComplaintSubmit(_userToken, payslipId, month, year);
            frm.ShowDialog();
        }

        private void BtnManageComplaints_Click(object? sender, EventArgs e)
        {
            FormManageComplaints frm = new FormManageComplaints(_userToken);
            frm.ShowDialog();
        }
    }
}
