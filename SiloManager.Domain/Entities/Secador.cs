namespace SiloManager.Domain.Entities
{
    public class Secador
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;

        // Navegação
        public Empresa Empresa { get; set; } = null!;
        public ICollection<Medicao> Medicoes { get; set; } = new List<Medicao>();
    }
}