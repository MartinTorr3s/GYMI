using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarAsistencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Asistencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MembresiaId = table.Column<int>(type: "INTEGER", nullable: false),
                    OperacionId = table.Column<Guid>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    FechaHoraUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    SesionesRestantes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asistencias", x => x.Id);
                    table.CheckConstraint("CK_Asistencias_FechaHora", "typeof(FechaHoraUtcTicks) = 'integer' AND FechaHoraUtcTicks BETWEEN 0 AND 3155378975999999999");
                    table.CheckConstraint("CK_Asistencias_Operacion", "length(OperacionId) = 36 AND OperacionId <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("CK_Asistencias_SesionesRestantes", "typeof(SesionesRestantes) = 'integer' AND SesionesRestantes BETWEEN 0 AND 2147483647");
                    table.ForeignKey(
                        name: "FK_Asistencias_Membresias_MembresiaId",
                        column: x => x.MembresiaId,
                        principalTable: "Membresias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_MembresiaId",
                table: "Asistencias",
                column: "MembresiaId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_OperacionId",
                table: "Asistencias",
                column: "OperacionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asistencias");
        }
    }
}
