using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiloManager.Domain.Entities;

namespace SiloManager.Infrastructure.Data.Configurations
{
    public class EquipamentoConfiguration : IEntityTypeConfiguration<Equipamento>
    {
        public void Configure(EntityTypeBuilder<Equipamento> b)
        {
            b.ToTable("Equipamentos");
            b.HasKey(e => e.Id);
            b.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            b.Property(e => e.Modelo).HasMaxLength(50);
            b.Property(e => e.NumeroSerie).HasMaxLength(50);
            b.Property(e => e.PortaCOM).IsRequired().HasMaxLength(10);
            b.Property(e => e.BaudRate).HasDefaultValue(9600);
            b.Property(e => e.Ativo).HasDefaultValue(true);

            b.HasOne(e => e.Empresa)
             .WithMany(emp => emp.Equipamentos)
             .HasForeignKey(e => e.EmpresaId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}