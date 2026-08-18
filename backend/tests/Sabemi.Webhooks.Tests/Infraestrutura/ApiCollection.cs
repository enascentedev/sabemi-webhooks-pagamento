namespace Sabemi.Webhooks.Tests.Infraestrutura;

/// <summary>
/// Coleção compartilhada por todas as classes de teste, para que exista uma única instância da
/// API — e, portanto, uma única execução das migrações — sobre o banco de testes.
/// Com IClassFixture cada classe subia o seu próprio host, e dois Database.MigrateAsync
/// concorrentes contra um banco vazio falham com 42701 ("column already exists"). O sintoma só
/// aparecia em banco limpo, como no CI.
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ApiCollection : ICollectionFixture<SabemiWebApplicationFactory>
{
    public const string Nome = "Api";
}
