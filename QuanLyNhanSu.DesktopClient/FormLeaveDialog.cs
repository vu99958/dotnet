using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuanLyNhanSu.DesktopClient
{
    public class FormLeaveDialog : Form
    {
        private string _userToken;
        private DateTimePicker dtpStart;
        private DateTimePicker dtpEnd;
        private TextBox txtReason;
        private Button btnConfirm;

        private string? _editId = null;

        public FormLeaveDialog(string token, string? editId = null, DateTime? start = null, DateTime? end = null, string? reason = null)
        {
            _userToken = token;
            _editId = editId;
            InitializeUI();

            if (start.HasValue) dtpStart.Value = start.Value;
            if (end.HasValue) dtpEnd.Value = end.Value;
            if (!string.IsNullOrEmpty(reason)) txtReason.Text = reason;
        }

        private void InitializeUI()
        {
            this.Text = "Đơn Nghỉ Phép";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblStart = new Label { Text = "Ngày bắt đầu:", Location = new Point(20, 20), AutoSize = true };
            dtpStart = new DateTimePicker { Location = new Point(20, 45), Width = 340, Format = DateTimePickerFormat.Short };

            Label lblEnd = new Label { Text = "Ngày kết thúc:", Location = new Point(20, 80), AutoSize = true };
            dtpEnd = new DateTimePicker { Location = new Point(20, 105), Width = 340, Format = DateTimePickerFormat.Short };

            Label lblReason = new Label { Text = "Lý do:", Location = new Point(20, 140), AutoSize = true };
            txtReason = new TextBox { Location = new Point(20, 165), Width = 340, Height = 80, Multiline = true };

            btnConfirm = new Button { Text = "Xác nhận", Location = new Point(140, 260), Width = 100, Height = 35, BackColor = Color.LightSeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnConfirm.Click += BtnConfirm_Click;

            this.Controls.Add(lblStart);
            this.Controls.Add(dtpStart);
            this.Controls.Add(lblEnd);
            this.Controls.Add(dtpEnd);
            this.Controls.Add(lblReason);
            this.Controls.Add(txtReason);
            this.Controls.Add(btnConfirm);
        }

        private async void BtnConfirm_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                MessageBox.Show("Vui lòng nhập lý do!");
                return;
            }

            if (dtpEnd.Value.Date < dtpStart.Value.Date)
            {
                MessageBox.Show("Ngày kết thúc không được nhỏ hơn ngày bắt đầu!");
                return;
            }

            btnConfirm.Enabled = false;

            try
            {
                // Gọi API Create
                var payload = new
                {
                    UserId = Guid.Empty, // Phía server nên lấy từ CurrentUser, ở đây mock tạm Guid rỗng hoặc lấy từ auth
                    StartDate = dtpStart.Value.Date,
                    EndDate = dtpEnd.Value.Date,
                    Reason = txtReason.Text,
                    Status = "Pending"
                };

                string jsonString = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _userToken);
                    HttpResponseMessage response;
                    
                    if (_editId != null)
                    {
                        // Sửa đơn: Dùng PUT
                        response = await client.PutAsync($"https://localhost:44387/api/app/leave-request/{_editId}", content);
                    }
                    else
                    {
                        // Tạo đơn: Dùng POST
                        response = await client.PostAsync("https://localhost:44387/api/app/leave-request", content);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show(_editId != null ? "Cập nhật đơn nghỉ phép thành công!" : "Tạo đơn nghỉ phép thành công!");
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Lỗi máy chủ: " + await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
            finally
            {
                btnConfirm.Enabled = true;
            }
        }
    }
}
