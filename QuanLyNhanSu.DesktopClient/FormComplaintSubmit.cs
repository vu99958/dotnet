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
    public class FormComplaintSubmit : Form
    {
        private string _userToken;
        private string _payslipId;
        private int _month;
        private int _year;
        
        private TextBox txtReason;
        private Button btnSubmit;

        public FormComplaintSubmit(string token, string payslipId, int month, int year)
        {
            _userToken = token;
            _payslipId = payslipId;
            _month = month;
            _year = year;
            
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = $"Gửi Khiếu Nại Phiếu Lương (Tháng {_month}/{_year})";
            this.Size = new Size(500, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lbl = new Label { Text = "Vui lòng nhập lý do khiếu nại chi tiết:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            txtReason = new TextBox { Location = new Point(20, 50), Size = new Size(440, 120), Multiline = true, Font = new Font("Segoe UI", 11F) };

            btnSubmit = new Button { Text = "Gửi Khiếu Nại", Location = new Point(175, 190), Size = new Size(150, 40), BackColor = Color.Teal, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            btnSubmit.Click += BtnSubmit_Click;

            this.Controls.Add(lbl);
            this.Controls.Add(txtReason);
            this.Controls.Add(btnSubmit);
        }

        private async void BtnSubmit_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                MessageBox.Show("Vui lòng nhập lý do khiếu nại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnSubmit.Enabled = false;

            try
            {
                var payload = new
                {
                    payslipId = _payslipId,
                    month = _month,
                    year = _year,
                    reason = txtReason.Text
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await ApiClient.PostAsync("api/app/payslip-complaint", content, _userToken);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Gửi khiếu nại thành công! Admin sẽ xem xét và phản hồi.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Lỗi khi gửi khiếu nại: " + response.StatusCode, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSubmit.Enabled = true;
            }
        }
    }
}
