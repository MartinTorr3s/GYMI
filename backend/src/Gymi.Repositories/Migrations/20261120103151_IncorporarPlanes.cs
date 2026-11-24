using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class IncorporarPlanes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Planes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PrecioCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    DuracionDias = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadSesiones = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planes", x => x.Id);
                    table.CheckConstraint("CK_Planes_Activo", "Activo IN (0, 1)");
                    table.CheckConstraint("CK_Planes_Descripcion", "length(Descripcion) BETWEEN 1 AND 255 AND length(trim(Descripcion)) > 0");
                    table.CheckConstraint("CK_Planes_Duracion", "typeof(DuracionDias) = 'integer' AND DuracionDias BETWEEN 1 AND 2147483647");
                    table.CheckConstraint("CK_Planes_Nombre", "length(Nombre) BETWEEN 1 AND 80 AND length(trim(Nombre)) > 0");
                    table.CheckConstraint("CK_Planes_Precio", "typeof(PrecioCentavos) = 'integer' AND PrecioCentavos BETWEEN 0 AND 9999999999");
                    table.CheckConstraint("CK_Planes_Sesiones", "typeof(CantidadSesiones) = 'integer' AND CantidadSesiones BETWEEN 1 AND 2147483647");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Planes");
        }
    }
}
