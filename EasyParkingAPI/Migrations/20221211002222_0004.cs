using Microsoft.EntityFrameworkCore.Migrations;

namespace EasyParkingAPI.Migrations
{
    public partial class _0004 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigtoDeValidacion",
                table: "Reservas");

            migrationBuilder.AddColumn<string>(
                name: "CodigoDeValidacion",
                table: "Reservas",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoDeValidacion",
                table: "Reservas");

            migrationBuilder.AddColumn<string>(
                name: "CodigtoDeValidacion",
                table: "Reservas",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
