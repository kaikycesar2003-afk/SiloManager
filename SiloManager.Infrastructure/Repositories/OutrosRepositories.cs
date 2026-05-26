using Microsoft.EntityFrameworkCore;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using SiloManager.Infrastructure.Data;

namespace SiloManager.Infrastructure.Repositories
{
    public class EmpresaRepository : Repository<Empresa>, IEmpresaRepository
    {
        public EmpresaRepository(AppDbContext db) : base(db) { }

        public async Task<IEnumerable<Empresa>> GetAtivasAsync() =>
            await _set.Where(e => e.Ativo).ToListAsync();
    }

    public class ProdutoRepository : Repository<Produto>, IProdutoRepository
    {
        public ProdutoRepository(AppDbContext db) : base(db) { }

        public async Task<IEnumerable<Produto>> GetAtivosAsync() =>
            await _set.Where(p => p.Ativo).ToListAsync();

        public async Task<Produto?> GetByNomeAsync(string nome) =>
            await _set.FirstOrDefaultAsync(p =>
                p.Nome.ToLower() == nome.ToLower() && p.Ativo);
    }

    public class EquipamentoRepository : Repository<Equipamento>, IEquipamentoRepository
    {
        public EquipamentoRepository(AppDbContext db) : base(db) { }

        public async Task<Equipamento?> GetByNumeroSerieAsync(string numeroSerie) =>
            await _set.FirstOrDefaultAsync(e =>
                e.NumeroSerie == numeroSerie && e.Ativo);

        public async Task<IEnumerable<Equipamento>> GetByEmpresaAsync(int empresaId) =>
            await _set.Where(e => e.EmpresaId == empresaId && e.Ativo).ToListAsync();
    }

    public class ConfiguracaoRepository : Repository<Configuracao>, IConfiguracaoRepository
    {
        public ConfiguracaoRepository(AppDbContext db) : base(db) { }

        public async Task<Configuracao?> GetByEmpresaAsync(int empresaId) =>
            await _set.FirstOrDefaultAsync(c => c.EmpresaId == empresaId);
    }
}