using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace EasyParkingAPI.Migrations
{
    /// <inheritdoc />
    public partial class MIG_0008 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaDeArribo",
                table: "Reservas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaDeSalida",
                table: "Reservas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaDeArribo",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "FechaDeSalida",
                table: "Reservas");
        }
    }
}
