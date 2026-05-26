namespace SiloManager.Domain.Entities
{
    public class Silo
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int? ProdutoId { get; set; }   // null = aceita qualquer grão
        public string Nome { get; set; } = string.Empty; // "Silo A", "Silo B"...
        public bool IsRetrabalho { get; set; } = false;
        public bool Ativo { get; set; } = true;

        // Navegação
        public Empresa Empresa { get; set; } = null!;
        public Produto? Produto { get; set; }
        public ICollection<Medicao> Medicoes { get; set; } = new List<Medicao>();
    }
}