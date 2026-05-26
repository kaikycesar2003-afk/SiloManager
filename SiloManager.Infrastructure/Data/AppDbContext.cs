using Microsoft.EntityFrameworkCore;
using SiloManager.Domain.Entities;
using SiloManager.Infrastructure.Data.Configurations;

namespace SiloManager.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Silo> Silos { get; set; }
        public DbSet<Equipamento> Equipamentos { get; set; }
        public DbSet<Medicao> Medicoes { get; set; }
        public DbSet<Configuracao> Configuracoes { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            mb.ApplyConfiguration(new EmpresaConfiguration());
            mb.ApplyConfiguration(new UsuarioConfiguration());
            mb.ApplyConfiguration(new ProdutoConfiguration());
            mb.ApplyConfiguration(new SiloConfiguration());
            mb.ApplyConfiguration(new EquipamentoConfiguration());
            mb.ApplyConfiguration(new MedicaoConfiguration());
            mb.ApplyConfiguration(new ConfiguracaoConfiguration());
        }
    }
}