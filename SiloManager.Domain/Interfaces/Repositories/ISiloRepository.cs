using SiloManager.Domain.Entities;

namespace SiloManager.Domain.Interfaces.Repositories
{
    public interface ISiloRepository : IRepository<Silo>
    {
        // Retorna silos da empresa para o produto selecionado + silos de retrabalho
        Task<IEnumerable<Silo>> GetDestinosDisponiveisAsync(int empresaId, int produtoId);

        // Lista todos os silos ativos de uma empresa
        Task<IEnumerable<Silo>> GetByEmpresaAsync(int empresaId);
    }
}