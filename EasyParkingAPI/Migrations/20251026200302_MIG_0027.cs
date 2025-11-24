using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyParkingAPI.Migrations
{
    /// <inheritdoc />
    public partial class MIG_0027 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Tarifas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Tarifas");
        }
    }
}
