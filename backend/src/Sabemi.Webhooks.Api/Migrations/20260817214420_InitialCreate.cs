using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabemi.Webhooks.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_webhook_brutos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdTransacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IdContrato = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DataPagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatusPagamentoBanco = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PayloadBruto = table.Column<string>(type: "jsonb", nullable: false),
                    AssinaturaRecebida = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StatusProcessamento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErroMensagem = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Tentativas = table.Column<int>(type: "integer", nullable: false),
                    RecebidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_webhook_brutos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "status_contrato",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdContrato = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ValorTotalPago = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QuantidadePagamentos = table.Column<int>(type: "integer", nullable: false),
                    UltimoIdTransacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UltimoPagamentoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_contrato", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_webhook_brutos_IdContrato",
                table: "eventos_webhook_brutos",
                column: "IdContrato");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_webhook_brutos_IdTransacao",
                table: "eventos_webhook_brutos",
                column: "IdTransacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_eventos_webhook_brutos_StatusProcessamento",
                table: "eventos_webhook_brutos",
                column: "StatusProcessamento");

            migrationBuilder.CreateIndex(
                name: "IX_status_contrato_IdContrato",
                table: "status_contrato",
                column: "IdContrato",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_webhook_brutos");

            migrationBuilder.DropTable(
                name: "status_contrato");
        }
    }
}
