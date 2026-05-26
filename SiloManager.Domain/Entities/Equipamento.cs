namespace SiloManager.Domain.Entities
{
    public class Equipamento
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nome { get; set; } = string.Empty; // nome amigável
        public string Modelo { get; set; } = string.Empty; // ex: "G2000"
        public string NumeroSerie { get; set; } = string.Empty; // ex: "23012786001080"
        public string PortaCOM { get; set; } = string.Empty; // ex: "COM3"
        public int BaudRate { get; set; } = 9600;
        public bool Ativo { get; set; } = true;

        // Navegação
        public Empresa Empresa { get; set; } = null!;
        public ICollection<Medicao> Medicoes { get; set; } = new List<Medicao>();
    }
}