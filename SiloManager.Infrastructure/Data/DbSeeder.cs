using SiloManager.Domain.Entities;
using SiloManager.Domain.Enums;

namespace SiloManager.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db)
        {
            // Só executa se não houver nenhuma empresa cadastrada
            if (db.Empresas.Any()) return;

            // Empresa padrão
            var empresa = new Empresa
            {
                Nome = "Empresa Padrão",
                CNPJ = "00.000.000/0001-00",
                Ativo = true
            };
            db.Empresas.Add(empresa);
            db.SaveChanges();

            // Usuário administrador padrão
            var admin = new Usuario
            {
                EmpresaId = empresa.Id,
                Nome = "Administrador",
                Login = "admin",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Nivel = NivelAcesso.Administrador,
                Ativo = true
            };
            db.Usuarios.Add(admin);

            // Produtos padrão
            var produtos = new List<Produto>
            {
                new() { Nome = "Soja",  UmidadeMinima = 10.0, UmidadeIdeal = 12.5, UmidadeMaxima = 14.0 },
                new() { Nome = "Milho", UmidadeMinima = 12.0, UmidadeIdeal = 13.5, UmidadeMaxima = 15.5 },
                new() { Nome = "Trigo", UmidadeMinima = 11.0, UmidadeIdeal = 13.0, UmidadeMaxima = 15.0 },
            };
            db.Produtos.AddRange(produtos);

            // Configuração padrão (timer de 15 minutos)
            db.Configuracoes.Add(new Configuracao
            {
                EmpresaId = empresa.Id,
                IntervaloMinimoSegundos = 900
            });

            db.SaveChanges();
        }
    }
}