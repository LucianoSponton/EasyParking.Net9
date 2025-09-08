using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyParkingAPI.Migrations
{
    /// <inheritdoc />
    public partial class MIG_0016 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TiempoDeEsperaEnMinutos",
                table: "Reservas");

            migrationBuilder.AddColumn<int>(
                name: "TiempoDeEsperaEnMinutos",
                table: "Estacionamientos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TiempoDeEsperaEnMinutos",
                table: "Estacionamientos");

            migrationBuilder.AddColumn<int>(
                name: "TiempoDeEsperaEnMinutos",
                table: "Reservas",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
