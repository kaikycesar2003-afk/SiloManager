using SiloManager.Domain.Entities;

namespace SiloManager.Domain.Interfaces.Repositories
{
    public interface IMedicaoRepository : IRepository<Medicao>
    {
        // Última medição de um silo (usado para calcular intervalo e checar timer)
        Task<Medicao?> GetUltimaPorSiloAsync(int empresaId, int siloId);

        // Última medição geral da empresa (para o timer global de 15 min)
        Task<Medicao?> GetUltimaGeralAsync(int empresaId);

        // Medições filtradas por período para relatórios
        Task<IEnumerable<Medicao>> GetByFiltroAsync(
            int empresaId,
            DateTime dataInicio,
            DateTime dataFim,
            int? produtoId = null,
            int? usuarioId = null,
            int? siloId = null);
    }
}