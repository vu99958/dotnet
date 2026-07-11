using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu.Domain;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace QuanLyNhanSu.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class QuanLyNhanSuDbContext :
    AbpDbContext<QuanLyNhanSuDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    //Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }
    
    // Application Entities
    public DbSet<UserKey> UserKeys { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; } //đại diện cho bảng chấm công
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<SalaryProfile> SalaryProfiles { get; set; }
    public DbSet<Payslip> Payslips { get; set; }
    public DbSet<PayslipComplaint> PayslipComplaints { get; set; }
    public DbSet<Branch> Branches { get; set; } // Bảng chi nhánh (Geofencing đa điểm)
    public DbSet<BiometricTemplate> BiometricTemplates { get; set; } // Bảng sinh trắc học (vân tay/khuôn mặt)

    #endregion

    public QuanLyNhanSuDbContext(DbContextOptions<QuanLyNhanSuDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */
        builder.Entity<AttendanceRecord>(b =>
        {
            b.ToTable("AppAttendanceRecords"); // Tên bảng sẽ tạo trong SQL Server
            b.ConfigureByConvention(); // Tự động cấu hình các cột chuẩn của ABP
            // DESIGN-02: Mỗi nhân viên chỉ có 1 bản ghi chấm công/ngày
            b.HasIndex(x => new { x.UserId, x.WorkDate }).IsUnique();
        });

        builder.Entity<LeaveRequest>(b =>
        {
            b.ToTable("AppLeaveRequests");
            b.ConfigureByConvention();
            // DESIGN-02: Tăng tốc query nghỉ phép theo user + trạng thái
            b.HasIndex(x => new { x.UserId, x.Status });
        });

        builder.Entity<SalaryProfile>(b =>
        {
            b.ToTable("AppSalaryProfiles");
            b.ConfigureByConvention();
            // DESIGN-02: Mỗi nhân viên chỉ có 1 cấu hình lương
            b.HasIndex(x => x.UserId).IsUnique();
        });

        builder.Entity<Payslip>(b =>
        {
            b.ToTable("AppPayslips");
            b.ConfigureByConvention();
            // DESIGN-02: Mỗi nhân viên chỉ có 1 phiếu lương/tháng
            b.HasIndex(x => new { x.UserId, x.Month, x.Year }).IsUnique();
        });

        builder.Entity<PayslipComplaint>(b =>
        {
            b.ToTable("AppPayslipComplaints");
            b.ConfigureByConvention();
        });

        builder.Entity<Branch>(b =>
        {
            b.ToTable("AppBranches"); // Bảng chi nhánh cho Geofencing
            b.ConfigureByConvention();
        });

        builder.Entity<BiometricTemplate>(b =>
        {
            b.ToTable("AppBiometricTemplates"); // Bảng sinh trắc học
            b.ConfigureByConvention();
            b.HasIndex(x => new { x.EnrollNumber, x.TemplateType, x.FingerIndex })
                .IsUnique(); // Chống trùng lặp composite key
        });

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        /* Configure your own tables/entities inside here */

        //builder.Entity<YourEntity>(b =>
        //{
        //    b.ToTable(QuanLyNhanSuConsts.DbTablePrefix + "YourEntities", QuanLyNhanSuConsts.DbSchema);
        //    b.ConfigureByConvention(); //auto configure for the base class props
        //    //...
        //});
    }
}
