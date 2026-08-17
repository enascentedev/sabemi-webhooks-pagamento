using System.Security.Cryptography;
using System.Text;

namespace Sabemi.Webhooks.Tests.Infraestrutura;

public static class AssinaturaTestHelper
{
    public static string CalcularAssinatura(string corpo, string segredo = SabemiWebApplicationFactory.SegredoAssinatura)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), Encoding.UTF8.GetBytes(corpo));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static HttpRequestMessage CriarRequisicaoWebhook(
        string corpoJson,
        string? apiKey = SabemiWebApplicationFactory.ApiKey,
        string? assinatura = null)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/webhooks/pagamento")
        {
            Content = new StringContent(corpoJson, Encoding.UTF8, "application/json")
        };

        if (apiKey is not null)
        {
            requisicao.Headers.Add("X-Api-Key", apiKey);
        }

        requisicao.Headers.Add("X-Signature", assinatura ?? CalcularAssinatura(corpoJson));

        return requisicao;
    }
}
