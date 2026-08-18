using System.Text.Json;

namespace Sabemi.Webhooks.Tests.Infraestrutura;

public static class PayloadTestHelper
{
    public static string CriarPayloadValido(
        string? idTransacao = null,
        string? idContrato = null,
        decimal valor = 150.75m,
        string status = "Sucesso",
        string? dataPagamento = null) =>
        JsonSerializer.Serialize(new
        {
            id_transacao = idTransacao ?? Guid.NewGuid().ToString(),
            id_contrato = idContrato ?? Guid.NewGuid().ToString(),
            valor,
            data_pagamento = dataPagamento ?? DateTime.UtcNow.ToString("O"),
            status
        });
}
