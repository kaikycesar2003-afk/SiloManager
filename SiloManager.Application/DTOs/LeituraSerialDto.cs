using SiloManager.Application.Enums;

namespace SiloManager.Application.DTOs
{
    public class LeituraSerialDto
    {
        // Campos extraídos da string do Gehaka
        public double Umidade { get; set; }  // campos[1]
        public string NomeProduto { get; set; } = string.Empty; // campos[7]
        public string ModeloEquipamento { get; set; } = string.Empty; // campos[10]
        public string NumeroSerieEquipamento { get; set; } = string.Empty; // campos[13]
        public DateTime DataHoraEquipamento { get; set; }  // campos[15] + campos[14]

        // String bruta recebida (para auditoria)
        public string DadosBrutos { get; set; } = string.Empty;

        // Resultado do semáforo (calculado após buscar o produto no banco)
        public StatusUmidade Status { get; set; } = StatusUmidade.Ideal;

        // Flag de conveniência
        public bool RetrabalhoSugerido => Status == StatusUmidade.Critico;
    }
}