using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class CupoElegidoPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Planes_Vigencia",
                table: "Planes");

            migrationBuilder.DropColumn(
                name: "SesionesPorSemana",
                table: "Planes");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Planes_Vigencia",
                table: "Planes",
                sql: "DuracionDias IS NULL OR (typeof(DuracionDias) = 'integer' AND DuracionDias BETWEEN 1 AND 2147483647)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La versión anterior calculaba el cupo mensual como frecuencia por cuatro.
            // Su restricción impide revertir si existe un cupo mensual que no puede representar.
            migrationBuilder.Sql("""
                CREATE TABLE PlanesReversionCupo (
                    Id INTEGER NOT NULL CONSTRAINT PK_Planes PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL CONSTRAINT CK_Planes_Nombre
                        CHECK (length(Nombre) BETWEEN 1 AND 80 AND length(trim(Nombre)) > 0),
                    Descripcion TEXT NOT NULL CONSTRAINT CK_Planes_Descripcion
                        CHECK (length(Descripcion) BETWEEN 1 AND 255 AND length(trim(Descripcion)) > 0),
                    PrecioCentavos INTEGER NOT NULL CONSTRAINT CK_Planes_Precio
                        CHECK (typeof(PrecioCentavos) = 'integer' AND PrecioCentavos BETWEEN 0 AND 9999999999),
                    DuracionDias INTEGER,
                    SesionesPorSemana INTEGER,
                    CantidadSesiones INTEGER NOT NULL CONSTRAINT CK_Planes_Sesiones
                        CHECK (typeof(CantidadSesiones) = 'integer' AND CantidadSesiones BETWEEN 1 AND 2147483647),
                    Activo INTEGER NOT NULL CONSTRAINT CK_Planes_Activo CHECK (Activo IN (0, 1)),
                    Version TEXT NOT NULL,
                    CONSTRAINT CK_Planes_Vigencia
                        CHECK (
                            (typeof(DuracionDias) = 'integer' AND DuracionDias BETWEEN 1 AND 2147483647 AND SesionesPorSemana IS NULL)
                            OR (DuracionDias IS NULL AND typeof(SesionesPorSemana) = 'integer'
                                AND SesionesPorSemana BETWEEN 1 AND 536870911
                                AND CantidadSesiones = SesionesPorSemana * 4)
                        )
                );

                INSERT INTO PlanesReversionCupo (Id, Nombre, Descripcion, PrecioCentavos, DuracionDias,
                    SesionesPorSemana, CantidadSesiones, Activo, Version)
                SELECT Id, Nombre, Descripcion, PrecioCentavos, DuracionDias,
                    CASE WHEN DuracionDias IS NULL THEN CantidadSesiones / 4 END, CantidadSesiones, Activo, Version
                FROM Planes;

                DROP TABLE Planes;
                ALTER TABLE PlanesReversionCupo RENAME TO Planes;
                """);
        }
    }
}
