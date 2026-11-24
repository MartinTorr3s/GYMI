using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class PlanesPorMesCalendario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Planes_Duracion",
                table: "Planes");

            migrationBuilder.AlterColumn<int>(
                name: "DuracionDias",
                table: "Planes",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "SesionesPorSemana",
                table: "Planes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Planes_Vigencia",
                table: "Planes",
                sql: "(typeof(DuracionDias) = 'integer' AND DuracionDias BETWEEN 1 AND 2147483647 AND SesionesPorSemana IS NULL) OR (DuracionDias IS NULL AND typeof(SesionesPorSemana) = 'integer' AND SesionesPorSemana BETWEEN 1 AND 536870911 AND CantidadSesiones = SesionesPorSemana * 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // El esquema anterior solo admite planes con una duración positiva en días.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE VerificarReversionPlanes (
                    Permitido INTEGER CHECK (Permitido = 1)
                );
                INSERT INTO VerificarReversionPlanes
                SELECT CASE WHEN EXISTS (SELECT 1 FROM Planes WHERE DuracionDias IS NULL) THEN 0 ELSE 1 END;
                DROP TABLE VerificarReversionPlanes;
                """);
            migrationBuilder.DropCheckConstraint(
                name: "CK_Planes_Vigencia",
                table: "Planes");

            migrationBuilder.DropColumn(
                name: "SesionesPorSemana",
                table: "Planes");

            migrationBuilder.AlterColumn<int>(
                name: "DuracionDias",
                table: "Planes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Planes_Duracion",
                table: "Planes",
                sql: "typeof(DuracionDias) = 'integer' AND DuracionDias BETWEEN 1 AND 2147483647");
        }
    }
}
