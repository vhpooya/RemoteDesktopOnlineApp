using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteDesktopOnlineApps.Migrations
{
    /// <inheritdoc />
    public partial class filetrancfe01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "FileTransfers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Chunks",
                table: "FileTransfers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "FileTransfers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransferId",
                table: "FileTransfers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Chunks",
                table: "FileTransfers");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "FileTransfers");

            migrationBuilder.DropColumn(
                name: "TransferId",
                table: "FileTransfers");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "FileTransfers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
