using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatePass.MS.ClientApp.Migrations
{
    /// <inheritdoc />
    public partial class addedapprovedtime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDateTimeEnd",
                table: "RequestInformation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDateTimeStart",
                table: "RequestInformation",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedDateTimeEnd",
                table: "RequestInformation");

            migrationBuilder.DropColumn(
                name: "ApprovedDateTimeStart",
                table: "RequestInformation");
        }
    }
}
