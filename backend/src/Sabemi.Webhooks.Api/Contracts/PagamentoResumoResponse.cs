namespace Sabemi.Webhooks.Api.Contracts;

public sealed record PagamentoResumoResponse(
    Guid Id,
    string? IdTransacao,
    string? IdContrato,
    decimal? Valor,
    DateTime? DataPagamento,
    string? StatusPagamentoBanco,
    string StatusProcessamento,
    string? ErroMensagem,
    string PayloadBruto,
    DateTimeOffset RecebidoEm,
    DateTimeOffset? ProcessadoEm);

public sealed record PagamentosPaginadosResponse(
    IReadOnlyList<PagamentoResumoResponse> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total);

public sealed record MetricasResponse(
    int Total,
    int Pendentes,
    int Processando,
    int Processados,
    int Falhas);
