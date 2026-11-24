using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gymi.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class GestionRecepcionistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivacionesUsuario",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionUsuario = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    VenceEnUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivacionesUsuario", x => x.UsuarioId);
                    table.CheckConstraint("CK_ActivacionesUsuario_TokenHash", "length(TokenHash) = 64");
                    table.ForeignKey(
                        name: "FK_ActivacionesUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO ActivacionesUsuario (UsuarioId, VersionUsuario, TokenHash, VenceEnUtcTicks)
                SELECT ClienteId, VersionCliente, TokenHash, VenceEnUtcTicks FROM ActivacionesCliente;
                """);

            migrationBuilder.DropTable(name: "ActivacionesCliente");

            migrationBuilder.CreateIndex(
                name: "IX_ActivacionesUsuario_TokenHash",
                table: "ActivacionesUsuario",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivacionesCliente",
                columns: table => new
                {
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    VenceEnUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    VersionCliente = table.Column<Guid>(type: "TEXT", nullable: false)
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

            migrationBuilder.Sql("""
                INSERT INTO ActivacionesCliente (ClienteId, VersionCliente, TokenHash, VenceEnUtcTicks)
                SELECT a.UsuarioId, a.VersionUsuario, a.TokenHash, a.VenceEnUtcTicks
                FROM ActivacionesUsuario a
                INNER JOIN Clientes c ON c.Id = a.UsuarioId;
                """);

            migrationBuilder.DropTable(name: "ActivacionesUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_ActivacionesCliente_TokenHash",
                table: "ActivacionesCliente",
                column: "TokenHash",
                unique: true);
        }
    }
}
