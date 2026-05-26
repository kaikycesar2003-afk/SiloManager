using SiloManager.Domain.Entities;

namespace SiloManager.Domain.Interfaces.Repositories
{
    public interface IEquipamentoRepository : IRepository<Equipamento>
    {
        // Busca equipamento pelo número de série (identificação automática via serial)
        Task<Equipamento?> GetByNumeroSerieAsync(string numeroSerie);

        // Lista equipamentos ativos de uma empresa
        Task<IEnumerable<Equipamento>> GetByEmpresaAsync(int empresaId);
    }
}