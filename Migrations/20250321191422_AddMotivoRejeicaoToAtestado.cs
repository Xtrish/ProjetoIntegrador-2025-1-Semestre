using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtestadoMedico.Migrations
{
    /// <inheritdoc />
    public partial class AddMotivoRejeicaoToAtestado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoRejeicao",
                table: "Atestados",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotivoRejeicao",
                table: "Atestados");
        }
    }
}
