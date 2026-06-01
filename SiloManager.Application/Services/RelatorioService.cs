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
        public string? ProdutoNome { get; set; }
        public string? SiloNome { get; set; }
        public string? SecadorNome { get; set; }
        public string? UsuarioNome { get; set; }
        public string? StatusFiltro { get; set; }
    }

    public class RelatorioLinhaDto
    {
        public int Id { get; set; }
        public DateTime DataHora { get; set; }
        public string Operador { get; set; } = string.Empty;
        public string Produto { get; set; } = string.Empty;
        public double Umidade { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Equipamento { get; set; } = string.Empty;
        public string Secador { get; set; } = string.Empty;
        public string SiloDestino { get; set; } = string.Empty;
        public string Intervalo { get; set; } = string.Empty;
        public string Observacao { get; set; } = string.Empty;
        public bool IsRetrabalho { get; set; }
    }

    public static class RelatorioService
    {
        public static List<RelatorioLinhaDto> Converter(IEnumerable<Medicao> medicoes)
        {
            // Ordena do mais antigo para o mais recente (primeiros do dia no topo)
            var lista = medicoes.OrderBy(m => m.DataHoraSistema).ToList();

            var resultado = new List<RelatorioLinhaDto>();

            // Agrupa por dia para calcular intervalo correto
            var porDia = lista.GroupBy(m => m.DataHoraSistema.Date);

            foreach (var dia in porDia)
            {
                var medicoesDia = dia.OrderBy(m => m.DataHoraSistema).ToList();

                for (int i = 0; i < medicoesDia.Count; i++)
                {
                    var m = medicoesDia[i];

                    // Primeira medição do dia → intervalo = 0
                    string intervalo = i == 0
                        ? "—"
                        : FormatarIntervalo((int)(m.DataHoraSistema - medicoesDia[i - 1].DataHoraSistema).TotalSeconds);

                    resultado.Add(new RelatorioLinhaDto
                    {
                        Id = m.Id,
                        DataHora = m.DataHoraSistema,
                        Operador = m.Usuario?.Nome ?? "—",
                        Produto = m.Produto?.Nome ?? "—",
                        Umidade = m.Umidade,
                        Status = CalcularStatus(m),
                        Equipamento = m.Equipamento?.Nome ?? "—",
                        Secador = m.Secador?.Nome ?? "—",
                        SiloDestino = m.SiloDestino?.Nome ?? "—",
                        Intervalo = intervalo,
                        Observacao = m.Observacao ?? string.Empty,
                        IsRetrabalho = m.IsRetrabalho
                    });
                }
            }

            return resultado;
        }

        // Calcula a média de intervalo do dia (ignora a primeira medição)
        public static string CalcularMediaIntervalo(List<RelatorioLinhaDto> linhas)
        {
            var comIntervalo = linhas
                .Where(l => l.Intervalo != "—" && l.Intervalo != string.Empty)
                .ToList();

            if (comIntervalo.Count == 0) return "—";

            // Converte intervalos de volta para segundos para calcular média
            var porDia = linhas.GroupBy(l => l.DataHora.Date);
            var medias = new List<string>();

            foreach (var dia in porDia)
            {
                var medicoesDia = dia.OrderBy(l => l.DataHora).ToList();
                if (medicoesDia.Count < 2) continue;

                var totalSegundos = (medicoesDia.Last().DataHora - medicoesDia.First().DataHora).TotalSeconds;
                var mediaSegundos = totalSegundos / (medicoesDia.Count - 1);

                medias.Add($"{dia.Key:dd/MM}: {FormatarIntervalo((int)mediaSegundos)}");
            }

            return medias.Count > 0 ? string.Join(" | ", medias) : "—";
        }

        private static string CalcularStatus(Medicao m)
        {
            if (m.Produto == null) return "—";
            if (m.Umidade < m.Produto.UmidadeMinima) return "Crítico";
            if (m.Umidade <= m.Produto.UmidadeMaxima) return "Ideal";
            return "Atenção";
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