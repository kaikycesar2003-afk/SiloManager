using SiloManager.Domain.Enums;

namespace SiloManager.Application.Session
{
    // Mantém os dados do usuário logado durante a sessão
    public class SessaoUsuario
    {
        public static SessaoUsuario? Atual { get; private set; }

        public int UsuarioId { get; private set; }
        public string Nome { get; private set; } = string.Empty;
        public int EmpresaId { get; private set; }
        public string NomeEmpresa { get; private set; } = string.Empty;
        public NivelAcesso Nivel { get; private set; }

        private SessaoUsuario() { }

        public static void Iniciar(int usuarioId, string nome,
                                   int empresaId, string nomeEmpresa,
                                   NivelAcesso nivel)
        {
            Atual = new SessaoUsuario
            {
                UsuarioId = usuarioId,
                Nome = nome,
                EmpresaId = empresaId,
                NomeEmpresa = nomeEmpresa,
                Nivel = nivel
            };
        }

        public static void Encerrar() => Atual = null;

        // Verifica se o usuário tem permissão mínima para uma ação
        public bool TemPermissao(NivelAcesso nivelMinimo) =>
            (int)Nivel >= (int)nivelMinimo;
    }
}