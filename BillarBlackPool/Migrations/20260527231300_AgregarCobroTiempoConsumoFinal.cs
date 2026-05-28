using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillarBlackPool.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCobroTiempoConsumoFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoMesa",
                table: "Consumos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MinutosJugados",
                table: "Consumos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioHora",
                table: "Consumos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioLibrePorMinuto",
                table: "Consumos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioMediaHora",
                table: "Consumos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TipoCobro",
                table: "Consumos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "Consumos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalProductos",
                table: "Consumos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoMesa",
                table: "Consumos");

            migrationBuilder.DropColumn(
                name: "MinutosJugados",
                table: "Consumos");

            migrationBuilder.DropColumn(
                name: "PrecioHora",
                table: "Consumos");

            migrationBuilder.DropColumn(
                name: "PrecioLibrePorMinuto",
                table: "Consumos");

            migrationBuilder.DropColumn(
                name: "PrecioMediaHora",
                table: "Consumos");

            migrationBuilder.DropColumn(
                name: "TipoCobro",
                table: "Consumos");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "Consumos");

            migrationBuilder.DropColumn(
                name: "TotalProductos",
                table: "Consumos");
        }
    }
}
