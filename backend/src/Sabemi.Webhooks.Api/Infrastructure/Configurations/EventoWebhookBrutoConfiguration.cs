using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabemi.Webhooks.Api.Domain;

namespace Sabemi.Webhooks.Api.Infrastructure.Configurations;

public sealed class EventoWebhookBrutoConfiguration : IEntityTypeConfiguration<EventoWebhookBruto>
{
    public void Configure(EntityTypeBuilder<EventoWebhookBruto> builder)
    {
        builder.ToTable("eventos_webhook_brutos");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.IdTransacao).HasMaxLength(200);
        builder.Property(e => e.IdContrato).HasMaxLength(200);
        builder.Property(e => e.Valor).HasColumnType("numeric(18,2)");
        builder.Property(e => e.StatusPagamentoBanco).HasMaxLength(50);
        builder.Property(e => e.PayloadBruto).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.AssinaturaRecebida).HasMaxLength(500);
        builder.Property(e => e.StatusProcessamento).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ErroMensagem).HasMaxLength(2000);

        // Único apenas entre valores não nulos: eventos com id_transacao ausente
        // (payload malformado) não disputam unicidade entre si no Postgres.
        builder.HasIndex(e => e.IdTransacao).IsUnique();
        builder.HasIndex(e => e.IdContrato);
        builder.HasIndex(e => e.StatusProcessamento);
    }
}
