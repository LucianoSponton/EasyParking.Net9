using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyParkingAPI.Migrations
{
    /// <inheritdoc />
    public partial class MIG_0026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Plazas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Tarifas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstacionamientoId = table.Column<int>(type: "int", nullable: false),
                    TipoDeVehiculo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tarifa_Hora = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarifa_Dia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarifa_Semana = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarifa_Mes = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tarifas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tarifas_Estacionamientos_EstacionamientoId",
                        column: x => x.EstacionamientoId,
                        principalTable: "Estacionamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tarifas_EstacionamientoId",
                table: "Tarifas",
                column: "EstacionamientoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tarifas");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Plazas");
        }
    }
}
