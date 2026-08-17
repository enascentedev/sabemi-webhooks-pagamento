using System.Security.Cryptography;
using System.Text;

namespace Sabemi.Webhooks.Api.Security;

/// <summary>
/// Valida o header "X-Api-Key" e a assinatura HMAC-SHA256 ("X-Signature") calculada
/// sobre o corpo bruto da requisição. O corpo lido e a assinatura recebida ficam
/// disponíveis em HttpContext.Items para o endpoint reaproveitar sem reler o stream.
/// </summary>
public sealed class AssinaturaWebhookFilter(IConfiguration configuration, ILogger<AssinaturaWebhookFilter> logger)
    : IEndpointFilter
{
    public const string HeaderApiKey = "X-Api-Key";
    public const string HeaderSignature = "X-Signature";
    public const string ItemCorpoBruto = "CorpoBrutoWebhook";
    public const string ItemAssinaturaRecebida = "AssinaturaRecebidaWebhook";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        var apiKeyEsperada = configuration["WebhookSeguranca:ApiKey"];
        var segredo = configuration["WebhookSeguranca:SegredoAssinatura"];

        if (string.IsNullOrEmpty(apiKeyEsperada) || string.IsNullOrEmpty(segredo))
        {
            logger.LogError("Configuração de segurança do webhook ausente (WebhookSeguranca:ApiKey / SegredoAssinatura).");
            return Results.Problem("Configuração de segurança do webhook indisponível.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var apiKeyRecebida = httpContext.Request.Headers[HeaderApiKey].ToString();
        if (!SaoIguais(apiKeyRecebida, apiKeyEsperada))
        {
            return Results.Unauthorized();
        }

        using var leitor = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
        var corpoBruto = await leitor.ReadToEndAsync(httpContext.RequestAborted);

        var assinaturaRecebida = httpContext.Request.Headers[HeaderSignature].ToString();
        var assinaturaEsperada = CalcularAssinatura(corpoBruto, segredo);

        if (!SaoIguais(assinaturaRecebida, assinaturaEsperada))
        {
            return Results.Unauthorized();
        }

        httpContext.Items[ItemCorpoBruto] = corpoBruto;
        httpContext.Items[ItemAssinaturaRecebida] = assinaturaRecebida;

        return await next(context);
    }

    private static string CalcularAssinatura(string corpo, string segredo)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), Encoding.UTF8.GetBytes(corpo));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool SaoIguais(string recebido, string esperado) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recebido), Encoding.UTF8.GetBytes(esperado));
}
