using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations;

public partial class JerarquiaUsuarios : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Usuarios",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Nombre = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                Apellido = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                Version = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Usuarios", x => x.Id);
                table.CheckConstraint("CK_Usuarios_Apellido", "length(Apellido) BETWEEN 1 AND 60");
                table.CheckConstraint("CK_Usuarios_Email", "length(Email) BETWEEN 1 AND 120");
                table.CheckConstraint("CK_Usuarios_Nombre", "length(Nombre) BETWEEN 1 AND 60");
                table.CheckConstraint("CK_Usuarios_PasswordHash",
                    "PasswordHash IS NULL OR (length(PasswordHash) <= 255 AND length(trim(PasswordHash)) > 0)");
            });

        migrationBuilder.Sql("""
            INSERT INTO Usuarios (Id, Nombre, Apellido, Email, Activo, Version)
            SELECT Id, Nombre, Apellido, Email, Activo, Version FROM Clientes;
            """);

        migrationBuilder.CreateTable(
            name: "ClientesMigrados",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false),
                Dni = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                Telefono = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                FechaAlta = table.Column<DateOnly>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Clientes", x => x.Id);
                table.CheckConstraint("CK_Clientes_Dni", DniValido);
                table.CheckConstraint("CK_Clientes_Telefono", TelefonoValido);
                table.ForeignKey("FK_Clientes_Usuarios_Id", x => x.Id, "Usuarios", "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql("""
            INSERT INTO ClientesMigrados (Id, Dni, Telefono, FechaAlta)
            SELECT Id, Dni, Telefono, FechaAlta FROM Clientes;
            """);

        migrationBuilder.DropTable(name: "Clientes");
        migrationBuilder.RenameTable(name: "ClientesMigrados", newName: "Clientes");
        migrationBuilder.CreateIndex(name: "IX_Clientes_Dni", table: "Clientes", column: "Dni", unique: true);

        migrationBuilder.CreateTable(
            name: "Administradores",
            columns: table => new { Id = table.Column<int>(type: "INTEGER", nullable: false) },
            constraints: table =>
            {
                table.PrimaryKey("PK_Administradores", x => x.Id);
                table.ForeignKey("FK_Administradores_Usuarios_Id", x => x.Id, "Usuarios", "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Recepcionistas",
            columns: table => new { Id = table.Column<int>(type: "INTEGER", nullable: false) },
            constraints: table =>
            {
                table.PrimaryKey("PK_Recepcionistas", x => x.Id);
                table.ForeignKey("FK_Recepcionistas_Usuarios_Id", x => x.Id, "Usuarios", "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // El esquema anterior no puede conservar usuarios internos ni credenciales.
        migrationBuilder.Sql("""
            CREATE TEMP TABLE VerificarReversionUsuarios (
                Permitido INTEGER CHECK (Permitido = 1)
            );
            INSERT INTO VerificarReversionUsuarios
            SELECT CASE WHEN EXISTS (SELECT 1 FROM Administradores)
                OR EXISTS (SELECT 1 FROM Recepcionistas) OR EXISTS (
                SELECT 1 FROM Usuarios u LEFT JOIN Clientes c ON c.Id = u.Id
                WHERE c.Id IS NULL OR u.PasswordHash IS NOT NULL
            ) THEN 0 ELSE 1 END;
            DROP TABLE VerificarReversionUsuarios;
            """);

        migrationBuilder.CreateTable(
            name: "ClientesMigrados",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Nombre = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                Apellido = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Dni = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                Telefono = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                FechaAlta = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                Version = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Clientes", x => x.Id);
                table.CheckConstraint("CK_Clientes_Apellido", "length(Apellido) BETWEEN 1 AND 60");
                table.CheckConstraint("CK_Clientes_Dni", DniValido);
                table.CheckConstraint("CK_Clientes_Email", "length(Email) BETWEEN 1 AND 120");
                table.CheckConstraint("CK_Clientes_Nombre", "length(Nombre) BETWEEN 1 AND 60");
                table.CheckConstraint("CK_Clientes_Telefono", TelefonoValido);
            });

        migrationBuilder.Sql("""
            INSERT INTO ClientesMigrados (Id, Nombre, Apellido, Email, Dni, Telefono, FechaAlta, Activo, Version)
            SELECT c.Id, u.Nombre, u.Apellido, u.Email, c.Dni, c.Telefono, c.FechaAlta, u.Activo, u.Version
            FROM Clientes c JOIN Usuarios u ON u.Id = c.Id;
            """);

        migrationBuilder.DropTable(name: "Administradores");
        migrationBuilder.DropTable(name: "Recepcionistas");
        migrationBuilder.DropTable(name: "Clientes");
        migrationBuilder.DropTable(name: "Usuarios");
        migrationBuilder.RenameTable(name: "ClientesMigrados", newName: "Clientes");
        migrationBuilder.CreateIndex(name: "IX_Clientes_Dni", table: "Clientes", column: "Dni", unique: true);
    }

    private const string DniValido = "length(Dni) BETWEEN 1 AND 8 AND Dni NOT GLOB '*[^0-9]*'";

    private const string TelefonoValido =
        "(length(Telefono) BETWEEN 1 AND 15 AND Telefono NOT GLOB '*[^0-9]*') " +
        "OR (substr(Telefono, 1, 1) = '+' AND length(Telefono) BETWEEN 2 AND 16 AND substr(Telefono, 2) NOT GLOB '*[^0-9]*')";
}
