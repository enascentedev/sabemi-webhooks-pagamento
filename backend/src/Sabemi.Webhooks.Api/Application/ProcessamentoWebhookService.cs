using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sabemi.Webhooks.Api.Contracts;
using Sabemi.Webhooks.Api.Domain;
using Sabemi.Webhooks.Api.Infrastructure;

namespace Sabemi.Webhooks.Api.Application;

public sealed class ProcessamentoWebhookService(
    AppDbContext db,
    IFilaProcessamentoPagamentos fila,
    ILogger<ProcessamentoWebhookService> logger) : IProcessamentoWebhookService
{
    private static readonly string[] StatusValidos = ["Sucesso", "Erro"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<WebhookPagamentoResponse> ReceberAsync(string payloadBruto, string? assinaturaRecebida, CancellationToken cancellationToken)
    {
        var (requisicao, erroParsing) = Desserializar(payloadBruto);

        // A coluna é timestamptz e o Npgsql só aceita DateTime com Kind=Utc nela. Normalizar aqui,
        // antes de qualquer uso, cobre tanto o evento válido quanto o inválido — sem isso, uma data
        // como "2026-08-17T12:00:00" (Kind=Unspecified) estouraria no SaveChanges e o evento não
        // seria nem registrado no log bruto.
        requisicao = requisicao is null
            ? null
            : requisicao with { DataPagamento = NormalizarParaUtc(requisicao.DataPagamento) };

        var erroValidacao = erroParsing ?? Validar(requisicao);

        var evento = erroValidacao is null
            ? EventoWebhookBruto.CriarValido(
                requisicao!.IdTransacao!,
                requisicao.IdContrato!,
                requisicao.Valor!.Value,
                requisicao.DataPagamento!.Value,
                NormalizarStatus(requisicao.Status!),
                payloadBruto,
                assinaturaRecebida)
            : EventoWebhookBruto.CriarInvalido(
                requisicao?.IdTransacao,
                requisicao?.IdContrato,
                requisicao?.Valor,
                requisicao?.DataPagamento,
                requisicao?.Status,
                payloadBruto,
                assinaturaRecebida,
                erroValidacao);

        db.EventosWebhookBrutos.Add(evento);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EhViolacaoDeChaveDuplicada(ex))
        {
            logger.LogInformation("Webhook duplicado recebido para id_transacao {IdTransacao}", requisicao?.IdTransacao);
            return new WebhookPagamentoResponse(Guid.Empty, "Duplicado", $"A transação '{requisicao?.IdTransacao}' já foi recebida anteriormente.");
        }

        if (erroValidacao is not null)
        {
            logger.LogWarning("Webhook inválido recebido (evento {EventoId}): {Erro}", evento.Id, erroValidacao);
            return new WebhookPagamentoResponse(evento.Id, "Invalido", erroValidacao);
        }

        await fila.EnfileirarAsync(evento.Id, cancellationToken);

        return new WebhookPagamentoResponse(evento.Id, "Recebido", "Notificação recebida e enfileirada para processamento.");
    }

    private static (WebhookPagamentoRequest? Requisicao, string? Erro) Desserializar(string payloadBruto)
    {
        try
        {
            var requisicao = JsonSerializer.Deserialize<WebhookPagamentoRequest>(payloadBruto, JsonOptions);
            return (requisicao, null);
        }
        catch (JsonException ex)
        {
            // ex.Path identifica o campo quando o JSON está sintaticamente correto mas um valor não
            // converte (ex.: "valor": "abc"). Sem isso, o painel mostraria "JSON inválido" para um
            // payload que na verdade é JSON válido, escondendo qual campo está errado.
            var campo = ex.Path?.TrimStart('$', '.');

            return (null, string.IsNullOrEmpty(campo)
                ? "Corpo da requisição não é um JSON válido."
                : $"Campo '{campo}' tem formato inválido.");
        }
    }

    /// <summary>
    /// Converte a data recebida para UTC. <c>Unspecified</c> (data sem fuso, como
    /// "2026-08-17T12:00:00") é assumido como já sendo UTC, seguindo o contrato do banco parceiro;
    /// datas com offset são convertidas para o instante equivalente em UTC.
    /// </summary>
    private static DateTime? NormalizarParaUtc(DateTime? valor) => valor?.Kind switch
    {
        null => null,
        DateTimeKind.Utc => valor,
        DateTimeKind.Local => valor.Value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc)
    };

    private static string? Validar(WebhookPagamentoRequest? requisicao)
    {
        if (requisicao is null)
        {
            return "Corpo da requisição não é um JSON válido.";
        }

        if (string.IsNullOrWhiteSpace(requisicao.IdTransacao))
        {
            return "Campo 'id_transacao' é obrigatório.";
        }

        if (string.IsNullOrWhiteSpace(requisicao.IdContrato))
        {
            return "Campo 'id_contrato' é obrigatório.";
        }

        if (requisicao.Valor is null or <= 0)
        {
            return "Campo 'valor' deve ser um número maior que zero.";
        }

        if (requisicao.DataPagamento is null)
        {
            return "Campo 'data_pagamento' é obrigatório.";
        }

        if (string.IsNullOrWhiteSpace(requisicao.Status) || !StatusValidos.Contains(requisicao.Status, StringComparer.OrdinalIgnoreCase))
        {
            return "Campo 'status' deve ser 'Sucesso' ou 'Erro'.";
        }

        return null;
    }

    private static string NormalizarStatus(string status) =>
        StatusValidos.First(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));

    private static bool EhViolacaoDeChaveDuplicada(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
