using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatePass.MS.ClientApp.Migrations
{
    /// <inheritdoc />
    public partial class useractivitylogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "UserActivityLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_CompanyId",
                table: "UserActivityLogs",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserActivityLogs_Company_CompanyId",
                table: "UserActivityLogs",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserActivityLogs_Company_CompanyId",
                table: "UserActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_UserActivityLogs_CompanyId",
                table: "UserActivityLogs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "UserActivityLogs");
        }
    }
}
