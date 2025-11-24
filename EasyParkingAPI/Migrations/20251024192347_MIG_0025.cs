using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyParkingAPI.Migrations
{
    /// <inheritdoc />
    public partial class MIG_0025 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloqueoPlazas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstacionamientoId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlazaId = table.Column<int>(type: "int", nullable: true),
                    TipoDeVehiculo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaDeCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueoPlazas", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloqueoPlazas");
        }
    }
}
