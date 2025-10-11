using Microsoft.EntityFrameworkCore.Migrations;

namespace EasyParkingAPI.Migrations
{
    public partial class _0001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Estacionamientos",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(nullable: true),
                    PublicacionPausada = table.Column<bool>(nullable: false),
                    Inactivo = table.Column<bool>(nullable: false),
                    Imagen = table.Column<byte[]>(nullable: true),
                    Ciudad = table.Column<string>(nullable: true),
                    Nombre = table.Column<string>(nullable: true),
                    Direccion = table.Column<string>(nullable: true),
                    Latitud = table.Column<double>(nullable: false),
                    Longitud = table.Column<double>(nullable: false),
                    TipoDeLugar = table.Column<string>(nullable: true),
                    MontoReserva = table.Column<decimal>(nullable: false),
                    Observaciones = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estacionamientos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Favoritos",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstacionamientoId = table.Column<int>(nullable: false),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favoritos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehiculos",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Patente = table.Column<string>(nullable: true),
                    TipoDeVehiculo = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehiculos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataVehiculoAlojados",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstacionamientoId = table.Column<int>(nullable: true),
                    TipoDeVehiculo = table.Column<string>(nullable: true),
                    CapacidadDeAlojamiento = table.Column<int>(nullable: false),
                    Tarifa_Hora = table.Column<decimal>(nullable: false),
                    Tarifa_Dia = table.Column<decimal>(nullable: false),
                    Tarifa_Semana = table.Column<decimal>(nullable: false),
                    Tarifa_Mes = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataVehiculoAlojados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataVehiculoAlojados_Estacionamientos_EstacionamientoId",
                        column: x => x.EstacionamientoId,
                        principalTable: "Estacionamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Jornadas",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstacionamientoId = table.Column<int>(nullable: true),
                    DiaDeLaSemana = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jornadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jornadas_Estacionamientos_EstacionamientoId",
                        column: x => x.EstacionamientoId,
                        principalTable: "Estacionamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RangoHs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JornadaId = table.Column<int>(nullable: true),
                    DesdeHora = table.Column<int>(nullable: false),
                    DesdeMinuto = table.Column<int>(nullable: false),
                    HastaHora = table.Column<int>(nullable: false),
                    HastaMinuto = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RangoHs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RangoHs_Jornadas_JornadaId",
                        column: x => x.JornadaId,
                        principalTable: "Jornadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataVehiculoAlojados_EstacionamientoId",
                table: "DataVehiculoAlojados",
                column: "EstacionamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_Jornadas_EstacionamientoId",
                table: "Jornadas",
                column: "EstacionamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_RangoHs_JornadaId",
                table: "RangoHs",
                column: "JornadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Patente",
                table: "Vehiculos",
                column: "Patente",
                unique: true,
                filter: "[Patente] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataVehiculoAlojados");

            migrationBuilder.DropTable(
                name: "Favoritos");

            migrationBuilder.DropTable(
                name: "RangoHs");

            migrationBuilder.DropTable(
                name: "Vehiculos");

            migrationBuilder.DropTable(
                name: "Jornadas");

            migrationBuilder.DropTable(
                name: "Estacionamientos");
        }
    }
}
