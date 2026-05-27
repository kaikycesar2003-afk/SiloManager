using SiloManager.Domain.Entities;

namespace SiloManager.Domain.Interfaces.Repositories
{
    public interface ISecadorRepository : IRepository<Secador>
    {
        Task<IEnumerable<Secador>> GetByEmpresaAsync(int empresaId);
    }
}