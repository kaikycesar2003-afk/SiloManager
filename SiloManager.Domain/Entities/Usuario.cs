using SiloManager.Domain.Enums;

namespace SiloManager.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public NivelAcesso Nivel { get; set; } = NivelAcesso.Operador;
        public bool Ativo { get; set; } = true;

        // Navegação
        public Empresa Empresa { get; set; } = null!;
        public ICollection<Medicao> Medicoes { get; set; } = new List<Medicao>();
    }
}