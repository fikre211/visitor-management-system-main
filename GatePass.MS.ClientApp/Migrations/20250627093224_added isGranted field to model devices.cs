using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatePass.MS.ClientApp.Migrations
{
    /// <inheritdoc />
    public partial class addedisGrantedfieldtomodeldevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGranted",
                table: "Device",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGranted",
                table: "Device");
        }
    }
}
