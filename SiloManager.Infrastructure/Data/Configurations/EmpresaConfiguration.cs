using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiloManager.Domain.Entities;

namespace SiloManager.Infrastructure.Data.Configurations
{
    public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
    {
        public void Configure(EntityTypeBuilder<Empresa> b)
        {
            b.ToTable("Empresas");
            b.HasKey(e => e.Id);
            b.Property(e => e.Nome).IsRequired().HasMaxLength(150);
            b.Property(e => e.CNPJ).HasMaxLength(18);
            b.Property(e => e.Ativo).HasDefaultValue(true);
        }
    }
}