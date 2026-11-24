using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gymi.Repositories.Migrations;

[DbContext(typeof(GymiDbContext))]
[Migration("20261123045000_CorreosEquivalentesYDatosContacto")]
public sealed class CorreosEquivalentesYDatosContacto : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ActualizarClavesDeAcceso(migrationBuilder, "gymi_email_acceso");
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_Clientes_ContactoSinNulos_Alta
            BEFORE INSERT ON Clientes
            WHEN instr(NEW.Dni, char(0)) > 0 OR instr(NEW.Telefono, char(0)) > 0
            BEGIN
                SELECT RAISE(ABORT, 'contacto_con_caracter_nulo');
            END;

            CREATE TRIGGER TR_Clientes_ContactoSinNulos_Edicion
            BEFORE UPDATE OF Dni, Telefono ON Clientes
            WHEN instr(NEW.Dni, char(0)) > 0 OR instr(NEW.Telefono, char(0)) > 0
            BEGIN
                SELECT RAISE(ABORT, 'contacto_con_caracter_nulo');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ActualizarClavesDeAcceso(migrationBuilder, "gymi_email_legado");
        migrationBuilder.Sql("""
            DROP TRIGGER TR_Clientes_ContactoSinNulos_Edicion;
            DROP TRIGGER TR_Clientes_ContactoSinNulos_Alta;
            """);
    }

    private static void ActualizarClavesDeAcceso(MigrationBuilder migrationBuilder, string normalizar)
    {
        // La restricción temporal detecta colisiones antes de modificar las cuentas.
        migrationBuilder.Sql($"""
            CREATE TEMP TABLE CorreosAccesoMigracion (Id INTEGER PRIMARY KEY, Clave TEXT NOT NULL UNIQUE);

            INSERT INTO CorreosAccesoMigracion (Id, Clave)
            SELECT Id, {normalizar}(Email) FROM Usuarios WHERE EmailAccesoNormalizado IS NOT NULL;

            DROP INDEX IX_Usuarios_EmailAccesoNormalizado;

            UPDATE Usuarios
            SET EmailAccesoNormalizado = (SELECT Clave FROM CorreosAccesoMigracion WHERE Id = Usuarios.Id)
            WHERE EmailAccesoNormalizado IS NOT NULL;

            CREATE UNIQUE INDEX IX_Usuarios_EmailAccesoNormalizado ON Usuarios (EmailAccesoNormalizado);
            DROP TABLE CorreosAccesoMigracion;
            """);
    }
}
