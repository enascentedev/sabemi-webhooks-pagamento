namespace Sabemi.Webhooks.Api.Domain;

/// <summary>
/// Log de Eventos Brutos: registra cada notificação recebida do banco parceiro,
/// válida ou não, preservando o payload original para auditoria.
/// </summary>
public sealed class EventoWebhookBruto
{
    public Guid Id { get; private set; }
    public string? IdTransacao { get; private set; }
    public string? IdContrato { get; private set; }
    public decimal? Valor { get; private set; }
    public DateTime? DataPagamento { get; private set; }
    public string? StatusPagamentoBanco { get; private set; }
    public string PayloadBruto { get; private set; } = string.Empty;
    public string? AssinaturaRecebida { get; private set; }
    public StatusProcessamento StatusProcessamento { get; private set; }
    public string? ErroMensagem { get; private set; }
    public int Tentativas { get; private set; }
    public DateTimeOffset RecebidoEm { get; private set; }
    public DateTimeOffset? ProcessadoEm { get; private set; }

    private EventoWebhookBruto()
    {
    }

    public static EventoWebhookBruto CriarValido(
        string idTransacao,
        string idContrato,
        decimal valor,
        DateTime dataPagamento,
        string statusPagamentoBanco,
        string payloadBruto,
        string? assinaturaRecebida) => new()
    {
        Id = Guid.NewGuid(),
        IdTransacao = idTransacao,
        IdContrato = idContrato,
        Valor = valor,
        DataPagamento = dataPagamento,
        StatusPagamentoBanco = statusPagamentoBanco,
        PayloadBruto = payloadBruto,
        AssinaturaRecebida = assinaturaRecebida,
        StatusProcessamento = StatusProcessamento.Pendente,
        RecebidoEm = DateTimeOffset.UtcNow
    };

    public static EventoWebhookBruto CriarInvalido(
        string? idTransacao,
        string? idContrato,
        decimal? valor,
        DateTime? dataPagamento,
        string? statusPagamentoBanco,
        string payloadBruto,
        string? assinaturaRecebida,
        string mensagemErro) => new()
    {
        Id = Guid.NewGuid(),
        IdTransacao = idTransacao,
        IdContrato = idContrato,
        Valor = valor,
        DataPagamento = dataPagamento,
        StatusPagamentoBanco = statusPagamentoBanco,
        PayloadBruto = payloadBruto,
        AssinaturaRecebida = assinaturaRecebida,
        StatusProcessamento = StatusProcessamento.Falha,
        ErroMensagem = mensagemErro,
        Tentativas = 1,
        RecebidoEm = DateTimeOffset.UtcNow,
        ProcessadoEm = DateTimeOffset.UtcNow
    };

    public void MarcarComoProcessando()
    {
        StatusProcessamento = StatusProcessamento.Processando;
        Tentativas++;
    }

    public void MarcarComoProcessado()
    {
        StatusProcessamento = StatusProcessamento.Processado;
        ProcessadoEm = DateTimeOffset.UtcNow;
        ErroMensagem = null;
    }

    public void MarcarComoFalha(string mensagemErro)
    {
        StatusProcessamento = StatusProcessamento.Falha;
        ErroMensagem = mensagemErro;
        ProcessadoEm = DateTimeOffset.UtcNow;
    }
}
