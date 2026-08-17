using Sabemi.Webhooks.Api.Contracts;

namespace Sabemi.Webhooks.Api.Application;

public interface IProcessamentoWebhookService
{
    Task<WebhookPagamentoResponse> ReceberAsync(string payloadBruto, string? assinaturaRecebida, CancellationToken cancellationToken);
}
