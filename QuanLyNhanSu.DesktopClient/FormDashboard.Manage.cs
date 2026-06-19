using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class FormDashboard
    {
        // ==========================================
        // LOGIC QUẢN LÝ NHÂN SỰ
        // ==========================================
        private async Task LoadEmployeeListAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(userToken)) return;
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
                    HttpResponseMessage response = await client.GetAsync("https://localhost:44387/api/app/employee/employee");
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var dataList = JsonSerializer.Deserialize<JsonElement>(jsonString);
                        dgvEmployees.Rows.Clear();
                        foreach (var emp in dataList.EnumerateArray())
                        {
                            dgvEmployees.Rows.Add(
                                emp.GetProperty("id").GetString() ?? "",
                                emp.GetProperty("userName").GetString() ?? "",
                                emp.GetProperty("email").GetString() ?? "",
                                emp.GetProperty("phoneNumber").GetString() ?? ""
                            );
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi máy chủ: " + ex.Message); }
        }

        private void BtnAddEmp_Click(object? sender, EventArgs e)
        {
            currentEditUserId = null; 
            lblAddEditTitle.Text = "THÊM NHÂN VIÊN MỚI";
            txtEmpUserName.Text = ""; txtEmpEmail.Text = ""; txtEmpPhone.Text = ""; txtEmpPassword.Text = "";
            
            cbEmpRole.Items.Clear();
            if (myCurrentRole == "superadmin")
            {
                cbEmpRole.Items.AddRange(new string[] { "SuperAdmin", "Admin", "User" });
                cbEmpRole.SelectedIndex = 2; 
            }
            else
            {
                cbEmpRole.Items.Add("User");
                cbEmpRole.SelectedIndex = 0;
            }
            
            lblEmpPassword.Visible = true; txtEmpPassword.Visible = true;
            btnDeleteEmp.Visible = false; btnIssueKey.Visible = false;
            SwitchPanel(pnlAddEditEmployee);
        }

        private void DgvEmployees_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                currentEditUserId = dgvEmployees.Rows[e.RowIndex].Cells["Id"].Value?.ToString();
                lblAddEditTitle.Text = "CHI TIẾT NHÂN VIÊN";
                txtEmpUserName.Text = dgvEmployees.Rows[e.RowIndex].Cells["UserName"].Value?.ToString() ?? "";
                txtEmpEmail.Text = dgvEmployees.Rows[e.RowIndex].Cells["Email"].Value?.ToString() ?? "";
                txtEmpPhone.Text = dgvEmployees.Rows[e.RowIndex].Cells["PhoneNumber"].Value?.ToString() ?? "";
                
                cbEmpRole.Items.Clear();
                if (myCurrentRole == "superadmin")
                {
                    cbEmpRole.Items.AddRange(new string[] { "SuperAdmin", "Admin", "User" });
                    cbEmpRole.SelectedIndex = 2; 
                }
                else
                {
                    cbEmpRole.Items.Add("User");
                    cbEmpRole.SelectedIndex = 0;
                }

                lblEmpPassword.Visible = false; txtEmpPassword.Visible = false;
                btnDeleteEmp.Visible = true; btnIssueKey.Visible = true;
                SwitchPanel(pnlAddEditEmployee);
            }
        }

        private async void BtnSaveEmp_Click(object? sender, EventArgs e)
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

                    HttpResponseMessage response;
                    if (currentEditUserId == null) 
                    {
                        var newUser = new { userName = txtEmpUserName.Text, email = txtEmpEmail.Text, phoneNumber = txtEmpPhone.Text, password = txtEmpPassword.Text, role = cbEmpRole.Text };
                        var content = new StringContent(JsonSerializer.Serialize(newUser), Encoding.UTF8, "application/json");
                        response = await client.PostAsync("https://localhost:44387/api/app/employee/account", content);
                    }
                    else
                    {
                        var updatedUser = new { userName = txtEmpUserName.Text, email = txtEmpEmail.Text, phoneNumber = txtEmpPhone.Text, role = cbEmpRole.Text };
                        var content = new StringContent(JsonSerializer.Serialize(updatedUser), Encoding.UTF8, "application/json");
                        response = await client.PutAsync($"https://localhost:44387/api/app/employee/{currentEditUserId}/account", content);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Lưu thông tin thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SwitchPanel(pnlManageContent);
                        await LoadEmployeeListAsync(); 
                    }
                    else
                    {
                        string rawResponse = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(rawResponse)) MessageBox.Show($"Lỗi API! Mã lỗi: {response.StatusCode}", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        else
                        {
                            try {
                                var errorData = JsonSerializer.Deserialize<JsonElement>(rawResponse);
                                string errMsg = errorData.GetProperty("error").GetProperty("message").GetString() ?? "Lỗi không xác định";
                                MessageBox.Show($"Thất bại: {errMsg}", "Lỗi phân quyền", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            } catch { MessageBox.Show($"Lỗi máy chủ:\n{rawResponse}", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private async void BtnDeleteEmp_Click(object? sender, EventArgs e)
        {
            if (currentEditUserId == null) return;
            DialogResult result = MessageBox.Show($"Xóa tài khoản [{txtEmpUserName.Text}]?", "Cảnh báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                try
                {
                    HttpClientHandler handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
                        HttpResponseMessage response = await client.DeleteAsync($"https://localhost:44387/api/app/employee/{currentEditUserId}/account");

                        if (response.IsSuccessStatusCode) {
                            MessageBox.Show("Xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            SwitchPanel(pnlManageContent); await LoadEmployeeListAsync();
                        }
                        else {
                            string rawResponse = await response.Content.ReadAsStringAsync();
                            try {
                                var errorData = JsonSerializer.Deserialize<JsonElement>(rawResponse);
                                string errMsg = errorData.GetProperty("error").GetProperty("message").GetString() ?? "Lỗi quyền";
                                MessageBox.Show($"Lỗi: {errMsg}", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            } catch { MessageBox.Show($"Lỗi máy chủ:\n{rawResponse}", "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Lỗi máy chủ: " + ex.Message); }
            }
        }

        private async void BtnIssueKey_Click(object? sender, EventArgs e)
        {
            if (currentEditUserId == null) return;
            DialogResult result = MessageBox.Show($"CẤP KEY MỚI cho tài khoản [{txtEmpUserName.Text}]?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                try
                {
                    HttpClientHandler handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

                        var emptyContent = new StringContent("", Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await client.PostAsync($"https://localhost:44387/api/app/employee/{currentEditUserId}/reset-key", emptyContent);
                        string rawResponse = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode) {
                            string newKey = rawResponse.Replace("\"", "");
                            MessageBox.Show($"CẤP LẠI KEY THÀNH CÔNG!\nKey Mới: {newKey}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else {
                            try {
                                var errorData = JsonSerializer.Deserialize<JsonElement>(rawResponse);
                                string errMsg = errorData.GetProperty("error").GetProperty("message").GetString() ?? "Lỗi quyền";
                                MessageBox.Show($"Thất bại: {errMsg}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            } catch { MessageBox.Show($"Lỗi máy chủ:\n{rawResponse}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
            }
        }
    }
}