using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatePass.MS.ClientApp.Migrations
{
    /// <inheritdoc />
    public partial class companyidisaddedtorequestinformtionstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestInformation_Company_CompanyId",
                table: "RequestInformation");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "RequestInformation",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestInformation_Company_CompanyId",
                table: "RequestInformation",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestInformation_Company_CompanyId",
                table: "RequestInformation");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyId",
                table: "RequestInformation",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestInformation_Company_CompanyId",
                table: "RequestInformation",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id");
        }
    }
}
