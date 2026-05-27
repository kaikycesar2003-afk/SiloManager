namespace SiloManager.Domain.Entities
{
    public class Empresa
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string CNPJ { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;

        // Navegação
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
        public ICollection<Silo> Silos { get; set; } = new List<Silo>();
        public ICollection<Equipamento> Equipamentos { get; set; } = new List<Equipamento>();
        public ICollection<Medicao> Medicoes { get; set; } = new List<Medicao>();
        public ICollection<Secador> Secadores { get; set; } = new List<Secador>();
        public Configuracao? Configuracao { get; set; }
    }
}