using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace EasyParkingAPI.Migrations
{
    public partial class _0002 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstacionamientoId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    VehiculoId = table.Column<int>(nullable: false),
                    Monto = table.Column<decimal>(nullable: false),
                    Patente = table.Column<string>(nullable: true),
                    CodigtoDeValidacion = table.Column<string>(nullable: true),
                    Estado = table.Column<string>(nullable: false),
                    FechaDeCreacion = table.Column<DateTime>(nullable: false),
                    FechaDeExpiracion = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservas");
        }
    }
}
