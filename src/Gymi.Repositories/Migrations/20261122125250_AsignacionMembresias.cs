using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AsignacionMembresias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Membresias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SesionesIncluidas = table.Column<int>(type: "INTEGER", nullable: false),
                    SesionesDisponibles = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Membresias", x => x.Id);
                    table.CheckConstraint("CK_Membresias_Cupo", "typeof(SesionesIncluidas) = 'integer' AND SesionesIncluidas BETWEEN 1 AND 2147483647");
                    table.CheckConstraint("CK_Membresias_Saldo", "typeof(SesionesDisponibles) = 'integer' AND SesionesDisponibles BETWEEN 0 AND SesionesIncluidas");
                    table.CheckConstraint("CK_Membresias_Vigencia", "FechaInicio < FechaVencimiento");
                    table.ForeignKey(
                        name: "FK_Membresias_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Membresias_Planes_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Planes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_ClienteId_FechaInicio_Id",
                table: "Membresias",
                columns: new[] { "ClienteId", "FechaInicio", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_PlanId",
                table: "Membresias",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Membresias");
        }
    }
}
