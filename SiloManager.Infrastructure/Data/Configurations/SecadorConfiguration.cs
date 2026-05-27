using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiloManager.Domain.Entities;

namespace SiloManager.Infrastructure.Data.Configurations
{
    public class SecadorConfiguration : IEntityTypeConfiguration<Secador>
    {
        public void Configure(EntityTypeBuilder<Secador> b)
        {
            b.ToTable("Secadores");
            b.HasKey(s => s.Id);
            b.Property(s => s.Nome).IsRequired().HasMaxLength(100);
            b.Property(s => s.Ativo).HasDefaultValue(true);

            b.HasOne(s => s.Empresa)
             .WithMany(e => e.Secadores)
             .HasForeignKey(s => s.EmpresaId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}