using System.Text.Json.Serialization;

namespace Sabemi.Webhooks.Api.Contracts;

/// <summary>
/// Payload enviado pelo banco parceiro em /webhooks/pagamento.
/// Os nomes dos campos seguem o snake_case definido pelo contrato do banco.
/// </summary>
public sealed record WebhookPagamentoRequest(
    [property: JsonPropertyName("id_transacao")] string? IdTransacao,
    [property: JsonPropertyName("id_contrato")] string? IdContrato,
    [property: JsonPropertyName("valor")] decimal? Valor,
    [property: JsonPropertyName("data_pagamento")] DateTime? DataPagamento,
    [property: JsonPropertyName("status")] string? Status);
