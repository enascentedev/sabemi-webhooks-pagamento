using Sabemi.Webhooks.Api.Contracts;

namespace Sabemi.Webhooks.Api.Application;

public interface IConsultaPagamentosService
{
    Task<PagamentosPaginadosResponse> ListarAsync(string? status, string? idContrato, int pagina, int tamanhoPagina, CancellationToken cancellationToken);

    Task<MetricasResponse> ObterMetricasAsync(CancellationToken cancellationToken);
}
