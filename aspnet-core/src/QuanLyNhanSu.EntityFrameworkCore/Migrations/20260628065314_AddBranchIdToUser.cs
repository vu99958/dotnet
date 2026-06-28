using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyNhanSu.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "UserKeys",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "UserKeys");
        }
    }
}
