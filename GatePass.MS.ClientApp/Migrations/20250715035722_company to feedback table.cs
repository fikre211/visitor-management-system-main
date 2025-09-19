using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatePass.MS.ClientApp.Migrations
{
    /// <inheritdoc />
    public partial class companytofeedbacktable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Feedback",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_CompanyId",
                table: "Feedback",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedback_Company_CompanyId",
                table: "Feedback",
                column: "CompanyId",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedback_Company_CompanyId",
                table: "Feedback");

            migrationBuilder.DropIndex(
                name: "IX_Feedback_CompanyId",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Feedback");
        }
    }
}
