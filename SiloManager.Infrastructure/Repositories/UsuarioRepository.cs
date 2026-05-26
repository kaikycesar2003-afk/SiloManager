using Microsoft.EntityFrameworkCore;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using SiloManager.Infrastructure.Data;

namespace SiloManager.Infrastructure.Repositories
{
    public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(AppDbContext db) : base(db) { }

        public async Task<Usuario?> GetByLoginAsync(int empresaId, string login) =>
            await _set.FirstOrDefaultAsync(u =>
                u.EmpresaId == empresaId &&
                u.Login == login &&
                u.Ativo);

        public async Task<IEnumerable<Usuario>> GetByEmpresaAsync(int empresaId) =>
            await _set.Where(u => u.EmpresaId == empresaId && u.Ativo).ToListAsync();
    }
}