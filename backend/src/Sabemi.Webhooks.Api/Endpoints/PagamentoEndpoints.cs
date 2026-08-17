using Microsoft.AspNetCore.Http.HttpResults;
using Sabemi.Webhooks.Api.Application;
using Sabemi.Webhooks.Api.Contracts;

namespace Sabemi.Webhooks.Api.Endpoints;

public static class PagamentoEndpoints
{
    public static IEndpointRouteBuilder MapPagamentoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api").WithTags("Pagamentos");

        grupo.MapGet("/pagamentos", ListarAsync)
            .WithName("ListarPagamentos")
            .WithSummary("Lista os eventos de pagamento recebidos, com filtros e paginação");

        grupo.MapGet("/metricas", ObterMetricasAsync)
            .WithName("ObterMetricas")
            .WithSummary("Retorna as métricas agregadas para os cards do dashboard");

        return app;
    }

    private static async Task<Ok<PagamentosPaginadosResponse>> ListarAsync(
        IConsultaPagamentosService servico,
        CancellationToken cancellationToken,
        string? status = null,
        string? idContrato = null,
        int pagina = 1,
        int tamanhoPagina = 20)
    {
        var resultado = await servico.ListarAsync(status, idContrato, pagina, tamanhoPagina, cancellationToken);
        return TypedResults.Ok(resultado);
    }

    private static async Task<Ok<MetricasResponse>> ObterMetricasAsync(
        IConsultaPagamentosService servico,
        CancellationToken cancellationToken)
    {
        var resultado = await servico.ObterMetricasAsync(cancellationToken);
        return TypedResults.Ok(resultado);
    }
}
