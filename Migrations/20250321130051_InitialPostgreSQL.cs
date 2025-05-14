using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AtestadoMedico.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Senha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Atestados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DataAtestado = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NomeMedico = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CRM = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AtualizadoPor = table.Column<int>(type: "integer", nullable: true),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NomeArquivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TipoArquivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CaminhoArquivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atestados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Atestados_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "DataCadastro", "Email", "IsAdmin", "Nome", "Senha" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 3, 18, 10, 0, 0, 0, DateTimeKind.Utc), "admin@admin.com", true, "Administrador", "admin" },
                    { 2, new DateTime(2024, 3, 18, 10, 0, 0, 0, DateTimeKind.Utc), "usuario@teste.com", false, "Usuário Teste", "123456" },
                    { 3, new DateTime(2024, 3, 18, 10, 0, 0, 0, DateTimeKind.Utc), "junior@gmail.com", true, "Junior", "junior@123" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Atestados_UsuarioId",
                table: "Atestados",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Atestados");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
