using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.Mvc;

namespace QuanLyNhanSu.Controllers
{
    [ApiController]
    [Route("iclock")]
    [AllowAnonymous] // [ONBOARDING COMMENT]: Giao thức ADMS không dùng Bearer Token, authentication thông qua tham số SN (Serial Number)
    public class IclockController : QuanLyNhanSuController
    {
        private readonly ILogger<IclockController> _logger;
        private readonly IAttendanceAppService _attendanceAppService;

        public IclockController(ILogger<IclockController> logger, IAttendanceAppService attendanceAppService)
        {
            _logger = logger;
            _attendanceAppService = attendanceAppService;
        }

        // [ONBOARDING COMMENT]: Bước 1: Máy chấm công gọi GET /iclock/cdata để lấy cấu hình kết nối ban đầu
        [HttpGet("cdata")]
        public IActionResult InitDevice([FromQuery] string SN)
        {
            _logger.LogInformation($"[ADMS] Handshake from Device SN: {SN}");
            // Trả về cấu hình mẫu cho thiết bị ZKTeco
            string response = $"GET OPTION FROM: {SN}\r\n" +
                              "Stamp=9999\r\n" +
                              "OpStamp=9999\r\n" +
                              "ErrorDelay=60\r\n" +
                              "Delay=30\r\n" +
                              "TransTimes=00:00;14:00\r\n" +
                              "TransInterval=1\r\n" +
                              "TransFlag=1111000000\r\n" +
                              "TimeZone=7\r\n" +
                              "Realtime=1\r\n" +
                              "Encrypt=0";

            return Content(response, "text/plain");
        }

        // [ONBOARDING COMMENT]: Bước 2: Máy chấm công gửi dữ liệu (POST)
        [HttpPost("cdata")]
        public async Task<IActionResult> ReceiveData([FromQuery] string SN, [FromQuery] string table)
        {
            if (string.IsNullOrEmpty(SN)) return BadRequest("Missing SN");

            using StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8);
            string body = await reader.ReadToEndAsync();

            _logger.LogInformation($"[ADMS] Data received from {SN}. Table: {table}. Payload length: {body.Length}");

            // Chỉ xử lý dữ liệu chấm công (ATTLOG)
            if (table == "ATTLOG" && !string.IsNullOrWhiteSpace(body))
            {
                var inputList = new List<SyncAttendanceDto>();
                var lines = body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    // Format ZKTeco: DeviceUserId \t TimeStamp \t CheckType \t VerifyCode
                    // VD: 1    2023-01-01 08:00:00    1    1
                    var parts = line.Split('\t');
                    if (parts.Length >= 2)
                    {
                        var deviceUserId = parts[0];
                        if (DateTime.TryParse(parts[1], out var checkTime))
                        {
                            var dto = new SyncAttendanceDto
                            {
                                DeviceUserId = deviceUserId,
                                TimeStamp = checkTime,
                                // Tạm thời dùng DeviceUserId làm UserName (cần map ở Backend)
                                UserName = deviceUserId, 
                                CheckType = parts.Length >= 3 ? parts[2] : "0",
                                VerifyMethod = parts.Length >= 4 ? parts[3] : "1"
                            };
                            inputList.Add(dto);
                        }
                    }
                }

                if (inputList.Count > 0)
                {
                    // Chuyển việc lưu DB sang AppService. (Sẽ dùng Event-Driven ở các bước sau)
                    await _attendanceAppService.SyncBulkDataAsync(inputList);
                }
            }

            // [ONBOARDING COMMENT]: Bắt buộc phải trả về "OK" để máy ZKTeco hiểu là đã gửi thành công và xóa log lưu tạm.
            return Content("OK", "text/plain");
        }

        // Endpoint phụ: Máy chấm công gọi để nhận lệnh từ server (vd: xóa user, khởi động lại)
        [HttpGet("getrequest")]
        public IActionResult GetRequest([FromQuery] string SN)
        {
            return Content("OK", "text/plain");
        }
    }
}
