using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhanSu.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa dữ liệu trùng lặp trước khi tạo Unique Index
            migrationBuilder.Sql(@"
                WITH CTE AS (
                    SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [UserId], [WorkDate] ORDER BY (SELECT NULL)) as rn
                    FROM [AppAttendanceRecords]
                )
                DELETE FROM CTE WHERE rn > 1;

                WITH CTE AS (
                    SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [UserId] ORDER BY (SELECT NULL)) as rn
                    FROM [AppSalaryProfiles]
                )
                DELETE FROM CTE WHERE rn > 1;

                WITH CTE AS (
                    SELECT [Id], ROW_NUMBER() OVER (PARTITION BY [UserId], [Month], [Year] ORDER BY (SELECT NULL)) as rn
                    FROM [AppPayslips]
                )
                DELETE FROM CTE WHERE rn > 1;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AppLeaveRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "AppBiometricTemplates",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExtraProperties",
                table: "AppBiometricTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalaryProfiles_UserId",
                table: "AppSalaryProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppPayslips_UserId_Month_Year",
                table: "AppPayslips",
                columns: new[] { "UserId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLeaveRequests_UserId_Status",
                table: "AppLeaveRequests",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppAttendanceRecords_UserId_WorkDate",
                table: "AppAttendanceRecords",
                columns: new[] { "UserId", "WorkDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppSalaryProfiles_UserId",
                table: "AppSalaryProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AppPayslips_UserId_Month_Year",
                table: "AppPayslips");

            migrationBuilder.DropIndex(
                name: "IX_AppLeaveRequests_UserId_Status",
                table: "AppLeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppAttendanceRecords_UserId_WorkDate",
                table: "AppAttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "AppBiometricTemplates");

            migrationBuilder.DropColumn(
                name: "ExtraProperties",
                table: "AppBiometricTemplates");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AppLeaveRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
