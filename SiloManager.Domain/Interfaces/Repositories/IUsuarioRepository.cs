using SiloManager.Domain.Entities;

namespace SiloManager.Domain.Interfaces.Repositories
{
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        // Busca usuário pelo login dentro de uma empresa (usado no login)
        Task<Usuario?> GetByLoginAsync(int empresaId, string login);

        // Lista todos os usuários ativos de uma empresa
        Task<IEnumerable<Usuario>> GetByEmpresaAsync(int empresaId);
    }
}