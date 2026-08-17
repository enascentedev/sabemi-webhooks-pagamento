using Microsoft.EntityFrameworkCore;
using Sabemi.Webhooks.Api.Contracts;
using Sabemi.Webhooks.Api.Domain;
using Sabemi.Webhooks.Api.Infrastructure;

namespace Sabemi.Webhooks.Api.Application;

public sealed class ConsultaPagamentosService(AppDbContext db) : IConsultaPagamentosService
{
    private const int TamanhoPaginaPadrao = 20;
    private const int TamanhoPaginaMaximo = 200;

    public async Task<PagamentosPaginadosResponse> ListarAsync(string? status, string? idContrato, int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        pagina = pagina < 1 ? 1 : pagina;
        tamanhoPagina = tamanhoPagina is < 1 or > TamanhoPaginaMaximo ? TamanhoPaginaPadrao : tamanhoPagina;

        var consulta = db.EventosWebhookBrutos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(idContrato))
        {
            consulta = consulta.Where(e => e.IdContrato != null && e.IdContrato.Contains(idContrato));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            consulta = string.Equals(status, "Falha", StringComparison.OrdinalIgnoreCase)
                ? consulta.Where(e => e.StatusProcessamento == StatusProcessamento.Falha)
                : consulta.Where(e => e.StatusPagamentoBanco == status && e.StatusProcessamento != StatusProcessamento.Falha);
        }

        var total = await consulta.CountAsync(cancellationToken);

        var itens = await consulta
            .OrderByDescending(e => e.RecebidoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(e => new PagamentoResumoResponse(
                e.Id,
                e.IdTransacao,
                e.IdContrato,
                e.Valor,
                e.DataPagamento,
                e.StatusPagamentoBanco,
                e.StatusProcessamento.ToString(),
                e.ErroMensagem,
                e.PayloadBruto,
                e.RecebidoEm,
                e.ProcessadoEm))
            .ToListAsync(cancellationToken);

        return new PagamentosPaginadosResponse(itens, pagina, tamanhoPagina, total);
    }

    public async Task<MetricasResponse> ObterMetricasAsync(CancellationToken cancellationToken)
    {
        var contagens = await db.EventosWebhookBrutos
            .GroupBy(e => e.StatusProcessamento)
            .Select(g => new { Status = g.Key, Quantidade = g.Count() })
            .ToListAsync(cancellationToken);

        int ObterQuantidade(StatusProcessamento status) =>
            contagens.FirstOrDefault(c => c.Status == status)?.Quantidade ?? 0;

        return new MetricasResponse(
            Total: contagens.Sum(c => c.Quantidade),
            Pendentes: ObterQuantidade(StatusProcessamento.Pendente),
            Processando: ObterQuantidade(StatusProcessamento.Processando),
            Processados: ObterQuantidade(StatusProcessamento.Processado),
            Falhas: ObterQuantidade(StatusProcessamento.Falha));
    }
}
