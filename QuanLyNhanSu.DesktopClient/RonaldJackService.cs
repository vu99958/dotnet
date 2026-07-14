using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using QuanLyNhanSu.DesktopClient.Models;

namespace QuanLyNhanSu.DesktopClient
{
    /// <summary>
    /// Service giao tiếp với máy chấm công Ronald Jack (Ronald Jack) thông qua COM zkemkeeper.dll.
    /// 
    /// Kiến trúc: Chia làm 4 vùng chức năng (#region) để dễ bảo trì:
    ///   1. Kết Nối - Connect, Disconnect, GetDeviceInfo
    ///   2. Đồng Bộ Log Chấm Công - GetAttendanceLogs
    ///   3. Sinh Trắc Học - Vân tay (Fingerprint) + Khuôn mặt (Face)
    ///   4. Quản Lý User Trên Máy - GetAllDeviceUsers
    /// 
    /// YÊU CẦU: Phải cài đặt Ronald Jack Standalone SDK và đăng ký COM:
    ///   regsvr32 zkemkeeper.dll (chạy với quyền Administrator)
    /// </summary>
    public class RonaldJackService : IDisposable
    {
        // Đối tượng COM giao tiếp với thiết bị
        private dynamic _rjDevice;
        private bool _isConnected;
        private int _machineNumber = 1; // Số máy mặc định (thường là 1)
        private string _deviceSerial = "";
        
        // Cấu hình mạng dùng cho tự động Reconnect
        private string _lastIp = "";
        private int _lastPort = 4370;

        // Sự kiện thông báo hệ thống
        public event Action<string> OnLogMessage;
        
        // Sự kiện thời gian thực (Real-time)
        public event Action<SyncAttendanceClientDto> OnRealTimeLogReceived;
        public event Action OnDeviceDisconnected;

        /// <summary>Trạng thái kết nối hiện tại</summary>
        public bool IsConnected => _isConnected;

        /// <summary>Serial Number của thiết bị đang kết nối</summary>
        public string DeviceSerial => _deviceSerial;

        public RonaldJackService()
        {
            _isConnected = false;
        }

        #region 1. KẾT NỐI

        /// <summary>
        /// Kết nối đến thiết bị Ronald Jack qua TCP/IP
        /// </summary>
        public bool Connect(string ip, int port = 4370)
        {
            try
            {
                Type zkemType = Type.GetTypeFromProgID("zkemkeeper.ZKEM.1");
                if (zkemType == null)
                {
                    LogMessage("LỖI: Không tìm thấy thư viện zkemkeeper.dll! Vui lòng cài đặt Ronald Jack SDK và chạy: regsvr32 zkemkeeper.dll");
                    return false;
                }

                _rjDevice = Activator.CreateInstance(zkemType)!;
                if (_rjDevice == null)
                {
                    LogMessage("LỖI: Không thể khởi tạo đối tượng COM zkemkeeper!");
                    return false;
                }

                LogMessage($"Đang kết nối tới thiết bị {ip}:{port}...");
                bool result = _rjDevice.Connect_Net(ip, port);

                if (result)
                {
                    _isConnected = true;
                    _rjDevice.EnableDevice(_machineNumber, false);

                    // Lưu Serial Number dùng khi upload sinh trắc học
                    try
                    {
                        string sn = "";
                        _rjDevice.GetSerialNumber(_machineNumber, out sn);
                        _deviceSerial = sn ?? "";
                    }
                    catch { _deviceSerial = ""; }

                    // Lưu thông tin kết nối để Reconnect sau này
                    _lastIp = ip;
                    _lastPort = port;

                    LogMessage($"Kết nối thành công tới máy chấm công tại {ip}:{port}");
                }
                else
                {
                    int errorCode = 0;
                    _rjDevice.GetLastError(ref errorCode);
                    LogMessage($"Kết nối thất bại! Mã lỗi: {errorCode}. Kiểm tra lại IP/Port và đảm bảo máy chấm công đã bật.");
                    _isConnected = false;
                }

                return _isConnected;
            }
            catch (COMException comEx)
            {
                LogMessage($"LỖI COM: {comEx.Message}. Hãy kiểm tra zkemkeeper.dll đã được đăng ký COM chưa (regsvr32 zkemkeeper.dll).");
                _isConnected = false;
                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"LỖI HỆ THỐNG: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        /// <summary>Ngắt kết nối với thiết bị</summary>
        public void Disconnect()
        {
            try
            {
                if (_rjDevice != null && _isConnected)
                {
                    _rjDevice.EnableDevice(_machineNumber, true);
                    _rjDevice.Disconnect();
                    LogMessage("Đã ngắt kết nối với thiết bị.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi khi ngắt kết nối: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
                _deviceSerial = "";
            }
        }

        /// <summary>Lấy thông tin thiết bị (Tên, Serial Number, Firmware)</summary>
        public string GetDeviceInfo()
        {
            if (_rjDevice == null || !_isConnected)
                return "Chưa kết nối";

            try
            {
                string serialNumber = "", deviceName = "", firmware = "";
                _rjDevice.GetSerialNumber(_machineNumber, out serialNumber);
                _rjDevice.GetProductName(_machineNumber, out deviceName);
                _rjDevice.GetFirmwareVersion(_machineNumber, out firmware);
                return $"Tên: {deviceName} | SN: {serialNumber} | FW: {firmware}";
            }
            catch
            {
                return "Không thể đọc thông tin thiết bị";
            }
        }


        /// <summary>
        /// Bật chế độ lắng nghe sự kiện thời gian thực từ thiết bị.
        /// Được gọi từ UI nếu người dùng muốn dùng Real-Time.
        /// </summary>
        public bool EnableRealTimeEvents()
        {
            if (!EnsureConnected()) return false;

            try
            {
                // Đăng ký nhận tất cả sự kiện (Mã 65535 = 0xFFFF)
                bool registered = _rjDevice!.RegEvent(_machineNumber, 65535);
                if (registered)
                {
                    // Hủy đăng ký cũ (tránh dội event nếu gọi nhiều lần)
                    _rjDevice.OnAttTransactionEx -= new Action<string, int, int, int, int, int, int, int, int, int, int>(Rj_OnAttTransactionEx);
                    _rjDevice.OnDisConnected -= new Action(Rj_OnDisConnected);

                    // Gắn hook mới
                    _rjDevice.OnAttTransactionEx += new Action<string, int, int, int, int, int, int, int, int, int, int>(Rj_OnAttTransactionEx);
                    _rjDevice.OnDisConnected += new Action(Rj_OnDisConnected);

                    LogMessage("Đã bật chế độ lắng nghe Chấm Công Thời Gian Thực (Real-Time).");
                    return true;
                }
                else
                {
                    LogErrorFromDevice("Không thể kích hoạt sự kiện Real-Time");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi đăng ký sự kiện: {ex.Message}");
                return false;
            }
        }

        private void Rj_OnAttTransactionEx(string enrollNumber, int isInValid, int attState, int verifyMethod, int year, int month, int day, int hour, int minute, int second, int workCode)
        {
            // Bỏ qua bản ghi không hợp lệ (isInValid != 0)
            if (isInValid != 0) return;

            var dto = new SyncAttendanceClientDto
            {
                UserName = enrollNumber.Trim(),
                TimeStamp = new DateTime(year, month, day, hour, minute, second),
                CheckType = MapInOutMode(attState),
                VerifyMethod = MapVerifyMode(verifyMethod),
                DeviceUserId = enrollNumber.Trim()
            };

            LogMessage($"[REAL-TIME] Nhân viên {dto.UserName} vừa chấm công ({dto.CheckType})");
            OnRealTimeLogReceived?.Invoke(dto);
        }

        private void Rj_OnDisConnected()
        {
            _isConnected = false;
            LogMessage("Thiết bị mất kết nối (Rớt mạng hoặc tắt nguồn)!");
            OnDeviceDisconnected?.Invoke();
        }

        /// <summary>
        /// Ping thiết bị nhẹ nhàng bằng cách đọc thời gian.
        /// Dùng cho luồng Auto-Recovery.
        /// </summary>
        public bool Ping()
        {
            if (!_isConnected || _rjDevice == null) return false;

            try
            {
                int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0;
                return _rjDevice.GetDeviceTime(_machineNumber, ref year, ref month, ref day, ref hour, ref minute, ref second);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Thử kết nối lại với thiết bị bằng IP/Port gần nhất.
        /// </summary>
        public bool Reconnect()
        {
            if (string.IsNullOrEmpty(_lastIp)) return false;
            Disconnect();
            System.Threading.Thread.Sleep(1000); // Đợi 1 giây
            return Connect(_lastIp, _lastPort);
        }

        #endregion

        #region 2. ĐỒNG BỘ LOG CHẤM CÔNG

        /// <summary>
        /// Đọc toàn bộ log chấm công từ thiết bị.
        /// Trả về danh sách DTO được parse và sẵn sàng gửi lên API.
        /// </summary>
        public List<SyncAttendanceClientDto> GetAttendanceLogs(DateTime? lastSyncTime = null)
        {
            var logs = new List<SyncAttendanceClientDto>();

            if (!EnsureConnected()) return logs;

            try
            {
                LogMessage("Đang đọc dữ liệu chấm công từ thiết bị...");

                bool readResult = _rjDevice!.ReadGeneralLogData(_machineNumber);
                if (!readResult)
                {
                    LogErrorFromDevice("Không thể đọc dữ liệu log");
                    return logs;
                }

                string enrollNumber = "";
                int verifyMode = 0, inOutMode = 0;
                int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0;
                int workCode = 0;
                int totalRead = 0, totalNew = 0;

                while (_rjDevice.SSR_GetGeneralLogData(
                    _machineNumber, out enrollNumber, out verifyMode, out inOutMode,
                    ref year, ref month, ref day, ref hour, ref minute, ref second, ref workCode))
                {
                    totalRead++;

                    DateTime logTime;
                    try { logTime = new DateTime(year, month, day, hour, minute, second); }
                    catch
                    {
                        LogMessage($"Bỏ qua bản ghi không hợp lệ: User={enrollNumber}");
                        continue;
                    }

                    if (lastSyncTime.HasValue && logTime <= lastSyncTime.Value)
                        continue;

                    logs.Add(new SyncAttendanceClientDto
                    {
                        UserName = enrollNumber.Trim(),
                        TimeStamp = logTime,
                        CheckType = MapInOutMode(inOutMode),
                        VerifyMethod = MapVerifyMode(verifyMode),
                        DeviceUserId = enrollNumber.Trim()
                    });
                    totalNew++;
                }

                LogMessage($"Đọc xong: Tổng {totalRead} bản ghi, {totalNew} bản ghi mới cần đồng bộ.");
                return logs;
            }
            catch (Exception ex)
            {
                LogMessage($"LỖI khi đọc log chấm công: {ex.Message}");
                return logs;
            }
        }

        #endregion

        #region 3. SINH TRẮC HỌC (VÂN TAY + KHUÔN MẶT)

        /// <summary>
        /// Đọc tất cả mẫu VÂN TAY từ thiết bị.
        /// Sử dụng hàm SSR_GetUserTmpStr() của SDK Ronald Jack.
        /// Mỗi nhân viên có thể có tối đa 10 mẫu vân tay (FingerIndex 0-9).
        /// </summary>
        public List<BiometricTemplateClientDto> GetAllFingerprints()
        {
            var results = new List<BiometricTemplateClientDto>();

            if (!EnsureConnected()) return results;

            try
            {
                LogMessage("Đang đọc dữ liệu VÂN TAY từ thiết bị...");

                // Lấy danh sách user trên máy trước
                var users = GetAllDeviceUsers();
                int totalFound = 0;

                foreach (var user in users)
                {
                    // Duyệt 10 ngón tay (index 0-9) cho mỗi nhân viên
                    for (int fingerIdx = 0; fingerIdx <= 9; fingerIdx++)
                    {
                        try
                        {
                            string tmpData = "";
                            int tmpLength = 0;

                            bool success = _rjDevice!.SSR_GetUserTmpStr(
                                _machineNumber,
                                user.EnrollNumber,
                                fingerIdx,
                                out tmpData,
                                out tmpLength
                            );

                            if (success && !string.IsNullOrEmpty(tmpData) && tmpLength > 0)
                            {
                                results.Add(new BiometricTemplateClientDto
                                {
                                    EnrollNumber = user.EnrollNumber,
                                    UserName = user.Name,
                                    TemplateType = "Fingerprint",
                                    FingerIndex = fingerIdx,
                                    TemplateData = tmpData,
                                    TemplateLength = tmpLength,
                                    SourceDeviceSerial = _deviceSerial
                                });
                                totalFound++;
                            }
                        }
                        catch { /* Bỏ qua ngón tay chưa đăng ký */ }
                    }
                }

                LogMessage($"Đọc xong VÂN TAY: Tìm thấy {totalFound} mẫu từ {users.Count} nhân viên.");
                return results;
            }
            catch (Exception ex)
            {
                LogMessage($"LỖI khi đọc vân tay: {ex.Message}");
                return results;
            }
        }

        /// <summary>
        /// Đọc tất cả mẫu KHUÔN MẶT từ thiết bị.
        /// Sử dụng hàm GetUserFaceStr() của SDK Ronald Jack.
        /// Mỗi nhân viên chỉ có tối đa 1 mẫu khuôn mặt (FaceIndex = 50).
        /// </summary>
        public List<BiometricTemplateClientDto> GetAllFaceTemplates()
        {
            var results = new List<BiometricTemplateClientDto>();

            if (!EnsureConnected()) return results;

            try
            {
                LogMessage("Đang đọc dữ liệu KHUÔN MẶT từ thiết bị...");

                var users = GetAllDeviceUsers();
                int totalFound = 0;

                foreach (var user in users)
                {
                    try
                    {
                        string faceData = "";
                        int faceLength = 0;
                        // FaceIndex = 50 là quy ước của Ronald Jack cho dữ liệu khuôn mặt
                        int faceIndex = 50;

                        bool success = _rjDevice!.GetUserFaceStr(
                            _machineNumber,
                            user.EnrollNumber,
                            faceIndex,
                            ref faceData,
                            ref faceLength
                        );

                        if (success && !string.IsNullOrEmpty(faceData) && faceLength > 0)
                        {
                            results.Add(new BiometricTemplateClientDto
                            {
                                EnrollNumber = user.EnrollNumber,
                                UserName = user.Name,
                                TemplateType = "Face",
                                FingerIndex = -1, // -1 = Không phải vân tay
                                TemplateData = faceData,
                                TemplateLength = faceLength,
                                SourceDeviceSerial = _deviceSerial
                            });
                            totalFound++;
                        }
                    }
                    catch { /* Bỏ qua user chưa đăng ký khuôn mặt */ }
                }

                LogMessage($"Đọc xong KHUÔN MẶT: Tìm thấy {totalFound} mẫu từ {users.Count} nhân viên.");
                return results;
            }
            catch (Exception ex)
            {
                LogMessage($"LỖI khi đọc khuôn mặt: {ex.Message}");
                return results;
            }
        }

        /// <summary>
        /// Ghi 1 mẫu VÂN TAY vào thiết bị (dùng khi đồng bộ từ Server xuống máy).
        /// </summary>
        public bool SetFingerprint(string enrollNumber, int fingerIndex, string templateData, int templateLength)
        {
            if (!EnsureConnected()) return false;

            try
            {
                bool success = _rjDevice!.SSR_SetUserTmpStr(
                    _machineNumber,
                    enrollNumber,
                    fingerIndex,
                    templateData,
                    templateLength
                );

                if (success)
                {
                    LogMessage($"Ghi vân tay thành công: User={enrollNumber}, Ngón={fingerIndex}");
                }
                else
                {
                    LogErrorFromDevice($"Ghi vân tay thất bại: User={enrollNumber}, Ngón={fingerIndex}");
                }

                return success;
            }
            catch (Exception ex)
            {
                LogMessage($"LỖI ghi vân tay: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ghi 1 mẫu KHUÔN MẶT vào thiết bị (dùng khi đồng bộ từ Server xuống máy).
        /// </summary>
        public bool SetFaceTemplate(string enrollNumber, string faceData, int faceLength)
        {
            if (!EnsureConnected()) return false;

            try
            {
                int faceIndex = 50; // Quy ước Ronald Jack

                bool success = _rjDevice!.SetUserFaceStr(
                    _machineNumber,
                    enrollNumber,
                    faceIndex,
                    faceData,
                    faceLength
                );

                if (success)
                {
                    LogMessage($"Ghi khuôn mặt thành công: User={enrollNumber}");
                }
                else
                {
                    LogErrorFromDevice($"Ghi khuôn mặt thất bại: User={enrollNumber}");
                }

                return success;
            }
            catch (Exception ex)
            {
                LogMessage($"LỖI ghi khuôn mặt: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 4. QUẢN LÝ USER TRÊN MÁY

        /// <summary>
        /// Lấy danh sách tất cả nhân viên đã đăng ký trên máy chấm công.
        /// </summary>
        public List<DeviceUserInfoDto> GetAllDeviceUsers()
        {
            var users = new List<DeviceUserInfoDto>();

            if (!EnsureConnected()) return users;

            try
            {
                bool readResult = _rjDevice!.ReadAllUserID(_machineNumber);
                if (!readResult)
                {
                    LogErrorFromDevice("Không thể đọc danh sách user trên máy");
                    return users;
                }

                string enrollNumber = "", name = "", password = "";
                int privilege = 0;
                bool enabled = false;

                while (_rjDevice.SSR_GetAllUserInfo(
                    _machineNumber,
                    out enrollNumber,
                    out name,
                    out password,
                    out privilege,
                    out enabled))
                {
                    users.Add(new DeviceUserInfoDto
                    {
                        EnrollNumber = enrollNumber?.Trim() ?? "",
                        Name = name?.Trim() ?? "",
                        Privilege = privilege
                    });
                }

                LogMessage($"Tìm thấy {users.Count} nhân viên trên thiết bị.");
                return users;
            }
            catch (Exception ex)
            {
                LogMessage($"LỖI khi đọc danh sách user: {ex.Message}");
                return users;
            }
        }

        #endregion

        #region HELPER & MAPPING

        /// <summary>
        /// Cập nhật lại bộ nhớ của thiết bị (cần gọi sau khi ghi dữ liệu mới).
        /// </summary>
        public void RefreshDeviceData()
        {
            if (!EnsureConnected()) return;
            try
            {
                _rjDevice!.RefreshData(_machineNumber);
                LogMessage("Đã làm mới bộ nhớ thiết bị.");
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi khi làm mới thiết bị: {ex.Message}");
            }
        }

        /// <summary>Kiểm tra kết nối trước khi thao tác. Trả về false nếu chưa kết nối.</summary>
        private bool EnsureConnected()
        {
            if (_rjDevice == null || !_isConnected)
            {
                LogMessage("Chưa kết nối với thiết bị. Vui lòng kết nối trước.");
                return false;
            }
            return true;
        }

        /// <summary>Đọc mã lỗi từ thiết bị và ghi log</summary>
        private void LogErrorFromDevice(string context)
        {
            int errorCode = 0;
            _rjDevice.GetLastError(ref errorCode);
            LogMessage($"{context}! Mã lỗi thiết bị: {errorCode}");
        }

        /// <summary>Mapping mã VerifyMode tên phương thức xác thực</summary>
        private string MapVerifyMode(int verifyMode)
        {
            return verifyMode switch
            {
                0 => "Fingerprint",    // Vân tay
                1 => "Fingerprint",    // Vân tay (loại 2)
                15 => "Face",          // Khuôn mặt
                2 => "Card",           // Thẻ từ
                3 => "Password",       // Mật khẩu
                _ => $"Unknown({verifyMode})"
            };
        }

        /// <summary>Mapping mã InOutMode trạng thái Check-in/Check-out</summary>
        private string MapInOutMode(int inOutMode)
        {
            return inOutMode switch
            {
                0 => "IN",     // Check-in
                1 => "OUT",    // Check-out
                2 => "IN",     // Break-in
                3 => "OUT",    // Break-out
                4 => "IN",     // Overtime-in
                5 => "OUT",    // Overtime-out
                _ => "IN"      // Mặc định
            };
        }

        /// <summary>Ghi log nội bộ và phát sự kiện ra ngoài</summary>
        private void LogMessage(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            OnLogMessage?.Invoke(timestampedMessage);
        }

        /// <summary>Giải phóng tài nguyên COM</summary>
        public void Dispose()
        {
            Disconnect();
            if (_rjDevice != null)
            {
                try { Marshal.ReleaseComObject(_rjDevice); }
                catch { }
                _rjDevice = null;
            }
        }

        #endregion
    }
}
