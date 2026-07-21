using SiloManager.Domain.Entities;

namespace SiloManager.Application.Services
{
    public class RelatorioFiltroDto
    {
        public DateTime DataInicio { get; set; } = DateTime.Today;
        public DateTime DataFim { get; set; } = DateTime.Now;
        public int? ProdutoId { get; set; }
        public int? SiloId { get; set; }
        public int? UsuarioId { get; set; }
        public int? SecadorId { get; set; }          // restaurado — filtro direto por ID
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
        public int? SecadorId { get; set; }          // restaurado — groupby confiável
        public string SiloDestino { get; set; } = string.Empty;
        public string Intervalo { get; set; } = string.Empty;
        public string Observacao { get; set; } = string.Empty;
        public double? GrauSecador { get; set; }      // v1.2.0
        public bool IsRetrabalho { get; set; }
    }

    public static class RelatorioService
    {
        public static List<RelatorioLinhaDto> Converter(IEnumerable<Medicao> medicoes)
        {
            var lista = medicoes.OrderBy(m => m.DataHoraSistema).ToList();
            var resultado = new List<RelatorioLinhaDto>();

            // Rastreia a última DataHora por (SecadorId, Date)
            // Intervalo é sempre calculado contra a medição anterior DO MESMO SECADOR no mesmo dia
            var ultimaPorSecadorDia = new Dictionary<(int?, DateTime), DateTime>();

            foreach (var m in lista)
            {
                var dia = m.DataHoraSistema.Date;
                var key = (m.SecadorId, dia);

                string intervalo;
                if (ultimaPorSecadorDia.TryGetValue(key, out var ultimaDataDoSecador))
                {
                    var segundos = (int)(m.DataHoraSistema - ultimaDataDoSecador).TotalSeconds;
                    intervalo = FormatarIntervalo(segundos);
                }
                else
                {
                    // Primeira medição deste secador neste dia → sem intervalo
                    intervalo = "—";
                }

                ultimaPorSecadorDia[key] = m.DataHoraSistema;

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
                    SecadorId = m.SecadorId,
                    SiloDestino = m.SiloDestino?.Nome ?? "—",
                    Intervalo = intervalo,
                    Observacao = m.Observacao ?? string.Empty,
                    GrauSecador = m.GrauSecador,
                    IsRetrabalho = m.IsRetrabalho
                });
            }

            // Mantém a ordenação mais recente primeiro para exibição,
            // já que o cálculo acima depende de ordem cronológica ascendente
            return resultado.OrderByDescending(r => r.DataHora).ToList();
        }

        // Média de intervalo calculada por secador/dia
        public static string CalcularMediaIntervalo(List<RelatorioLinhaDto> linhas)
        {
            // Agrupa por (SecadorId, Data) — só grupos com >= 2 medições têm intervalo
            var grupos = linhas
                .GroupBy(l => (l.SecadorId, l.DataHora.Date))
                .Where(g => g.Count() >= 2)
                .ToList();

            if (grupos.Count == 0) return "—";

            var medias = new List<string>();

            foreach (var grupo in grupos)
            {
                var ordenadas = grupo.OrderBy(l => l.DataHora).ToList();
                var totalSegundos = (ordenadas.Last().DataHora - ordenadas.First().DataHora).TotalSeconds;
                var mediaSegundos = (int)(totalSegundos / (ordenadas.Count - 1));
                var nomeSecador = ordenadas.First().Secador;

                medias.Add($"{grupo.Key.Date:dd/MM} {nomeSecador}: {FormatarIntervalo(mediaSegundos)}");
            }

            return string.Join("  |  ", medias);
        }

        private static string CalcularStatus(Medicao m)
        {
            if (m.IsRetrabalho) return "Rodízio";
            if (m.Produto == null) return "—";
            if (m.Umidade < m.Produto.UmidadeMinima) return "Crítico";
            if (m.Umidade <= m.Produto.UmidadeMaxima) return "Ideal";
            return "Atenção";
        }

        private static string FormatarIntervalo(int segundos)
        {
            var ts = TimeSpan.FromSeconds(segundos);
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes:00}min";
            if (ts.TotalMinutes >= 1)
                return $"{(int)ts.TotalMinutes}min {ts.Seconds:00}s";
            return $"{ts.Seconds}s";
        }
    }
}