using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiloManager.Domain.Entities;

namespace SiloManager.Infrastructure.Data.Configurations
{
    public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
    {
        public void Configure(EntityTypeBuilder<Produto> b)
        {
            b.ToTable("Produtos");
            b.HasKey(p => p.Id);
            b.Property(p => p.Nome).IsRequired().HasMaxLength(100);
            b.Property(p => p.UmidadeMinima).IsRequired();
            b.Property(p => p.UmidadeMaxima).IsRequired();
            b.Property(p => p.Ativo).HasDefaultValue(true);
        }
    }
}