namespace Sabemi.Webhooks.Api.Contracts;

/// <summary>
/// Situacao: "Recebido" (aceito e enfileirado), "Duplicado" (id_transacao já processado
/// anteriormente) ou "Invalido" (falhou na validação do payload).
/// </summary>
public sealed record WebhookPagamentoResponse(Guid EventoId, string Situacao, string Mensagem);
