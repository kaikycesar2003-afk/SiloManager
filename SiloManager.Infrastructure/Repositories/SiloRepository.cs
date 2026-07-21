using Microsoft.EntityFrameworkCore;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using SiloManager.Infrastructure.Data;

namespace SiloManager.Infrastructure.Repositories
{
    public class SiloRepository : Repository<Silo>, ISiloRepository
    {
        public SiloRepository(AppDbContext db) : base(db) { }

        public async Task<IEnumerable<Silo>> GetDestinosDisponiveisAsync(int empresaId, int produtoId) =>
            await _set.Where(s =>
                s.EmpresaId == empresaId &&
                s.Ativo &&
                (s.ProdutoId == produtoId   // silos do produto detectado
                 || s.IsRetrabalho          // silos de retrabalho/rodízio
                 || s.ProdutoId == null))   // silos sem produto (ex: Carregamento)
            .OrderBy(s => s.IsRetrabalho)   // normais primeiro, retrabalho depois
            .ThenBy(s => s.Nome)
            .ToListAsync();

        public async Task<IEnumerable<Silo>> GetByEmpresaAsync(int empresaId) =>
            await _set.Where(s => s.EmpresaId == empresaId && s.Ativo)
                      .OrderBy(s => s.Nome)
                      .ToListAsync();
    }
}