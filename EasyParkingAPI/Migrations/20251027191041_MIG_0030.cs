using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyParkingAPI.Migrations
{
    /// <inheritdoc />
    public partial class MIG_0030 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataVehiculoAlojados");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Tarifas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Tarifas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DataVehiculoAlojados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CantidadActualAlojados = table.Column<int>(type: "int", nullable: false),
                    CapacidadDeAlojamiento = table.Column<int>(type: "int", nullable: false),
                    EstacionamientoId = table.Column<int>(type: "int", nullable: false),
                    Tarifa_Dia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarifa_Hora = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarifa_Mes = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tarifa_Semana = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TipoDeVehiculo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataVehiculoAlojados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataVehiculoAlojados_Estacionamientos_EstacionamientoId",
                        column: x => x.EstacionamientoId,
                        principalTable: "Estacionamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataVehiculoAlojados_EstacionamientoId",
                table: "DataVehiculoAlojados",
                column: "EstacionamientoId");
        }
    }
}
