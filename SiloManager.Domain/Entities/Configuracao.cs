namespace SiloManager.Domain.Entities
{
    public class Configuracao
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }

        // Timer de segurança: tempo mínimo entre medições (padrão: 900s = 15min)
        public int IntervaloMinimoSegundos { get; set; } = 900;

        // Navegação
        public Empresa Empresa { get; set; } = null!;
    }
}