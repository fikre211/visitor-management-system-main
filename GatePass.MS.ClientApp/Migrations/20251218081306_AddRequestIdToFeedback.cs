using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatePass.MS.ClientApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestIdToFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestId",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestInformationId",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_RequestInformationId",
                table: "Feedback",
                column: "RequestInformationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedback_RequestInformation_RequestInformationId",
                table: "Feedback",
                column: "RequestInformationId",
                principalTable: "RequestInformation",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedback_RequestInformation_RequestInformationId",
                table: "Feedback");

            migrationBuilder.DropIndex(
                name: "IX_Feedback_RequestInformationId",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "RequestInformationId",
                table: "Feedback");
        }
    }
}
