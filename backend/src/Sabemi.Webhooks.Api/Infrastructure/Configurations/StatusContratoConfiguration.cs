using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabemi.Webhooks.Api.Domain;

namespace Sabemi.Webhooks.Api.Infrastructure.Configurations;

public sealed class StatusContratoConfiguration : IEntityTypeConfiguration<StatusContrato>
{
    public void Configure(EntityTypeBuilder<StatusContrato> builder)
    {
        builder.ToTable("status_contrato");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.IdContrato).HasMaxLength(200).IsRequired();
        builder.Property(c => c.ValorTotalPago).HasColumnType("numeric(18,2)");
        builder.Property(c => c.UltimoIdTransacao).HasMaxLength(200);
        builder.Property(c => c.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => c.IdContrato).IsUnique();
    }
}
