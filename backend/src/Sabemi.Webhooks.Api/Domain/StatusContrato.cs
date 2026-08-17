namespace Sabemi.Webhooks.Api.Domain;

/// <summary>
/// Status do Contrato: visão agregada e atualizada por contrato,
/// mantida pelo processamento em background a partir dos eventos brutos.
/// </summary>
public sealed class StatusContrato
{
    public Guid Id { get; private set; }
    public string IdContrato { get; private set; } = string.Empty;
    public decimal ValorTotalPago { get; private set; }
    public int QuantidadePagamentos { get; private set; }
    public string UltimoIdTransacao { get; private set; } = string.Empty;
    public DateTime UltimoPagamentoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }
    public SituacaoContrato Situacao { get; private set; }

    private StatusContrato()
    {
    }

    public static StatusContrato CriarComPagamento(
        string idContrato,
        decimal valor,
        string idTransacao,
        DateTime dataPagamento,
        SituacaoContrato situacao) => new()
    {
        Id = Guid.NewGuid(),
        IdContrato = idContrato,
        ValorTotalPago = valor,
        QuantidadePagamentos = 1,
        UltimoIdTransacao = idTransacao,
        UltimoPagamentoEm = dataPagamento,
        AtualizadoEm = DateTimeOffset.UtcNow,
        Situacao = situacao
    };

    public void RegistrarPagamento(decimal valor, string idTransacao, DateTime dataPagamento, SituacaoContrato situacao)
    {
        ValorTotalPago += valor;
        QuantidadePagamentos++;
        UltimoIdTransacao = idTransacao;
        UltimoPagamentoEm = dataPagamento;
        AtualizadoEm = DateTimeOffset.UtcNow;
        Situacao = situacao;
    }
}
