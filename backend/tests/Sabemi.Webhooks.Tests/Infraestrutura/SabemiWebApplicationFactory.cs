using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Sabemi.Webhooks.Tests.Infraestrutura;

/// <summary>
/// Sobe a API sobre um banco Postgres real e isolado ("sabemi_webhooks_tests"),
/// aplicando as mesmas migrations do app (Database.Migrate cria o banco se não existir).
/// Reaproveita o mesmo servidor Postgres do docker-compose usado em desenvolvimento.
/// </summary>
public sealed class SabemiWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-api-key";
    public const string SegredoAssinatura = "test-segredo-hmac";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] =
                    "Host=localhost;Port=5432;Database=sabemi_webhooks_tests;Username=sabemi;Password=sabemi_dev_password",
                ["WebhookSeguranca:ApiKey"] = ApiKey,
                ["WebhookSeguranca:SegredoAssinatura"] = SegredoAssinatura,
            });
        });
    }
}
