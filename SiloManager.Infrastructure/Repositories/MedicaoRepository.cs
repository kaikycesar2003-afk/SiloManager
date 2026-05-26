using Microsoft.EntityFrameworkCore;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using SiloManager.Infrastructure.Data;

namespace SiloManager.Infrastructure.Repositories
{
    public class MedicaoRepository : Repository<Medicao>, IMedicaoRepository
    {
        public MedicaoRepository(AppDbContext db) : base(db) { }

        public async Task<Medicao?> GetUltimaPorSiloAsync(int empresaId, int siloId) =>
            await _set
                .Where(m => m.EmpresaId == empresaId && m.SiloDestinoId == siloId)
                .OrderByDescending(m => m.DataHoraSistema)
                .FirstOrDefaultAsync();

        public async Task<Medicao?> GetUltimaGeralAsync(int empresaId) =>
            await _set
                .Where(m => m.EmpresaId == empresaId)
                .OrderByDescending(m => m.DataHoraSistema)
                .FirstOrDefaultAsync();

        public async Task<IEnumerable<Medicao>> GetByFiltroAsync(
            int empresaId, DateTime dataInicio, DateTime dataFim,
            int? produtoId = null, int? usuarioId = null, int? siloId = null) =>
            await _set
                .Include(m => m.Usuario)
                .Include(m => m.Produto)
                .Include(m => m.SiloDestino)
                .Include(m => m.Equipamento)
                .Where(m =>
                    m.EmpresaId == empresaId &&
                    m.DataHoraSistema >= dataInicio &&
                    m.DataHoraSistema <= dataFim &&
                    (produtoId == null || m.ProdutoId == produtoId) &&
                    (usuarioId == null || m.UsuarioId == usuarioId) &&
                    (siloId == null || m.SiloDestinoId == siloId))
                .OrderByDescending(m => m.DataHoraSistema)
                .ToListAsync();
    }
}