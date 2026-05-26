using SiloManager.Domain.Entities;

namespace SiloManager.Application.Services
{
    // DTOs para o relatório
    public class RelatorioFiltroDto
    {
        public DateTime DataInicio { get; set; } = DateTime.Today;
        public DateTime DataFim { get; set; } = DateTime.Now;
        public int? ProdutoId { get; set; }
        public int? SiloId { get; set; }
        public int? UsuarioId { get; set; }
    }

    public class RelatorioLinhaDto
    {
        public DateTime DataHora { get; set; }
        public string Operador { get; set; } = string.Empty;
        public string Produto { get; set; } = string.Empty;
        public double Umidade { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Equipamento { get; set; } = string.Empty;
        public string SiloDestino { get; set; } = string.Empty;
        public string Intervalo { get; set; } = string.Empty;
        public string Observacao { get; set; } = string.Empty;
        public bool IsRetrabalho { get; set; }
    }

    public static class RelatorioService
    {
        public static List<RelatorioLinhaDto> Converter(IEnumerable<Medicao> medicoes)
        {
            return medicoes.Select(m => new RelatorioLinhaDto
            {
                DataHora = m.DataHoraSistema,
                Operador = m.Usuario?.Nome ?? "—",
                Produto = m.Produto?.Nome ?? "—",
                Umidade = m.Umidade,
                Status = CalcularStatus(m),
                Equipamento = m.Equipamento?.Nome ?? "—",
                SiloDestino = m.SiloDestino?.Nome ?? "—",
                Intervalo = FormatarIntervalo(m.IntervaloSegundos),
                Observacao = m.Observacao ?? string.Empty,
                IsRetrabalho = m.IsRetrabalho
            }).ToList();
        }

        private static string CalcularStatus(Medicao m)
        {
            if (m.Produto == null) return "—";
            if (m.Umidade < m.Produto.UmidadeMinima) return "Seco";
            if (m.Umidade <= m.Produto.UmidadeIdeal) return "Ideal";
            if (m.Umidade <= m.Produto.UmidadeMaxima) return "Atenção";
            return "Crítico";
        }

        private static string FormatarIntervalo(int? segundos)
        {
            if (segundos == null) return "—";
            var ts = TimeSpan.FromSeconds(segundos.Value);
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}min";
            if (ts.TotalMinutes >= 1)
                return $"{(int)ts.TotalMinutes}min {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }
    }
}