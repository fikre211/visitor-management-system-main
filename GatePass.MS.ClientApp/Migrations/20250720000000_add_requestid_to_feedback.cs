using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatePass.MS.ClientApp.Migrations
{
    public partial class addrequestidtofeedback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestId",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_RequestId",
                table: "Feedback",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedback_RequestInformation_RequestId",
                table: "Feedback",
                column: "RequestId",
                principalTable: "RequestInformation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedback_RequestInformation_RequestId",
                table: "Feedback");

            migrationBuilder.DropIndex(
                name: "IX_Feedback_RequestId",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Feedback");
        }
    }
}
