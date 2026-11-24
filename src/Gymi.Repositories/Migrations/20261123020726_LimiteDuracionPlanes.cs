using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class LimiteDuracionPlanes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Las filas históricas se conservan. El límite rige para altas y cambios de duración.
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_Planes_LimiteDuracion_Alta
                BEFORE INSERT ON Planes
                WHEN NEW.DuracionDias > 3652058
                BEGIN
                    SELECT RAISE(ABORT, 'duracion_dias_fuera_de_rango');
                END;

                CREATE TRIGGER TR_Planes_LimiteDuracion_Edicion
                BEFORE UPDATE OF DuracionDias ON Planes
                WHEN NEW.DuracionDias > 3652058
                BEGIN
                    SELECT RAISE(ABORT, 'duracion_dias_fuera_de_rango');
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER TR_Planes_LimiteDuracion_Edicion;
                DROP TRIGGER TR_Planes_LimiteDuracion_Alta;
                """);
        }
    }
}
