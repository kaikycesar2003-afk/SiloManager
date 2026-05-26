using BCrypt.Net;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;

namespace SiloManager.Application.Services
{
    public class AuthService
    {
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IEmpresaRepository _empresaRepo;

        public AuthService(IUsuarioRepository usuarioRepo,
                           IEmpresaRepository empresaRepo)
        {
            _usuarioRepo = usuarioRepo;
            _empresaRepo = empresaRepo;
        }

        public async Task<bool> LoginAsync(int empresaId, string login, string senha)
        {
            var usuario = await _usuarioRepo.GetByLoginAsync(empresaId, login);
            if (usuario is null) return false;

            var senhaValida = BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash);
            if (!senhaValida) return false;

            var empresa = await _empresaRepo.GetByIdAsync(empresaId);

            SessaoUsuario.Iniciar(
                usuario.Id,
                usuario.Nome,
                empresa!.Id,
                empresa.Nome,
                usuario.Nivel);

            return true;
        }

        public void Logout() => SessaoUsuario.Encerrar();

        // Gera hash seguro para salvar no banco (usado no cadastro de usuário)
        public static string GerarHash(string senha) =>
            BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 12);
    }
}