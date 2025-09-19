using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatePass.MS.ClientApp.Migrations
{
    /// <inheritdoc />
    public partial class addedisDeniedfieldtomodeldevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsGranted",
                table: "Device",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<bool>(
                name: "IsDenied",
                table: "Device",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDenied",
                table: "Device");

            migrationBuilder.AlterColumn<bool>(
                name: "IsGranted",
                table: "Device",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);
        }
    }
}
