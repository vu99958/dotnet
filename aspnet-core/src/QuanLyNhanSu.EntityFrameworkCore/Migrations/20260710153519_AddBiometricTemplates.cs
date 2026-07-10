using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhanSu.Migrations
{
    /// <inheritdoc />
    public partial class AddBiometricTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppBiometricTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnrollNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TemplateType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FingerIndex = table.Column<int>(type: "int", nullable: false),
                    TemplateData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateLength = table.Column<int>(type: "int", nullable: false),
                    SourceDeviceSerial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBiometricTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppBiometricTemplates_EnrollNumber_TemplateType_FingerIndex",
                table: "AppBiometricTemplates",
                columns: new[] { "EnrollNumber", "TemplateType", "FingerIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBiometricTemplates");
        }
    }
}
