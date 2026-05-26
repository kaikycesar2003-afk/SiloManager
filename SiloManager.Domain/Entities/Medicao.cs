namespace SiloManager.Domain.Entities
{
    public class Medicao
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int UsuarioId { get; set; }
        public int ProdutoId { get; set; }
        public int EquipamentoId { get; set; }
        public int SiloDestinoId { get; set; }

        // Valores da leitura
        public double Umidade { get; set; }

        // Timestamps
        public DateTime DataHoraSistema { get; set; }  // relógio da máquina ← principal
        public DateTime? DataHoraEquipamento { get; set; } // vinda do serial ← conferência

        // Intervalo desde a medição anterior (em segundos)
        public int? IntervaloSegundos { get; set; }

        // Destino e status
        public bool IsRetrabalho { get; set; } = false;
        public string? Observacao { get; set; }

        // String bruta recebida da serial (auditoria)
        public string? DadosBrutosSerial { get; set; }

        // Navegação
        public Empresa Empresa { get; set; } = null!;
        public Usuario Usuario { get; set; } = null!;
        public Produto Produto { get; set; } = null!;
        public Equipamento Equipamento { get; set; } = null!;
        public Silo SiloDestino { get; set; } = null!;
    }
}