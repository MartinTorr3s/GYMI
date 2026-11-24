using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE Membresias ADD COLUMN DiaAnclaMensual INTEGER
                    CONSTRAINT CK_Membresias_DiaAnclaMensual
                    CHECK (DiaAnclaMensual IS NULL OR (typeof(DiaAnclaMensual) = 'integer' AND DiaAnclaMensual BETWEEN 1 AND 31));

                UPDATE Membresias
                SET DiaAnclaMensual = CAST(strftime('%d', FechaInicio) AS INTEGER)
                WHERE EXISTS (
                    SELECT 1 FROM Planes WHERE Planes.Id = Membresias.PlanId AND DuracionDias IS NULL
                )
                AND FechaVencimiento = CASE
                    WHEN strftime('%d', date(FechaInicio, '+1 month')) = strftime('%d', FechaInicio)
                    THEN date(FechaInicio, '+1 month')
                    ELSE date(FechaInicio, 'start of month', '+2 months', '-1 day')
                END;
                """);

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MembresiaOrigenId = table.Column<int>(type: "INTEGER", nullable: false),
                    MembresiaCubiertaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaPago = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MontoCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    MedioPago = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.CheckConstraint("CK_Pagos_MedioPago", "length(MedioPago) BETWEEN 1 AND 40 AND length(trim(MedioPago)) > 0");
                    table.CheckConstraint("CK_Pagos_Monto", "typeof(MontoCentavos) = 'integer' AND MontoCentavos BETWEEN 1 AND 9999999999");
                    table.CheckConstraint("CK_Pagos_Observacion", "Observacion IS NULL OR (length(Observacion) <= 255 AND length(trim(Observacion)) > 0)");
                    table.CheckConstraint("CK_Pagos_TipoYPeriodo", "(Tipo = 1 AND MembresiaOrigenId = MembresiaCubiertaId) OR (Tipo = 2 AND MembresiaOrigenId <> MembresiaCubiertaId)");
                    table.ForeignKey(
                        name: "FK_Pagos_Membresias_MembresiaCubiertaId",
                        column: x => x.MembresiaCubiertaId,
                        principalTable: "Membresias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagos_Membresias_MembresiaOrigenId",
                        column: x => x.MembresiaOrigenId,
                        principalTable: "Membresias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_MembresiaCubiertaId",
                table: "Pagos",
                column: "MembresiaCubiertaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_MembresiaOrigenId",
                table: "Pagos",
                column: "MembresiaOrigenId",
                unique: true,
                filter: "Tipo = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Membresias_DiaAnclaMensual",
                table: "Membresias");

            migrationBuilder.DropColumn(
                name: "DiaAnclaMensual",
                table: "Membresias");
        }
    }
}
