using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiloManager.Domain.Entities;

namespace SiloManager.Infrastructure.Data.Configurations
{
    public class ConfiguracaoConfiguration : IEntityTypeConfiguration<Configuracao>
    {
        public void Configure(EntityTypeBuilder<Configuracao> b)
        {
            b.ToTable("Configuracoes");
            b.HasKey(c => c.Id);
            b.Property(c => c.IntervaloMinimoSegundos)
             .IsRequired()
             .HasDefaultValue(900); // 15 minutos

            b.HasOne(c => c.Empresa)
             .WithOne(e => e.Configuracao)
             .HasForeignKey<Configuracao>(c => c.EmpresaId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}