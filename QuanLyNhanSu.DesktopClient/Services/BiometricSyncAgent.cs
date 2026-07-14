using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;
using QuanLyNhanSu.DesktopClient.Models;

namespace QuanLyNhanSu.DesktopClient.Services
{
    /// <summary>
    /// Service chy `Tc lp ` `"ng bT sinh tr_c h?c.
    /// Gii quyt vn `?: Zero Memory Leak (ReleaseComObject) vA Resilience (Polly Circuit Breaker).
    /// </summary>
    public class BiometricSyncAgent
    {
        private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
        private readonly string _userToken;

        public BiometricSyncAgent(string userToken)
        {
            _userToken = userToken;
            
            // [ONBOARDING COMMENT]: Áp dụng Resilience bằng Polly. 
            // Nếu gọi API Backend thất bại 5 lần liên tục (do đứt cáp/server sập), ngắt mạch 30 giây để không treo luồng UI WinForms.
            _circuitBreaker = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }

        public async Task<(int Success, int Fail)> SyncTemplatesToDeviceAsync(string machineIp, int port = 4370, int machineNumber = 1)
        {
            dynamic? device = null;
            int successCount = 0;
            int failCount = 0;

            try
            {
                // 1. Kéo dữ liệu từ Backend qua API (Có bọc Polly Circuit Breaker)
                List<BiometricTemplateClientDto> templatesToSync = new();
                await _circuitBreaker.ExecuteAsync(async () =>
                {
                    var response = await ApiClient.GetAsync("api/app/biometric/all-templates", _userToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        templatesToSync = JsonSerializer.Deserialize<List<BiometricTemplateClientDto>>(json, options) ?? new List<BiometricTemplateClientDto>();
                    }
                    else
                    {
                        throw new Exception($"Lỗi từ Backend: {response.StatusCode}");
                    }
                });

                if (templatesToSync.Count == 0) return (0, 0);

                // 2. Kết nối máy chấm công (Hardware) qua COM (Late Binding để tránh phụ thuộc DLL tĩnh nếu cần)
                Type? type = Type.GetTypeFromProgID("zkemkeeper.CZKEM");
                if (type == null) throw new Exception("Không tìm thấy zkemkeeper COM Class. Hãy đăng ký DLL (regsvr32).");
                
                device = Activator.CreateInstance(type);
                if (device == null) throw new Exception("Không thể khởi tạo zkemkeeper.CZKEM.");

                bool isConnected = device.Connect_Net(machineIp, port);
                if (!isConnected) throw new Exception($"Không thể kết nối máy chấm công IP: {machineIp}:{port}");

                // [ONBOARDING COMMENT]: Khóa máy chấm công (không cho chấm) trong quá trình ghi đè dữ liệu để tránh xung đột file
                device.EnableDevice(machineNumber, false);

                foreach (var template in templatesToSync)
                {
                    bool ok = false;
                    // Bơm vân tay (index 0-9) hoặc khuôn mặt (index 50) xuống thiết bị.
                    if (template.TemplateType == "Fingerprint")
                    {
                        ok = device.SSR_SetUserTmpStr(machineNumber, template.EnrollNumber, template.FingerIndex, template.TemplateData);
                    }
                    else if (template.TemplateType == "Face")
                    {
                        ok = device.SetUserFaceStr(machineNumber, template.EnrollNumber, 50, template.TemplateData, template.TemplateLength);
                    }

                    if (ok) successCount++;
                    else failCount++;
                }

                // [ONBOARDING COMMENT]: QUY TẮC BẮT BUỘC. Nếu không gọi hàm này, máy chấm công sẽ không nạp vân tay từ Flash vào RAM, nhân viên quẹt thẻ sẽ bị báo "Xin thử lại".
                device.RefreshData(machineNumber);

                return (successCount, failCount);
            }
            finally
            {
                // 3. Dọn dẹp tài nguyên (Phải luôn luôn chạy dù thành công hay ném Exception)
                if (device != null)
                {
                    try
                    {
                        device.EnableDevice(machineNumber, true); // Mở khóa máy
                        device.Disconnect();
                    }
                    catch { /* Ignore disconnect errors */ }
                    
                    // [ONBOARDING COMMENT]: Quy tắc Zero-Tolerance với Memory Leak. 
                    // Phải ép HĐH giải phóng ngay vùng nhớ của đối tượng COM. Không phó mặc cho Garbage Collector của C#.
                    Marshal.ReleaseComObject(device);
                    device = null;
                }
            }
        }
    }
}
