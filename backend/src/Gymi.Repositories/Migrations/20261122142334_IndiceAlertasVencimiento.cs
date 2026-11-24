using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class IndiceAlertasVencimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Membresias_FechaVencimiento_ClienteId",
                table: "Membresias",
                columns: new[] { "FechaVencimiento", "ClienteId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Membresias_FechaVencimiento_ClienteId",
                table: "Membresias");
        }
    }
}
