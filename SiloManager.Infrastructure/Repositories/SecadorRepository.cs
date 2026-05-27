using Microsoft.EntityFrameworkCore;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using SiloManager.Infrastructure.Data;

namespace SiloManager.Infrastructure.Repositories
{
    public class SecadorRepository : Repository<Secador>, ISecadorRepository
    {
        public SecadorRepository(AppDbContext db) : base(db) { }

        public async Task<IEnumerable<Secador>> GetByEmpresaAsync(int empresaId) =>
            await _set.Where(s => s.EmpresaId == empresaId && s.Ativo)
                      .OrderBy(s => s.Nome)
                      .ToListAsync();
    }
}