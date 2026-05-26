namespace SiloManager.Domain.Entities
{
    public class Produto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        // Range de umidade — base do semáforo
        public double UmidadeMinima { get; set; }  // ex: 10.0
        public double UmidadeIdeal { get; set; }  // ex: 12.5
        public double UmidadeMaxima { get; set; }  // ex: 15.0
        public bool Ativo { get; set; } = true;

        // Navegação
        public ICollection<Silo> Silos { get; set; } = new List<Silo>();
        public ICollection<Medicao> Medicoes { get; set; } = new List<Medicao>();
    }
}