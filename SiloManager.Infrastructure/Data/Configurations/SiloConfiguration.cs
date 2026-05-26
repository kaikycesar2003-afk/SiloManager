using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiloManager.Domain.Entities;

namespace SiloManager.Infrastructure.Data.Configurations
{
    public class SiloConfiguration : IEntityTypeConfiguration<Silo>
    {
        public void Configure(EntityTypeBuilder<Silo> b)
        {
            b.ToTable("Silos");
            b.HasKey(s => s.Id);
            b.Property(s => s.Nome).IsRequired().HasMaxLength(100);
            b.Property(s => s.IsRetrabalho).HasDefaultValue(false);
            b.Property(s => s.Ativo).HasDefaultValue(true);

            b.HasOne(s => s.Empresa)
             .WithMany(e => e.Silos)
             .HasForeignKey(s => s.EmpresaId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(s => s.Produto)
             .WithMany(p => p.Silos)
             .HasForeignKey(s => s.ProdutoId)
             .OnDelete(DeleteBehavior.SetNull);
        }
    }
}