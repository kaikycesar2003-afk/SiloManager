using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiloManager.Domain.Entities;

namespace SiloManager.Infrastructure.Data.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> b)
        {
            b.ToTable("Usuarios");
            b.HasKey(u => u.Id);
            b.Property(u => u.Nome).IsRequired().HasMaxLength(150);
            b.Property(u => u.Login).IsRequired().HasMaxLength(50);
            b.Property(u => u.SenhaHash).IsRequired();
            b.Property(u => u.Nivel).IsRequired();
            b.Property(u => u.Ativo).HasDefaultValue(true);

            b.HasIndex(u => new { u.EmpresaId, u.Login }).IsUnique();

            b.HasOne(u => u.Empresa)
             .WithMany(e => e.Usuarios)
             .HasForeignKey(u => u.EmpresaId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}