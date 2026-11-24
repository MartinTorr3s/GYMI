using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class InicialClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
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
                    table.CheckConstraint("CK_Clientes_Dni", "length(Dni) BETWEEN 1 AND 8 AND Dni NOT GLOB '*[^0-9]*'");
                    table.CheckConstraint("CK_Clientes_Email", "length(Email) BETWEEN 1 AND 120");
                    table.CheckConstraint("CK_Clientes_Nombre", "length(Nombre) BETWEEN 1 AND 60");
                    table.CheckConstraint("CK_Clientes_Telefono", "(length(Telefono) BETWEEN 1 AND 15 AND Telefono NOT GLOB '*[^0-9]*') OR (substr(Telefono, 1, 1) = '+' AND length(Telefono) BETWEEN 2 AND 16 AND substr(Telefono, 2) NOT GLOB '*[^0-9]*')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Dni",
                table: "Clientes",
                column: "Dni",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
