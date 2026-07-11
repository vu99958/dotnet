using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using QuanLyNhanSu.DesktopClient.Services;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class FormDashboard
    {
        // ==========================================
        // LOGIC HỒ SƠ (PROFILE)
        // ==========================================
        private async void BtnEditProfile_Click(object? sender, EventArgs e)
        {
            isEditMode = !isEditMode;
            if (isEditMode)
            {
                txtEditEmail.ReadOnly = false; txtEditEmail.BorderStyle = BorderStyle.FixedSingle;
                txtEditPhone.ReadOnly = false; txtEditPhone.BorderStyle = BorderStyle.FixedSingle;
                txtEditAddress.ReadOnly = false; txtEditAddress.BorderStyle = BorderStyle.FixedSingle;
                btnEditProfile.Text = "💾 LƯU THÔNG TIN"; btnEditProfile.BackColor = Color.OrangeRed;
            }
            else
            {
                // BUG-08 FIX: Gọi API cập nhật thật thay vì chỉ MessageBox
                try
                {
                    var payload = new
                    {
                        email = txtEditEmail.Text,
                        phoneNumber = txtEditPhone.Text
                    };
                    var json = System.Text.Json.JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    HttpResponseMessage response = await ApiClient.PostAsync("api/app/my-profile/update-my-profile", content, userToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Cập nhật hồ sơ thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Lỗi khi cập nhật: " + await response.Content.ReadAsStringAsync(), "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kết nối: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                txtEditEmail.ReadOnly = true; txtEditEmail.BorderStyle = BorderStyle.None;
                txtEditPhone.ReadOnly = true; txtEditPhone.BorderStyle = BorderStyle.None;
                txtEditAddress.ReadOnly = true; txtEditAddress.BorderStyle = BorderStyle.None;
                btnEditProfile.Text = "✍️ CHỈNH SỬA HỒ SƠ"; btnEditProfile.BackColor = Color.FromArgb(32, 161, 68);
            }
        }

        private async Task LoadMyProfileAsync()
        {
            lblProName.Text = "Đang tải dữ liệu...";
            try
            {
                if (string.IsNullOrEmpty(userToken)) return;
                
                HttpResponseMessage response = await ApiClient.GetAsync("api/app/my-profile/my-profile", userToken);
                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                    lblProName.Text = data.GetProperty("userName").GetString()?.ToUpper();
                    txtEditEmail.Text = data.GetProperty("email").GetString();
                    string roleStr = data.GetProperty("roles").GetString() ?? "USER";
                    lblProRole.Text = string.IsNullOrEmpty(roleStr) ? "USER" : roleStr.ToUpper();
                    txtEditBranch.Text = data.TryGetProperty("branchName", out var bn) && bn.ValueKind == JsonValueKind.String ? bn.GetString() : "Chưa phân bổ";
                    DateTime creationTime = data.GetProperty("creationTime").GetDateTime();
                    lblProDate.Text = "Thành viên từ: " + creationTime.ToString("dd/MM/yyyy");
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }
    }
}