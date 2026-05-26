using SiloManager.Domain.Entities;

namespace SiloManager.Domain.Interfaces.Repositories
{
    public interface IEmpresaRepository : IRepository<Empresa>
    {
        Task<IEnumerable<Empresa>> GetAtivasAsync();
    }

    public interface IProdutoRepository : IRepository<Produto>
    {
        Task<IEnumerable<Produto>> GetAtivosAsync();

        // Busca produto pelo nome (vindo da string serial: "Soja", "Milho"...)
        Task<Produto?> GetByNomeAsync(string nome);
    }

    public interface IConfiguracaoRepository : IRepository<Configuracao>
    {
        Task<Configuracao?> GetByEmpresaAsync(int empresaId);
    }
}