using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiloManager.Domain.Entities;

namespace SiloManager.Infrastructure.Data.Configurations
{
    public class MedicaoConfiguration : IEntityTypeConfiguration<Medicao>
    {
        public void Configure(EntityTypeBuilder<Medicao> b)
        {
            b.ToTable("Medicoes");
            b.HasKey(m => m.Id);
            b.Property(m => m.Umidade).IsRequired();
            b.Property(m => m.DataHoraSistema).IsRequired();
            b.Property(m => m.DataHoraEquipamento);
            b.Property(m => m.IntervaloSegundos);
            b.Property(m => m.IsRetrabalho).HasDefaultValue(false);
            b.Property(m => m.Observacao).HasMaxLength(500);
            b.Property(m => m.DadosBrutosSerial).HasMaxLength(1000);

            b.HasOne(m => m.Empresa)
             .WithMany(e => e.Medicoes)
             .HasForeignKey(m => m.EmpresaId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(m => m.Usuario)
             .WithMany(u => u.Medicoes)
             .HasForeignKey(m => m.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(m => m.Produto)
             .WithMany(p => p.Medicoes)
             .HasForeignKey(m => m.ProdutoId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(m => m.Equipamento)
             .WithMany(e => e.Medicoes)
             .HasForeignKey(m => m.EquipamentoId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(m => m.SiloDestino)
             .WithMany(s => s.Medicoes)
             .HasForeignKey(m => m.SiloDestinoId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}