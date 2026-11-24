using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class IndicesReportes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Pagos_FechaPago",
                table: "Pagos",
                column: "FechaPago");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_FechaAlta",
                table: "Clientes",
                column: "FechaAlta");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_FechaHoraUtcTicks",
                table: "Asistencias",
                column: "FechaHoraUtcTicks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pagos_FechaPago",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_FechaAlta",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Asistencias_FechaHoraUtcTicks",
                table: "Asistencias");
        }
    }
}
