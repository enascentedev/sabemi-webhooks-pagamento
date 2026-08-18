using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabemi.Webhooks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaContadorPagamentosComErro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantidadePagamentosComErro",
                table: "status_contrato",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantidadePagamentosComErro",
                table: "status_contrato");
        }
    }
}
