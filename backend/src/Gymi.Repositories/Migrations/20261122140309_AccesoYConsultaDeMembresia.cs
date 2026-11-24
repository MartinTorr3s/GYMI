using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AccesoYConsultaDeMembresia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailAccesoNormalizado",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivacionesCliente",
                columns: table => new
                {
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionCliente = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    VenceEnUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivacionesCliente", x => x.ClienteId);
                    table.CheckConstraint("CK_ActivacionesCliente_TokenHash", "length(TokenHash) = 64");
                    table.ForeignKey(
                        name: "FK_ActivacionesCliente_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SesionesUsuario",
                columns: table => new
                {
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionUsuario = table.Column<Guid>(type: "TEXT", nullable: false),
                    VenceEnUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionesUsuario", x => x.TokenHash);
                    table.CheckConstraint("CK_SesionesUsuario_TokenHash", "length(TokenHash) = 64");
                    table.ForeignKey(
                        name: "FK_SesionesUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmailAccesoNormalizado",
                table: "Usuarios",
                column: "EmailAccesoNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivacionesCliente_TokenHash",
                table: "ActivacionesCliente",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SesionesUsuario_UsuarioId",
                table: "SesionesUsuario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesUsuario_VenceEnUtcTicks",
                table: "SesionesUsuario",
                column: "VenceEnUtcTicks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivacionesCliente");

            migrationBuilder.DropTable(
                name: "SesionesUsuario");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_EmailAccesoNormalizado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EmailAccesoNormalizado",
                table: "Usuarios");
        }
    }
}
