using Microsoft.EntityFrameworkCore;
using Sabemi.Webhooks.Api.Domain;

namespace Sabemi.Webhooks.Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<EventoWebhookBruto> EventosWebhookBrutos => Set<EventoWebhookBruto>();
    public DbSet<StatusContrato> StatusContratos => Set<StatusContrato>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
