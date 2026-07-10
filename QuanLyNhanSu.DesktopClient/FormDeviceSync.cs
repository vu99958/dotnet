using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using QuanLyNhanSu.DesktopClient.Models;

namespace QuanLyNhanSu.DesktopClient
{
    /// <summary>
    /// Form quản lý đồng bộ dữ liệu từ máy chấm công Ronald Jack.
    /// Gồm 2 Tab: (1) Đồng bộ chấm công, (2) Sinh trắc học.
    /// Chỉ dành cho Admin/SuperAdmin.
    /// </summary>
    public class FormDeviceSync : Form
    {
        // === SERVICE & STATE ===
        private RonaldJackService _deviceService;
        private string _userToken;
        private DateTime? _lastSyncTime;

        // === UI — Kết Nối (dùng chung cho cả 2 tab) ===
        private TextBox txtIpAddress = null!;
        private NumericUpDown nudPort = null!;
        private Button btnConnect = null!, btnDisconnect = null!;
        private Label lblConnectionStatus = null!, lblDeviceInfo = null!;

        // === UI — Tab 1: Đồng Bộ Chấm Công ===
        private Button btnSyncNow = null!, btnRealTime = null!;
        private Label lblLastSync = null!, lblSyncStatus = null!;
        private DataGridView dgvSyncLog = null!;
        private System.Windows.Forms.Timer? _pingTimer;
        private bool _isRealTimeRunning = false;

        // === UI — Tab 2: Sinh Trắc Học ===
        private Button btnDownloadBio = null!, btnUploadBio = null!;
        private Label lblBioStatus = null!;
        private DataGridView dgvBiometric = null!;

        // === CONSTANTS ===
        private const string API_BASE_URL = "https://localhost:44387";
        private const int DEFAULT_SYNC_INTERVAL_MS = 5 * 60 * 1000;

        // === MÀU SẮC (dùng chung) ===
        private readonly Color _primaryBlue = Color.FromArgb(0, 102, 204);
        private readonly Color _successGreen = Color.FromArgb(32, 161, 68);
        private readonly Color _warningOrange = Color.FromArgb(255, 140, 0);
        private readonly Color _dangerRed = Color.FromArgb(220, 53, 69);
        private readonly Color _darkGray = Color.FromArgb(80, 80, 80);

        public FormDeviceSync(string token)
        {
            _userToken = token;
            _deviceService = new RonaldJackService();
            _deviceService.OnLogMessage += OnDeviceLogReceived;
            _deviceService.OnRealTimeLogReceived += OnRealTimeLogReceived;
            _deviceService.OnDeviceDisconnected += OnDeviceDisconnected;

            InitializeFormUI();
            InitializePingTimer();
        }

        private void InitializePingTimer()
        {
            _pingTimer = new System.Windows.Forms.Timer { Interval = 10000 }; // 10 giây
            _pingTimer.Tick += PingTimer_Tick;
        }

        #region ═══════════════ KHỞI TẠO GIAO DIỆN ═══════════════

        private void InitializeFormUI()
        {
            // === CẤU HÌNH FORM ===
            this.Text = "🔄 Quản Lý Máy Chấm Công Ronald Jack";
            this.Size = new Size(920, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Font = new Font("Segoe UI", 10F);
            this.BackColor = Color.FromArgb(240, 242, 245);

            // === TIÊU ĐỀ ===
            Label lblTitle = new Label
            {
                Text = "⚙️ QUẢN LÝ MÁY CHẤM CÔNG RONALD JACK",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = _primaryBlue,
                Dock = DockStyle.Top, Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // === NHÓM KẾT NỐI (dùng chung) ===
            GroupBox grpConnection = BuildConnectionGroup();

            // === TAB CONTROL ===
            TabControl tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            TabPage tabSync = new TabPage("📋 Đồng Bộ Chấm Công");
            TabPage tabBio = new TabPage("🧬 Sinh Trắc Học");

            BuildSyncTab(tabSync);
            BuildBiometricTab(tabBio);

            tabControl.TabPages.Add(tabSync);
            tabControl.TabPages.Add(tabBio);

            // === NÚT ĐÓNG ===
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 45 };
            Button btnClose = new Button
            {
                Text = "❌ ĐÓNG", Dock = DockStyle.Right, Width = 130,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnClose);

            // === GẮN VÀO FORM ===
            this.Controls.Add(tabControl);
            this.Controls.Add(grpConnection);
            this.Controls.Add(lblTitle);
            this.Controls.Add(pnlBottom);
        }

        /// <summary>Xây dựng GroupBox kết nối thiết bị (dùng chung cho cả 2 tab)</summary>
        private GroupBox BuildConnectionGroup()
        {
            GroupBox grp = new GroupBox
            {
                Text = "📡 Kết Nối Thiết Bị", Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = _darkGray, Dock = DockStyle.Top, Height = 100, Padding = new Padding(15, 5, 15, 5)
            };

            Label lblIp = new Label { Text = "IP:", Location = new Point(15, 28), AutoSize = true, Font = new Font("Segoe UI", 10F) };
            txtIpAddress = new TextBox { Text = "192.168.1.201", Location = new Point(40, 25), Width = 160, Font = new Font("Segoe UI", 10F), BorderStyle = BorderStyle.FixedSingle };

            Label lblPort = new Label { Text = "Port:", Location = new Point(210, 28), AutoSize = true, Font = new Font("Segoe UI", 10F) };
            nudPort = new NumericUpDown { Value = 4370, Minimum = 1, Maximum = 65535, Location = new Point(260, 25), Width = 70, Font = new Font("Segoe UI", 10F) };

            btnConnect = CreateButton("🔗 Kết Nối", new Point(345, 22), 110, _successGreen);
            btnConnect.Click += BtnConnect_Click;

            btnDisconnect = CreateButton("🔌 Ngắt", new Point(465, 22), 85, _dangerRed);
            btnDisconnect.Enabled = false;
            btnDisconnect.Click += BtnDisconnect_Click;

            lblConnectionStatus = new Label { Text = "⚪ Chưa kết nối", Location = new Point(15, 60), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            lblDeviceInfo = new Label { Text = "", Location = new Point(200, 62), Width = 500, Height = 20, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 9F, FontStyle.Italic) };

            grp.Controls.AddRange(new Control[] { lblIp, txtIpAddress, lblPort, nudPort, btnConnect, btnDisconnect, lblConnectionStatus, lblDeviceInfo });
            return grp;
        }

        /// <summary>Xây dựng Tab 1: Đồng Bộ Chấm Công</summary>
        private void BuildSyncTab(TabPage tab)
        {
            tab.BackColor = Color.FromArgb(240, 242, 245);

            // Thanh công cụ
            Panel pnlSyncTools = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(10, 5, 10, 5) };

            btnSyncNow = CreateButton("⚡ Đồng Bộ Ngay", new Point(5, 5), 150, _primaryBlue);
            btnSyncNow.Enabled = false;
            btnSyncNow.Click += async (s, e) => await PerformSyncAsync();

            btnRealTime = CreateButton("▶️ Bật Chấm Công Real-Time", new Point(165, 5), 220, _warningOrange);
            btnRealTime.Enabled = false;
            btnRealTime.Click += BtnRealTime_Click;

            lblLastSync = new Label { Text = "Lần cuối: Chưa bao giờ", Location = new Point(400, 10), AutoSize = true, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 9F, FontStyle.Italic) };

            pnlSyncTools.Controls.AddRange(new Control[] { btnSyncNow, btnRealTime, lblLastSync });

            // Label trạng thái
            lblSyncStatus = new Label { Text = "", Dock = DockStyle.Top, Height = 25, ForeColor = _primaryBlue, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Padding = new Padding(10, 3, 0, 0) };

            // Bảng log
            dgvSyncLog = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9F), SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvSyncLog.Columns.Add("Time", "Thời gian");
            dgvSyncLog.Columns.Add("Action", "Hành động");
            dgvSyncLog.Columns.Add("Records", "Số bản ghi");
            dgvSyncLog.Columns.Add("Result", "Kết quả");
            dgvSyncLog.Columns.Add("Detail", "Chi tiết");
            dgvSyncLog.Columns["Time"].Width = 120;
            dgvSyncLog.Columns["Records"].Width = 80;
            dgvSyncLog.Columns["Result"].Width = 80;

            dgvSyncLog.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 3 && e.Value != null)
                {
                    string result = e.Value.ToString() ?? "";
                    if (result == "Thành công") { e.CellStyle.ForeColor = Color.White; e.CellStyle.BackColor = _successGreen; e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold); }
                    else if (result == "Thất bại") { e.CellStyle.ForeColor = Color.White; e.CellStyle.BackColor = _dangerRed; e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold); }
                }
            };

            tab.Controls.Add(dgvSyncLog);
            tab.Controls.Add(lblSyncStatus);
            tab.Controls.Add(pnlSyncTools);
        }

        /// <summary>Xây dựng Tab 2: Sinh Trắc Học (Vân Tay + Khuôn Mặt)</summary>
        private void BuildBiometricTab(TabPage tab)
        {
            tab.BackColor = Color.FromArgb(240, 242, 245);

            // Thanh công cụ
            Panel pnlBioTools = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(10, 5, 10, 5) };

            btnDownloadBio = CreateButton("📥 Tải từ Máy → Server", new Point(5, 5), 200, _primaryBlue);
            btnDownloadBio.Enabled = false;
            btnDownloadBio.Click += async (s, e) => await DownloadBiometricFromDeviceAsync();

            btnUploadBio = CreateButton("📤 Đẩy từ Server → Máy", new Point(215, 5), 200, _successGreen);
            btnUploadBio.Enabled = false;
            btnUploadBio.Click += async (s, e) => await UploadBiometricToDeviceAsync();

            pnlBioTools.Controls.AddRange(new Control[] { btnDownloadBio, btnUploadBio });

            // Label trạng thái
            lblBioStatus = new Label { Text = "Kết nối thiết bị để bắt đầu.", Dock = DockStyle.Top, Height = 25, ForeColor = _primaryBlue, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Padding = new Padding(10, 3, 0, 0) };

            // Bảng dữ liệu sinh trắc học
            dgvBiometric = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9F), SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvBiometric.Columns.Add("EnrollNo", "Mã NV");
            dgvBiometric.Columns.Add("Name", "Tên");
            dgvBiometric.Columns.Add("Type", "Loại");
            dgvBiometric.Columns.Add("FingerIdx", "Ngón tay");
            dgvBiometric.Columns.Add("DataLen", "Kích thước (bytes)");
            dgvBiometric.Columns.Add("Source", "Nguồn");

            dgvBiometric.Columns["EnrollNo"].Width = 70;
            dgvBiometric.Columns["FingerIdx"].Width = 70;
            dgvBiometric.Columns["DataLen"].Width = 100;

            // Tô màu theo loại sinh trắc học
            dgvBiometric.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 2 && e.Value != null)
                {
                    string type = e.Value.ToString() ?? "";
                    if (type == "Fingerprint") { e.CellStyle.ForeColor = Color.White; e.CellStyle.BackColor = _primaryBlue; e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold); }
                    else if (type == "Face") { e.CellStyle.ForeColor = Color.White; e.CellStyle.BackColor = _successGreen; e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold); }
                }
            };

            tab.Controls.Add(dgvBiometric);
            tab.Controls.Add(lblBioStatus);
            tab.Controls.Add(pnlBioTools);
        }

        /// <summary>Helper tạo Button có style nhất quán</summary>
        private Button CreateButton(string text, Point location, int width, Color backColor)
        {
            var btn = new Button
            {
                Text = text, Location = location, Width = width, Height = 32,
                BackColor = backColor, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        #endregion

        #region ═══════════════ XỬ LÝ KẾT NỐI ═══════════════

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            string ip = txtIpAddress.Text.Trim();
            int port = (int)nudPort.Value;

            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ IP!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConnect.Enabled = false;
            btnConnect.Text = "Đang kết nối...";

            bool success = _deviceService.Connect(ip, port);

            if (success)
            {
                lblConnectionStatus.Text = "🟢 Đã kết nối";
                lblConnectionStatus.ForeColor = _successGreen;
                lblDeviceInfo.Text = _deviceService.GetDeviceInfo();

                SetConnectionState(true);
                AddSyncLogEntry("Kết nối", "-", "Thành công", $"Kết nối tới {ip}:{port}");
            }
            else
            {
                lblConnectionStatus.Text = "🔴 Kết nối thất bại";
                lblConnectionStatus.ForeColor = _dangerRed;
                btnConnect.Enabled = true;
                AddSyncLogEntry("Kết nối", "-", "Thất bại", $"Không thể kết nối tới {ip}:{port}");
            }

            btnConnect.Text = "🔗 Kết Nối";
        }

        private void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            if (_isRealTimeRunning) StopRealTime();
            _deviceService.Disconnect();

            lblConnectionStatus.Text = "⚪ Chưa kết nối";
            lblConnectionStatus.ForeColor = Color.Gray;
            lblDeviceInfo.Text = "";

            SetConnectionState(false);
            AddSyncLogEntry("Ngắt kết nối", "-", "Thành công", "Đã ngắt kết nối");
        }

        /// <summary>Bật/tắt các nút theo trạng thái kết nối</summary>
        private void SetConnectionState(bool connected)
        {
            btnConnect.Enabled = !connected;
            btnDisconnect.Enabled = connected;
            txtIpAddress.Enabled = !connected;
            nudPort.Enabled = !connected;

            // Tab 1
            btnSyncNow.Enabled = connected;
            btnRealTime.Enabled = connected;

            // Tab 2
            btnDownloadBio.Enabled = connected;
            btnUploadBio.Enabled = connected;
        }

        #endregion

        #region ═══════════════ TAB 1: ĐỒNG BỘ CHẤM CÔNG ═══════════════

        private async Task PerformSyncAsync()
        {
            if (!_deviceService.IsConnected) return;

            btnSyncNow.Enabled = false;
            btnSyncNow.Text = "⏳ Đang đồng bộ...";
            lblSyncStatus.Text = "Đang đọc dữ liệu từ thiết bị...";

            try
            {
                var logs = _deviceService.GetAttendanceLogs(_lastSyncTime);

                if (logs.Count == 0)
                {
                    lblSyncStatus.Text = "Không có dữ liệu mới.";
                    AddSyncLogEntry("Đồng bộ", "0", "Thành công", "Không có bản ghi mới");
                    _lastSyncTime = DateTime.Now;
                    lblLastSync.Text = $"Lần cuối: {_lastSyncTime:dd/MM HH:mm:ss}";
                    return;
                }

                lblSyncStatus.Text = $"Đang gửi {logs.Count} bản ghi lên server...";
                int insertedCount = await SendAttendanceToServerAsync(logs);

                if (insertedCount >= 0)
                {
                    _lastSyncTime = DateTime.Now;
                    lblLastSync.Text = $"Lần cuối: {_lastSyncTime:dd/MM HH:mm:ss}";
                    lblSyncStatus.Text = $"✅ Đồng bộ thành công: {insertedCount} bản ghi mới.";
                    AddSyncLogEntry("Đồng bộ", logs.Count.ToString(), "Thành công", $"Insert {insertedCount} bản ghi");
                }
                else
                {
                    lblSyncStatus.Text = "❌ Đồng bộ thất bại!";
                    AddSyncLogEntry("Đồng bộ", logs.Count.ToString(), "Thất bại", "Lỗi API server");
                }
            }
            catch (Exception ex)
            {
                lblSyncStatus.Text = $"❌ Lỗi: {ex.Message}";
                AddSyncLogEntry("Đồng bộ", "-", "Thất bại", ex.Message);
            }
            finally
            {
                btnSyncNow.Enabled = true;
                btnSyncNow.Text = "⚡ Đồng Bộ Ngay";
            }
        }

        private void BtnRealTime_Click(object? sender, EventArgs e)
        {
            if (_isRealTimeRunning) StopRealTime();
            else StartRealTime();
        }

        private void StartRealTime()
        {
            if (_deviceService.EnableRealTimeEvents())
            {
                _isRealTimeRunning = true;
                btnRealTime.Text = "⏸️ Tắt Chấm Công Real-Time";
                btnRealTime.BackColor = _dangerRed;
                lblSyncStatus.Text = "📡 Đang lắng nghe dữ liệu trực tiếp từ máy chấm công...";
                _pingTimer?.Start(); // Bắt đầu giám sát mạng
                AddSyncLogEntry("Real-Time", "-", "Thành công", "Bật chế độ lắng nghe sự kiện");
            }
            else
            {
                MessageBox.Show("Không thể bật chế độ Real-Time!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopRealTime()
        {
            _isRealTimeRunning = false;
            btnRealTime.Text = "▶️ Bật Chấm Công Real-Time";
            btnRealTime.BackColor = _warningOrange;
            lblSyncStatus.Text = "Chế độ Real-Time đã tắt.";
            _pingTimer?.Stop();
            AddSyncLogEntry("Real-Time", "-", "Thành công", "Tắt chế độ lắng nghe");
        }

        // ================= XỬ LÝ SỰ KIỆN REAL-TIME & AUTO-RECOVERY =================
        
        private async void OnRealTimeLogReceived(SyncAttendanceClientDto dto)
        {
            // Cần invoke nếu không nằm trên UI thread
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnRealTimeLogReceived(dto)));
                return;
            }

            try
            {
                // Gửi thẳng lên Server
                var logs = new List<SyncAttendanceClientDto> { dto };
                int insertedCount = await SendAttendanceToServerAsync(logs);

                if (insertedCount > 0)
                {
                    AddSyncLogEntry("Real-Time Push", "1", "Thành công", $"User {dto.UserName} ({dto.CheckType})");
                    _lastSyncTime = DateTime.Now;
                    lblLastSync.Text = $"Lần cuối: {_lastSyncTime:dd/MM HH:mm:ss}";
                }
                else
                {
                    AddSyncLogEntry("Real-Time Push", "1", "Thất bại", $"Lỗi gửi API user {dto.UserName}");
                }
            }
            catch (Exception ex)
            {
                AddSyncLogEntry("Real-Time Push", "1", "Lỗi", ex.Message);
            }
        }

        private void OnDeviceDisconnected()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(OnDeviceDisconnected));
                return;
            }
            lblConnectionStatus.Text = "🔴 Mất kết nối - Đang thử lại...";
            lblConnectionStatus.ForeColor = _dangerRed;
            SetConnectionState(false);
            btnDisconnect.Enabled = true; // Cho phép bấm nút ngắt thực sự
        }

        private async void PingTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isRealTimeRunning) return;

            // Kiểm tra kết nối (Ping nhẹ bằng GetDeviceTime)
            if (!_deviceService.Ping())
            {
                // Ping thất bại -> Gọi Reconnect
                _pingTimer?.Stop();
                OnDeviceDisconnected();
                AddSyncLogEntry("Auto-Recovery", "-", "Đang thử", "Mất kết nối, tự động kết nối lại...");

                await Task.Run(() =>
                {
                    int attempts = 0;
                    while (!_deviceService.IsConnected && _isRealTimeRunning)
                    {
                        attempts++;
                        OnDeviceLogReceived($"Đang thử lại lần {attempts}...");
                        bool success = _deviceService.Reconnect();
                        
                        if (success)
                        {
                            OnDeviceLogReceived("✅ Nối lại thành công!");
                            this.Invoke(new Action(() =>
                            {
                                lblConnectionStatus.Text = "🟢 Đã kết nối lại";
                                lblConnectionStatus.ForeColor = _successGreen;
                                SetConnectionState(true);
                                
                                // Phải đăng ký lại Event vì COM object đã bị hủy và tạo lại
                                _deviceService.EnableRealTimeEvents();
                                _pingTimer?.Start();
                                AddSyncLogEntry("Auto-Recovery", "-", "Thành công", $"Đã khôi phục sau {attempts} lần");
                            }));
                            break;
                        }
                        
                        System.Threading.Thread.Sleep(5000); // Thử lại mỗi 5 giây
                    }
                });
            }
        }

        #endregion

        #region ═══════════════ TAB 2: SINH TRẮC HỌC ═══════════════

        /// <summary>
        /// Tải tất cả vân tay + khuôn mặt TỪ MÁY CHẤM CÔNG → Lưu lên Server.
        /// Dùng khi muốn backup hoặc chuẩn bị đồng bộ chéo sang máy khác.
        /// </summary>
        private async Task DownloadBiometricFromDeviceAsync()
        {
            btnDownloadBio.Enabled = false;
            btnDownloadBio.Text = "⏳ Đang tải...";
            lblBioStatus.Text = "Đang đọc vân tay từ thiết bị...";
            dgvBiometric.Rows.Clear();

            try
            {
                // Bước 1: Đọc vân tay
                var fingerprints = _deviceService.GetAllFingerprints();
                foreach (var fp in fingerprints)
                {
                    dgvBiometric.Rows.Add(fp.EnrollNumber, fp.UserName, fp.TemplateType,
                        $"Ngón {fp.FingerIndex}", fp.TemplateLength.ToString(), fp.SourceDeviceSerial);
                }

                // Bước 2: Đọc khuôn mặt
                lblBioStatus.Text = "Đang đọc khuôn mặt từ thiết bị...";
                var faces = _deviceService.GetAllFaceTemplates();
                foreach (var face in faces)
                {
                    dgvBiometric.Rows.Add(face.EnrollNumber, face.UserName, face.TemplateType,
                        "Khuôn mặt", face.TemplateLength.ToString(), face.SourceDeviceSerial);
                }

                int total = fingerprints.Count + faces.Count;
                lblBioStatus.Text = $"Đọc xong: {fingerprints.Count} vân tay + {faces.Count} khuôn mặt. Đang gửi lên server...";

                if (total == 0)
                {
                    lblBioStatus.Text = "Không tìm thấy dữ liệu sinh trắc học trên thiết bị.";
                    return;
                }

                // Bước 3: Gửi lên Backend API
                var allTemplates = new List<BiometricTemplateClientDto>();
                allTemplates.AddRange(fingerprints);
                allTemplates.AddRange(faces);

                int saved = await SendBiometricToServerAsync(allTemplates);
                lblBioStatus.Text = saved >= 0
                    ? $"✅ Hoàn tất: {saved} mẫu sinh trắc học đã lưu lên server."
                    : "❌ Lỗi khi gửi dữ liệu lên server!";
            }
            catch (Exception ex)
            {
                lblBioStatus.Text = $"❌ Lỗi: {ex.Message}";
            }
            finally
            {
                btnDownloadBio.Enabled = true;
                btnDownloadBio.Text = "📥 Tải từ Máy → Server";
            }
        }

        /// <summary>
        /// Tải tất cả vân tay + khuôn mặt TỪ SERVER → Ghi vào MÁY CHẤM CÔNG hiện tại.
        /// Dùng khi muốn đồng bộ dữ liệu từ máy A sang máy B.
        /// </summary>
        private async Task UploadBiometricToDeviceAsync()
        {
            var confirm = MessageBox.Show(
                "Bạn có chắc muốn TẢI TẤT CẢ vân tay & khuôn mặt từ Server và GHI VÀO máy chấm công đang kết nối?\n\nThao tác này sẽ thêm dữ liệu sinh trắc học cho các nhân viên chưa có trên máy hiện tại.",
                "Xác nhận đồng bộ chéo", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            btnUploadBio.Enabled = false;
            btnUploadBio.Text = "⏳ Đang ghi...";
            lblBioStatus.Text = "Đang tải dữ liệu từ server...";

            try
            {
                // Bước 1: Tải danh sách mẫu từ Server
                var templates = await GetBiometricFromServerAsync();
                if (templates == null || templates.Count == 0)
                {
                    lblBioStatus.Text = "Server chưa có dữ liệu sinh trắc học nào.";
                    return;
                }

                lblBioStatus.Text = $"Đã tải {templates.Count} mẫu. Đang ghi vào máy chấm công...";
                int successCount = 0, failCount = 0;

                // Bước 2: Ghi từng mẫu vào máy
                foreach (var tpl in templates)
                {
                    bool ok;
                    if (tpl.TemplateType == "Fingerprint")
                    {
                        ok = _deviceService.SetFingerprint(tpl.EnrollNumber, tpl.FingerIndex, tpl.TemplateData, tpl.TemplateLength);
                    }
                    else // Face
                    {
                        ok = _deviceService.SetFaceTemplate(tpl.EnrollNumber, tpl.TemplateData, tpl.TemplateLength);
                    }

                    if (ok) successCount++; else failCount++;
                }

                // Bước 3: Làm mới bộ nhớ trên máy
                _deviceService.RefreshDeviceData();

                lblBioStatus.Text = $"✅ Hoàn tất: {successCount} mẫu ghi thành công, {failCount} thất bại.";
            }
            catch (Exception ex)
            {
                lblBioStatus.Text = $"❌ Lỗi: {ex.Message}";
            }
            finally
            {
                btnUploadBio.Enabled = true;
                btnUploadBio.Text = "📤 Đẩy từ Server → Máy";
            }
        }

        #endregion

        #region ═══════════════ GỌI API BACKEND ═══════════════

        /// <summary>Gửi log chấm công lên API SyncBulkDataAsync</summary>
        private async Task<int> SendAttendanceToServerAsync(List<SyncAttendanceClientDto> logs)
        {
            return await PostToApiAsync("/api/app/attendance/sync-bulk-data", logs);
        }

        /// <summary>Gửi dữ liệu sinh trắc học lên API UploadTemplatesAsync</summary>
        private async Task<int> SendBiometricToServerAsync(List<BiometricTemplateClientDto> templates)
        {
            // Map Client DTO → Server DTO format
            var serverDtos = templates.Select(t => new
            {
                enrollNumber = t.EnrollNumber,
                templateType = t.TemplateType,
                fingerIndex = t.FingerIndex,
                templateData = t.TemplateData,
                templateLength = t.TemplateLength,
                sourceDeviceSerial = t.SourceDeviceSerial
            }).ToList();

            return await PostToApiAsync("/api/app/biometric/upload-templates", serverDtos);
        }

        /// <summary>Tải danh sách mẫu sinh trắc học từ Server</summary>
        private async Task<List<BiometricTemplateClientDto>?> GetBiometricFromServerAsync()
        {
            try
            {
                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (s, c, ch, ssl) => true;
                using var client = new HttpClient(handler) { BaseAddress = new Uri(API_BASE_URL) };
                if (!string.IsNullOrEmpty(_userToken))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

                var response = await client.GetAsync("/api/app/biometric/all-templates");
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<BiometricTemplateClientDto>>(json, options);
            }
            catch (Exception ex)
            {
                OnDeviceLogReceived($"[{DateTime.Now:HH:mm:ss}] ❌ Lỗi tải từ server: {ex.Message}");
                return null;
            }
        }

        /// <summary>Helper gọi POST API chung, trả về số nguyên từ response</summary>
        private async Task<int> PostToApiAsync<T>(string endpoint, T data)
        {
            try
            {
                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (s, c, ch, ssl) => true;
                using var client = new HttpClient(handler) { BaseAddress = new Uri(API_BASE_URL) };
                if (!string.IsNullOrEmpty(_userToken))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return int.TryParse(result, out int count) ? count : 0;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    OnDeviceLogReceived($"[{DateTime.Now:HH:mm:ss}] ❌ API {response.StatusCode}: {error}");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                OnDeviceLogReceived($"[{DateTime.Now:HH:mm:ss}] ❌ Lỗi kết nối: {ex.Message}");
                return -1;
            }
        }

        #endregion

        #region ═══════════════ HELPERS ═══════════════

        private void AddSyncLogEntry(string action, string records, string result, string detail)
        {
            dgvSyncLog.Rows.Insert(0, DateTime.Now.ToString("dd/MM HH:mm:ss"), action, records, result, detail);
            while (dgvSyncLog.Rows.Count > 100) dgvSyncLog.Rows.RemoveAt(dgvSyncLog.Rows.Count - 1);
        }

        private void OnDeviceLogReceived(string message)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => OnDeviceLogReceived(message))); return; }
            lblSyncStatus.Text = message;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _pingTimer?.Stop(); _pingTimer?.Dispose();
            _deviceService.Dispose();
        }

        #endregion
    }
}
