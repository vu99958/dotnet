using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;

namespace QuanLyNhanSu.DesktopClient
{
    public partial class FormDashboard : Form
    {
        private string userToken; 
        private string myCurrentRole = "user";
        private string? currentEditUserId = null; 

        // CÁC BIẾN UI CHUNG
        private Panel pnlDashboard = null!, pnlProfile = null!, pnlManageContent = null!, pnlAddEditEmployee = null!;
        private Button btnManageEmp = null!;
        private Button btnMenuAttendance = null!;
        private Button btnPayroll = null!;
        private Button btnBranchManager = null!;
        private Button btnMonthlyReport = null!;
        private TextBox txtEmpUserName = null!, txtEmpEmail = null!, txtEmpPhone = null!, txtEmpPassword = null!;
        private TextBox txtSearchEmp = null!;
        private Label lblEmpPassword = null!, lblAddEditTitle = null!;
        private ComboBox cbEmpRole = null!;
        private ComboBox cmbBranch = null!;
        private Button btnSaveEmp = null!, btnDeleteEmp = null!, btnIssueKey = null!;
        
        private TextBox txtEditEmail = null!, txtEditPhone = null!, txtEditAddress = null!, txtEditBranch = null!;
        private Label lblProName = null!, lblProRole = null!, lblProDate = null!;
        private Button btnEditProfile = null!;
        private DataGridView dgvEmployees = null!;
        private bool isEditMode = false;
        
        // CÁC BIẾN CHO SIDEBAR & HEADER
        private Panel pnlSidebar = null!, pnlHeader = null!, pnlMainContent = null!;
        private FlowLayoutPanel flpSummary = null!;
        private Button btnViewProfile = null!, btnLeaveManagement = null!, btnLogoutDash = null!;
        private Label lblHeaderTitle = null!, lblHeaderTime = null!;
        private System.Windows.Forms.Timer timerHeader = null!;

        // CÁC BIẾN CHO THỐNG KÊ DASHBOARD
        private Label lblTotalEmp = null!, lblTotalAdmin = null!, lblTotalUser = null!;
        private Panel pnlStat1 = null!, pnlStat2 = null!, pnlStat3 = null!;

        public FormDashboard(string token)
        {
            userToken = token; 
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "Bảng Điều Khiển - Premium";
            this.Size = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 10F);

            this.FormClosed += (s, e) => Application.Exit();
            
            // Hàm vẽ Dashboard nằm trong file này
            VeGiaoDienDashboard();
            // Hàm vẽ Chấm Công sẽ được gọi từ file FormDashboard.Attendance.cs
            VeGiaoDienChamCong();
            // Hàm vẽ Biểu đồ thống kê (Pie + Column) từ file FormDashboard.Charts.cs
            VeGiaoDienBieuDo();
            // Hàm vẽ Báo cáo Tổng hợp Tháng từ file FormDashboard.MonthlyReport.cs
            VeGiaoDienBaoCaoThang();
            
            // Refactor UI Layout
            InitializeCustomUI();
            
            this.Load += FormDashboard_Load; 
        }

        private void VeGiaoDienDashboard()
        {
            Color primaryBlue = Color.FromArgb(0, 102, 204), primaryGreen = Color.FromArgb(32, 161, 68);
            Color primaryOrange = Color.FromArgb(255, 140, 0), warningYellow = Color.FromArgb(255, 193, 7);
            Color lightGray = Color.FromArgb(245, 247, 250), darkGray = Color.FromArgb(80, 80, 80), dangerRed = Color.FromArgb(220, 53, 69);
            int startX = 40, width = 400;

            // 1. DASHBOARD
            pnlDashboard = new Panel { BackColor = Color.FromArgb(240, 242, 245), Visible = true };
            
            // Summary Cards — sẽ được đưa vào FlowLayoutPanel trong InitializeCustomUI
            pnlStat1 = new Panel { BackColor = primaryBlue, Width = 220, Height = 80, Margin = new Padding(10), BorderStyle = BorderStyle.None, Visible = false };
            pnlStat1.Controls.Add(new Label { Text = "TỔNG NHÂN SỰ", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 10), AutoSize = true });
            lblTotalEmp = new Label { Text = "-", Font = new Font("Segoe UI", 24F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 35), AutoSize = true };
            pnlStat1.Controls.Add(lblTotalEmp);

            pnlStat2 = new Panel { BackColor = warningYellow, Width = 220, Height = 80, Margin = new Padding(10), BorderStyle = BorderStyle.None, Visible = false };
            pnlStat2.Controls.Add(new Label { Text = "QUẢN TRỊ VIÊN", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 10), AutoSize = true });
            lblTotalAdmin = new Label { Text = "-", Font = new Font("Segoe UI", 24F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 35), AutoSize = true };
            pnlStat2.Controls.Add(lblTotalAdmin);

            pnlStat3 = new Panel { BackColor = primaryGreen, Width = 220, Height = 80, Margin = new Padding(10), BorderStyle = BorderStyle.None, Visible = false };
            pnlStat3.Controls.Add(new Label { Text = "NHÂN VIÊN", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 10), AutoSize = true });
            lblTotalUser = new Label { Text = "-", Font = new Font("Segoe UI", 24F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(10, 35), AutoSize = true };
            pnlStat3.Controls.Add(lblTotalUser);

            EventHandler clickTongSo = async (s, e) => { SwitchPanel(pnlManageContent); txtSearchEmp.Text = ""; await LoadEmployeeListAsync(); };
            pnlStat1.Cursor = Cursors.Hand; pnlStat1.Click += clickTongSo; foreach (Control c in pnlStat1.Controls) { c.Cursor = Cursors.Hand; c.Click += clickTongSo; }

            EventHandler clickQuanTri = async (s, e) => { SwitchPanel(pnlManageContent); await LoadEmployeeListAsync(); txtSearchEmp.Text = "admin"; };
            pnlStat2.Cursor = Cursors.Hand; pnlStat2.Click += clickQuanTri; foreach (Control c in pnlStat2.Controls) { c.Cursor = Cursors.Hand; c.Click += clickQuanTri; }

            EventHandler clickNhanVien = async (s, e) => { SwitchPanel(pnlManageContent); await LoadEmployeeListAsync(); txtSearchEmp.Text = "user"; };
            pnlStat3.Cursor = Cursors.Hand; pnlStat3.Click += clickNhanVien; foreach (Control c in pnlStat3.Controls) { c.Cursor = Cursors.Hand; c.Click += clickNhanVien; }

            btnViewProfile = new Button { Text = "👤 Hồ sơ của tôi" };
            btnViewProfile.Click += async (s, e) => { SwitchPanel(pnlProfile); await LoadMyProfileAsync(); };

            btnMenuAttendance = new Button { Text = "⏰ Chấm công" };
            btnMenuAttendance.Click += async (s, e) => { SwitchPanel(pnlAttendance); await LoadAttendanceDataAsync(DateTime.Now.Date); };

            btnManageEmp = new Button { Text = "👥 Quản lý nhân sự", Visible = false };
            btnManageEmp.Click += async (s, e) => { SwitchPanel(pnlManageContent); await LoadEmployeeListAsync(); };

            btnLeaveManagement = new Button { Text = "📝 Nghỉ phép" };
            btnLeaveManagement.Click += (s, e) => { FormLeaveRequest frm = new FormLeaveRequest(userToken); frm.ShowDialog(); };

            btnPayroll = new Button { Text = "💰 Tính lương" };
            btnPayroll.Click += (s, e) => { FormPayroll frm = new FormPayroll(userToken, myCurrentRole); frm.ShowDialog(); };

            btnBranchManager = new Button { Text = "🏢 Quản lý Chi Nhánh", Visible = false };
            btnBranchManager.Click += (s, e) => { new FormBranchManager(userToken).ShowDialog(); };

            btnMonthlyReport = new Button { Text = "📊 Báo cáo tháng", Visible = false };
            btnMonthlyReport.Click += async (s, e) => { SwitchPanel(pnlMonthlyReport); await LoadMonthlyReportAsync(); };

            btnLogoutDash = new Button { Text = "Đăng xuất" };
            btnLogoutDash.Click += (s, e) => { Application.Restart(); };            // 2. PROFILE CARD
            pnlProfile = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false, AutoScroll = true, Padding = new Padding(30, 20, 30, 20) };
            Label lblProfileTitle = new Label { Text = "THẺ NHÂN VIÊN", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = primaryGreen, Dock = DockStyle.Top, Height = 50, TextAlign = ContentAlignment.MiddleLeft };
            Panel pnlCard = new Panel { Dock = DockStyle.Top, Height = 400, BackColor = Color.White, Padding = new Padding(30), Margin = new Padding(0, 10, 0, 10) };
            lblProName = new Label { Text = "Đang tải...", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = darkGray, Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleLeft };
            lblProRole = new Label { Text = "ROLE", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryOrange, AutoSize = true, Padding = new Padding(8, 4, 8, 4) };
            Panel pnlRoleWrap = new Panel { Dock = DockStyle.Top, Height = 40 };
            pnlRoleWrap.Controls.Add(lblProRole);

            Panel pnlFieldEmail = new Panel { Dock = DockStyle.Top, Height = 36 };
            pnlFieldEmail.Controls.Add(new Label { Text = "✉️ Email:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Dock = DockStyle.Left, Width = 100, TextAlign = ContentAlignment.MiddleLeft });
            txtEditEmail = new TextBox { Font = new Font("Segoe UI", 11F), Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White };
            pnlFieldEmail.Controls.Add(txtEditEmail);

            Panel pnlFieldPhone = new Panel { Dock = DockStyle.Top, Height = 36 };
            pnlFieldPhone.Controls.Add(new Label { Text = "📞 SĐT:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Dock = DockStyle.Left, Width = 100, TextAlign = ContentAlignment.MiddleLeft });
            txtEditPhone = new TextBox { Text = "Chưa cập nhật", Font = new Font("Segoe UI", 11F), Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White };
            pnlFieldPhone.Controls.Add(txtEditPhone);

            Panel pnlFieldAddr = new Panel { Dock = DockStyle.Top, Height = 36 };
            pnlFieldAddr.Controls.Add(new Label { Text = "📍 Địa chỉ:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Dock = DockStyle.Left, Width = 100, TextAlign = ContentAlignment.MiddleLeft });
            txtEditAddress = new TextBox { Text = "Chưa cập nhật", Font = new Font("Segoe UI", 11F), Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White };
            pnlFieldAddr.Controls.Add(txtEditAddress);

            Panel pnlFieldBranch = new Panel { Dock = DockStyle.Top, Height = 36 };
            pnlFieldBranch.Controls.Add(new Label { Text = "🏢 Chi nhánh:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Dock = DockStyle.Left, Width = 100, TextAlign = ContentAlignment.MiddleLeft });
            txtEditBranch = new TextBox { Text = "Đang tải...", Font = new Font("Segoe UI", 11F), Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White };
            pnlFieldBranch.Controls.Add(txtEditBranch);

            lblProDate = new Label { Text = "Tham gia: ...", Font = new Font("Segoe UI", 10F, FontStyle.Italic), ForeColor = Color.Gray, Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleLeft };
            btnEditProfile = new Button { Text = "✍️ CHỈNH SỬA HỒ SƠ", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Dock = DockStyle.Top, Height = 40, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnEditProfile.FlatAppearance.BorderSize = 0; btnEditProfile.Click += BtnEditProfile_Click; 
            // Add controls bottom-to-top because Dock.Top stacks in reverse add order
            pnlCard.Controls.Add(btnEditProfile);
            pnlCard.Controls.Add(lblProDate);
            pnlCard.Controls.Add(pnlFieldBranch);
            pnlCard.Controls.Add(pnlFieldAddr);
            pnlCard.Controls.Add(pnlFieldPhone);
            pnlCard.Controls.Add(pnlFieldEmail);
            pnlCard.Controls.Add(pnlRoleWrap);
            pnlCard.Controls.Add(lblProName);
            pnlProfile.Controls.Add(pnlCard);
            pnlProfile.Controls.Add(lblProfileTitle);

            // 3. QUẢN LÝ NHÂN SỰ
            pnlManageContent = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false, Padding = new Padding(30, 20, 30, 20) };
            Label lblManageTitle = new Label { Text = "DANH SÁCH NHÂN VIÊN", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = primaryOrange, Dock = DockStyle.Top, Height = 50, TextAlign = ContentAlignment.MiddleLeft };
            
            txtSearchEmp = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 12F), PlaceholderText = "🔍 Nhập Tên, Email hoặc SĐT để tìm nhanh...", Height = 35 };
            txtSearchEmp.TextChanged += TxtSearchEmp_TextChanged;

            dgvEmployees = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowTemplate = { Height = 35 } };
            dgvEmployees.Columns.Add("Id", "ID"); dgvEmployees.Columns["Id"].Visible = false; 
            dgvEmployees.Columns.Add("BranchId", "BranchId"); dgvEmployees.Columns["BranchId"].Visible = false; 
            dgvEmployees.Columns.Add("Role", "Role"); dgvEmployees.Columns["Role"].Visible = false;
            dgvEmployees.Columns.Add("UserName", "TÀI KHOẢN"); dgvEmployees.Columns.Add("Email", "EMAIL"); dgvEmployees.Columns.Add("PhoneNumber", "SĐT"); dgvEmployees.Columns.Add("BranchName", "CHI NHÁNH");
            dgvEmployees.CellDoubleClick += DgvEmployees_CellDoubleClick; 
            
            Panel pnlManageBottom = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            Button btnAddEmp = new Button { Text = "➕ THÊM NHÂN VIÊN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnAddEmp.FlatAppearance.BorderSize = 0; btnAddEmp.Click += BtnAddEmp_Click;
            pnlManageBottom.Controls.Add(btnAddEmp);
            
            pnlManageContent.Controls.Add(dgvEmployees);
            pnlManageContent.Controls.Add(txtSearchEmp);
            pnlManageContent.Controls.Add(pnlManageBottom);
            pnlManageContent.Controls.Add(lblManageTitle);

            // 4. KHUNG THÊM / SỬA NHÂN VIÊN
            pnlAddEditEmployee = new Panel { Dock = DockStyle.Fill, BackColor = lightGray, Visible = false, AutoScroll = true, Padding = new Padding(30, 20, 30, 20) };
            lblAddEditTitle = new Label { Text = "THÔNG TIN CHI TIẾT", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = primaryOrange, Dock = DockStyle.Top, Height = 50, TextAlign = ContentAlignment.MiddleLeft };
            Panel pnlFormEdit = new Panel { Dock = DockStyle.Top, Height = 280, BackColor = Color.White, Padding = new Padding(20) };
            int formY = 15;
            pnlFormEdit.Controls.Add(new Label { Text = "Tài khoản:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(15, formY), AutoSize = true });
            txtEmpUserName = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(115, formY - 2), Width = 350, BorderStyle = BorderStyle.FixedSingle };
            pnlFormEdit.Controls.Add(new Label { Text = "Email:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(15, formY += 45), AutoSize = true });
            txtEmpEmail = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(115, formY - 2), Width = 350, BorderStyle = BorderStyle.FixedSingle };
            pnlFormEdit.Controls.Add(new Label { Text = "Số ĐT:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(15, formY += 45), AutoSize = true });
            txtEmpPhone = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(115, formY - 2), Width = 350, BorderStyle = BorderStyle.FixedSingle };
            lblEmpPassword = new Label { Text = "Mật khẩu:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(15, formY += 45), AutoSize = true };
            txtEmpPassword = new TextBox { Font = new Font("Segoe UI", 11F), Location = new Point(115, formY - 2), Width = 350, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };
            pnlFormEdit.Controls.Add(new Label { Text = "Quyền:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(15, formY += 45), AutoSize = true });
            cbEmpRole = new ComboBox { Font = new Font("Segoe UI", 11F), Location = new Point(115, formY - 2), Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            pnlFormEdit.Controls.Add(new Label { Text = "Chi nhánh:", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = darkGray, Location = new Point(15, formY += 45), AutoSize = true });
            cmbBranch = new ComboBox { Font = new Font("Segoe UI", 11F), Location = new Point(115, formY - 2), Width = 350, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };
            Label lblHint = new Label { Text = "*Mật khẩu phải có hoa, thường, số, ký tự đặc biệt", Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = Color.Gray, Location = new Point(115, formY + 35), Width = 350, Height= 20 };
            pnlFormEdit.Controls.AddRange(new Control[] { txtEmpUserName, txtEmpEmail, txtEmpPhone, lblEmpPassword, txtEmpPassword, cbEmpRole, cmbBranch, lblHint });

            Panel pnlEditButtons = new Panel { Dock = DockStyle.Top, Height = 110, Padding = new Padding(0, 10, 0, 0) };
            btnSaveEmp = new Button { Text = "💾 LƯU THÔNG TIN", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = primaryGreen, Dock = DockStyle.Top, Height = 45, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSaveEmp.FlatAppearance.BorderSize = 0; btnSaveEmp.Click += BtnSaveEmp_Click;
            Panel pnlEditRow2 = new Panel { Dock = DockStyle.Top, Height = 45 };
            btnIssueKey = new Button { Text = "🔑 CẤP LẠI KEY", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = warningYellow, Dock = DockStyle.Left, Width = 250, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnIssueKey.FlatAppearance.BorderSize = 0; btnIssueKey.Click += BtnIssueKey_Click;
            btnDeleteEmp = new Button { Text = "🗑️ XÓA", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = dangerRed, Dock = DockStyle.Left, Width = 250, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnDeleteEmp.FlatAppearance.BorderSize = 0; btnDeleteEmp.Click += BtnDeleteEmp_Click;
            pnlEditRow2.Controls.Add(btnDeleteEmp);
            pnlEditRow2.Controls.Add(btnIssueKey);
            Button btnCancelEdit = new Button { Text = "❌ HỦY BỎ / QUAY LẠI", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = darkGray, BackColor = Color.LightGray, Dock = DockStyle.Bottom, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCancelEdit.FlatAppearance.BorderSize = 0; btnCancelEdit.Click += (s, e) => { SwitchPanel(pnlManageContent); };
            pnlEditButtons.Controls.Add(pnlEditRow2);
            pnlEditButtons.Controls.Add(btnSaveEmp);

            pnlAddEditEmployee.Controls.Add(btnCancelEdit);
            pnlAddEditEmployee.Controls.Add(pnlEditButtons);
            pnlAddEditEmployee.Controls.Add(pnlFormEdit);
            pnlAddEditEmployee.Controls.Add(lblAddEditTitle);

            this.Controls.Add(pnlAddEditEmployee); 
            this.Controls.Add(pnlManageContent); 
            this.Controls.Add(pnlProfile); 
        }

        private void InitializeCustomUI()
        {
            // BƯỚC 2: QUY HOẠCH SIDEBAR
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = Color.FromArgb(41, 53, 65)
            };

            Panel pnlLogo = new Panel { Dock = DockStyle.Top, Height = 80 };
            Label lblLogo = new Label
            {
                Text = "HỆ THỐNG HR",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlLogo.Controls.Add(lblLogo);
            pnlSidebar.Controls.Add(pnlLogo);

            // Format các nút menu
            Button[] menuButtons = { btnLogoutDash, btnMonthlyReport, btnBranchManager, btnPayroll, btnLeaveManagement, btnManageEmp, btnMenuAttendance, btnViewProfile };
            
            // Re-order buttons for docking. Since they dock Top, adding them in reverse logical order is needed if we iterate, 
            // but actually let's just configure them and add them directly.
            foreach (var btn in menuButtons)
            {
                btn.Dock = (btn == btnLogoutDash) ? DockStyle.Bottom : DockStyle.Top;
                btn.Height = 50;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.ForeColor = Color.White;
                btn.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Padding = new Padding(20, 0, 0, 0);
                btn.Cursor = Cursors.Hand;
            }
            
            // Tắt nút đăng xuất về màu đỏ nhạt để nổi bật
            btnLogoutDash.BackColor = Color.FromArgb(220, 53, 69);
            btnLogoutDash.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 35, 51);

            // Add to sidebar (must be in correct visual order top to bottom: Profile -> Attendance -> Manage -> Leave -> Payroll)
            pnlSidebar.Controls.Add(btnMonthlyReport);
            pnlSidebar.Controls.Add(btnBranchManager);
            pnlSidebar.Controls.Add(btnPayroll);
            pnlSidebar.Controls.Add(btnLeaveManagement);
            pnlSidebar.Controls.Add(btnManageEmp);
            pnlSidebar.Controls.Add(btnMenuAttendance);
            pnlSidebar.Controls.Add(btnViewProfile);
            pnlSidebar.Controls.Add(btnLogoutDash);

            // FIX 1: Đảm bảo Logo luôn nằm trên cùng Sidebar
            pnlLogo.BringToFront();

            // BƯỚC 3: QUY HOẠCH HEADER
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White
            };

            lblHeaderTitle = new Label
            {
                Text = "Xin chào, đang tải...",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            lblHeaderTime = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblHeaderTime.Location = new Point(pnlHeader.Width - 250, 20);

            timerHeader = new System.Windows.Forms.Timer { Interval = 1000 };
            timerHeader.Tick += (s, e) => { lblHeaderTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); };
            timerHeader.Start();

            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(lblHeaderTime);

            // BƯỚC 4: QUY HOẠCH MAIN CONTENT
            pnlMainContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 242, 245)
            };

            // FIX 2: FlowLayoutPanel cho 3 thẻ tóm tắt — nằm trong pnlDashboard
            flpSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 100,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(240, 242, 245),
                WrapContents = false
            };
            flpSummary.Controls.Add(pnlStat1);
            flpSummary.Controls.Add(pnlStat2);
            flpSummary.Controls.Add(pnlStat3);

            // Setup các panel nội dung cũ thành Dock.Fill
            pnlDashboard.Dock = DockStyle.Fill;
            pnlProfile.Dock = DockStyle.Fill;
            pnlManageContent.Dock = DockStyle.Fill;
            pnlAddEditEmployee.Dock = DockStyle.Fill;
            if (pnlAttendance != null) pnlAttendance.Dock = DockStyle.Fill;
            if (pnlMonthlyReport != null) pnlMonthlyReport.Dock = DockStyle.Fill;

            // FIX 3: Chỉnh lại Chart — đảm bảo chart được Add vào Panel đúng cách
            if (pnlCharts != null)
            {
                pnlCharts.Dock = DockStyle.Fill;
                pnlCharts.BackColor = Color.FromArgb(240, 242, 245);

                // Tạo 2 Panel trắng chứa Chart, đặt trong TableLayoutPanel 50-50
                TableLayoutPanel tableCharts = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    BackColor = Color.Transparent,
                    Padding = new Padding(10)
                };
                tableCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                tableCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                tableCharts.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                // Panel trắng bên trái chứa Pie Chart
                Panel pnlChartLeft = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Margin = new Padding(10),
                    Padding = new Padding(5)
                };
                chartAttendance.Dock = DockStyle.Fill;
                pnlChartLeft.Controls.Add(chartAttendance);

                // Panel trắng bên phải chứa Column Chart
                Panel pnlChartRight = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Margin = new Padding(10),
                    Padding = new Padding(5)
                };
                chartSalary.Dock = DockStyle.Fill;
                pnlChartRight.Controls.Add(chartSalary);

                tableCharts.Controls.Add(pnlChartLeft, 0, 0);
                tableCharts.Controls.Add(pnlChartRight, 1, 0);

                pnlCharts.Controls.Clear();
                pnlCharts.Controls.Add(tableCharts);
            }

            // Gắn pnlCharts + flpSummary vào pnlDashboard
            pnlDashboard.Controls.Add(pnlCharts);
            pnlDashboard.Controls.Add(flpSummary);

            // Gắn mọi thứ vào pnlMainContent
            pnlMainContent.Controls.Add(pnlAddEditEmployee);
            pnlMainContent.Controls.Add(pnlManageContent);
            if (pnlAttendance != null) pnlMainContent.Controls.Add(pnlAttendance);
            if (pnlMonthlyReport != null) pnlMainContent.Controls.Add(pnlMonthlyReport);
            pnlMainContent.Controls.Add(pnlProfile);
            pnlMainContent.Controls.Add(pnlDashboard);

            this.Controls.Add(pnlMainContent);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlSidebar);

            // Đưa form về trang chính
            SwitchPanel(pnlDashboard);
        }

        private void SwitchPanel(Panel target)
        {
            pnlDashboard.Visible = false; pnlProfile.Visible = false; pnlManageContent.Visible = false; pnlAddEditEmployee.Visible = false;
            if(pnlAttendance != null) pnlAttendance.Visible = false;
            if(pnlMonthlyReport != null) pnlMonthlyReport.Visible = false;
            target.Visible = true;
            target.BringToFront();
        }

        private async void FormDashboard_Load(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(userToken)) return;
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (s, c, ch, ssl) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
                    
                    HttpResponseMessage response = await client.GetAsync("https://localhost:44387/api/app/my-profile/my-profile");
                    if (response.IsSuccessStatusCode)
                    {
                        var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                        myCurrentRole = data.GetProperty("roles").GetString()?.ToLower() ?? "user";
                        
                        if (myCurrentRole == "admin" || myCurrentRole == "superadmin") 
                        {
                            btnManageEmp.Visible = true;
                            pnlStat1.Visible = true;
                            pnlStat2.Visible = true;
                            pnlStat3.Visible = true;

                            // Hiện biểu đồ thống kê và tải dữ liệu
                            if (pnlCharts != null) pnlCharts.Visible = true;
                            await LoadDashboardChartsAsync();

                            // Hiện nút Quản lý Chi Nhánh cho Admin
                            btnBranchManager.Visible = true;

                            // Hiện nút Báo cáo Tổng hợp Tháng cho Admin
                            btnMonthlyReport.Visible = true;
                        }  
                        else
                        {
                            btnPayroll.Text = "💰 Phiếu lương";
                            // Ẩn biểu đồ cho User thường
                            if (pnlCharts != null) pnlCharts.Visible = false;
                        }
                        
                        // Cập nhật câu chào
                        string roleText = (myCurrentRole == "admin" || myCurrentRole == "superadmin") ? "Quản trị viên" : "Nhân viên";
                        lblHeaderTitle.Text = $"Xin chào, {roleText}";
                    }

                    HttpResponseMessage statsRes = await client.GetAsync("https://localhost:44387/api/app/employee/dashboard-stats");
                    if (statsRes.IsSuccessStatusCode)
                    {
                        var statsData = JsonSerializer.Deserialize<JsonElement>(await statsRes.Content.ReadAsStringAsync());
                        lblTotalEmp.Text = statsData.GetProperty("totalEmployees").GetInt32().ToString();
                        lblTotalAdmin.Text = statsData.GetProperty("totalAdmins").GetInt32().ToString();
                        lblTotalUser.Text = statsData.GetProperty("totalUsers").GetInt32().ToString();
                    }
                }
            } catch { }
        }
    }
}