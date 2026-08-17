using Microsoft.AspNetCore.Http.HttpResults;
using Sabemi.Webhooks.Api.Application;
using Sabemi.Webhooks.Api.Contracts;
using Sabemi.Webhooks.Api.Security;

namespace Sabemi.Webhooks.Api.Endpoints;

public static class WebhookEndpoints
{
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/webhooks")
            .WithTags("Webhooks")
            .MapPost("/pagamento", RecebePagamentoAsync)
            .AddEndpointFilter<AssinaturaWebhookFilter>()
            .WithName("RecebePagamento")
            .WithSummary("Recebe uma notificação de pagamento do banco parceiro")
            .WithDescription(
                "Valida o ApiKey e a assinatura HMAC-SHA256 do corpo, garante idempotência por " +
                "id_transacao e enfileira o processamento em background, respondendo imediatamente.");

        return app;
    }

    private static async Task<Results<Accepted<WebhookPagamentoResponse>, Ok<WebhookPagamentoResponse>, BadRequest<WebhookPagamentoResponse>>> RecebePagamentoAsync(
        HttpContext httpContext,
        IProcessamentoWebhookService servico,
        CancellationToken cancellationToken)
    {
        var corpoBruto = httpContext.Items[AssinaturaWebhookFilter.ItemCorpoBruto] as string ?? string.Empty;
        var assinaturaRecebida = httpContext.Items[AssinaturaWebhookFilter.ItemAssinaturaRecebida] as string;

        var resultado = await servico.ReceberAsync(corpoBruto, assinaturaRecebida, cancellationToken);

        return resultado.Situacao switch
        {
            "Invalido" => TypedResults.BadRequest(resultado),
            "Duplicado" => TypedResults.Ok(resultado),
            _ => TypedResults.Accepted((string?)null, resultado)
        };
    }
}
