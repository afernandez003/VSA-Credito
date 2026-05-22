using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Creditos.Infra.Data.Configurations;

public sealed class CreditoConfiguration : IEntityTypeConfiguration<Domain.Credito>
{
    public void Configure(EntityTypeBuilder<Domain.Credito> builder)
    {
        builder.ToTable("credito");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(c => c.NumeroCredito)
            .HasColumnName("numero_credito")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.NumeroNfse)
            .HasColumnName("numero_nfse")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.DataConstituicao)
            .HasColumnName("data_constituicao")
            .IsRequired();

        builder.Property(c => c.ValorIssqn)
            .HasColumnName("valor_issqn")
            .HasPrecision(15, 2)
            .IsRequired();

        builder.Property(c => c.TipoCredito)
            .HasColumnName("tipo_credito")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.SimplesNacional)
            .HasColumnName("simples_nacional")
            .IsRequired();

        builder.Property(c => c.Aliquota)
            .HasColumnName("aliquota")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(c => c.ValorFaturado)
            .HasColumnName("valor_faturado")
            .HasPrecision(15, 2)
            .IsRequired();

        builder.Property(c => c.ValorDeducao)
            .HasColumnName("valor_deducao")
            .HasPrecision(15, 2)
            .IsRequired();

        builder.Property(c => c.BaseCalculo)
            .HasColumnName("base_calculo")
            .HasPrecision(15, 2)
            .IsRequired();

        builder.HasIndex(c => c.NumeroCredito).IsUnique();
        builder.HasIndex(c => c.NumeroNfse);
    }
}
