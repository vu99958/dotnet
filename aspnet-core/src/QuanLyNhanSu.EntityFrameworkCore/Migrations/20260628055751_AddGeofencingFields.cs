using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhanSu.Migrations
{
    /// <inheritdoc />
    public partial class AddGeofencingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "AppAttendanceRecords",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "AppAttendanceRecords",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "AppAttendanceRecords");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "AppAttendanceRecords");
        }
    }
}
