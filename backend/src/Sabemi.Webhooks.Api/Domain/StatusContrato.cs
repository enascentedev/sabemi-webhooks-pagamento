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
    public int QuantidadePagamentosComErro { get; private set; }
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
        SituacaoContrato situacao)
    {
        var contrato = new StatusContrato
        {
            Id = Guid.NewGuid(),
            IdContrato = idContrato
        };

        contrato.RegistrarPagamento(valor, idTransacao, dataPagamento, situacao);
        return contrato;
    }

    /// <summary>
    /// Só um pagamento liquidado (<see cref="SituacaoContrato.Sucesso"/>) entra no valor total e na
    /// contagem de pagamentos: uma notificação que o banco reportou como <c>Erro</c> não liquidou
    /// nada e inflaria o saldo do contrato. Ela é contabilizada à parte, e em ambos os casos o
    /// contrato passa a refletir o último evento recebido.
    /// </summary>
    public void RegistrarPagamento(decimal valor, string idTransacao, DateTime dataPagamento, SituacaoContrato situacao)
    {
        if (situacao == SituacaoContrato.Sucesso)
        {
            ValorTotalPago += valor;
            QuantidadePagamentos++;
        }
        else
        {
            QuantidadePagamentosComErro++;
        }

        UltimoIdTransacao = idTransacao;
        UltimoPagamentoEm = dataPagamento;
        AtualizadoEm = DateTimeOffset.UtcNow;
        Situacao = situacao;
    }
}
