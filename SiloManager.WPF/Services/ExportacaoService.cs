using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SiloManager.Application.Services;
using SiloManager.Application.Session;

namespace SiloManager.WPF.Services
{
    public static class ExportacaoService
    {
        private const int TOTAL_COLUNAS = 11; // v1.2.0: adicionada coluna Grau (°C)

        // ═══ EXCEL ═══
        public static void ExportarExcel(
            List<RelatorioLinhaDto> linhas,
            string caminho,
            RelatorioFiltroDto filtro,
            string resumoRodizio = "")
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Medições");

            int linha = 1;

            // Cabeçalho empresa
            ws.Cell(linha, 1).Value = "Seedry — Relatório de Medições";
            ws.Cell(linha, 1).Style.Font.Bold = true;
            ws.Cell(linha, 1).Style.Font.FontSize = 14;
            ws.Cell(linha, 1).Style.Font.FontColor = XLColor.FromHtml("#2E7D32");
            ws.Range(linha, 1, linha, TOTAL_COLUNAS).Merge();
            linha++;

            ws.Cell(linha, 1).Value = $"Empresa: {SessaoUsuario.Atual?.NomeEmpresa}";
            ws.Cell(linha, 6).Value = $"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}";
            linha++;

            ws.Cell(linha, 1).Value = $"Período: {filtro.DataInicio:dd/MM/yyyy} até {filtro.DataFim:dd/MM/yyyy}";
            ws.Cell(linha, 6).Value = $"Total de medições: {linhas.Count}";
            linha++;

            // Filtros aplicados
            var filtrosTexto = new List<string>();
            if (filtro.ProdutoNome != null) filtrosTexto.Add($"Produto: {filtro.ProdutoNome}");
            if (filtro.SiloNome != null) filtrosTexto.Add($"Silo: {filtro.SiloNome}");
            if (filtro.SecadorNome != null) filtrosTexto.Add($"Secador: {filtro.SecadorNome}");
            if (filtro.UsuarioNome != null) filtrosTexto.Add($"Operador: {filtro.UsuarioNome}");
            if (filtro.StatusFiltro != null && filtro.StatusFiltro != "Todos")
                filtrosTexto.Add($"Status: {filtro.StatusFiltro}");

            if (filtrosTexto.Count > 0)
            {
                ws.Cell(linha, 1).Value = $"Filtros: {string.Join(" | ", filtrosTexto)}";
                ws.Range(linha, 1, linha, TOTAL_COLUNAS).Merge();
                linha++;
            }

            // Totalizadores
            ws.Cell(linha, 1).Value = $"Ideais: {linhas.Count(l => l.Status == "Ideal")}";
            ws.Cell(linha, 2).Value = $"Atenção: {linhas.Count(l => l.Status == "Atenção")}";
            ws.Cell(linha, 3).Value = $"Crítico: {linhas.Count(l => l.Status == "Crítico")}";
            ws.Cell(linha, 4).Value = $"Rodízio: {linhas.Count(l => l.IsRetrabalho)}";
            ws.Range(linha, 1, linha, 4).Style.Font.Bold = true;
            linha++;

            // Resumo Rodízio
            if (!string.IsNullOrEmpty(resumoRodizio))
            {
                linha++;
                ws.Cell(linha, 1).Value = "RESUMO RODÍZIO";
                ws.Cell(linha, 1).Style.Font.Bold = true;
                ws.Cell(linha, 1).Style.Font.FontColor = XLColor.FromHtml("#6A1B9A");
                ws.Range(linha, 1, linha, TOTAL_COLUNAS).Merge();
                linha++;

                foreach (var rodLine in resumoRodizio.Split('\n'))
                {
                    ws.Cell(linha, 1).Value = rodLine.Replace("🔄 ", "");
                    ws.Range(linha, 1, linha, TOTAL_COLUNAS).Merge();
                    ws.Cell(linha, 1).Style.Font.FontColor = XLColor.FromHtml("#4A148C");
                    linha++;
                }
            }

            linha++;

            // Cabeçalhos da tabela
            var cabecalhos = new[]
            {
                "Data/Hora", "Operador", "Produto", "Umidade (%)",
                "Status", "Secador", "Grau (°C)", "Equipamento",
                "Silo Destino", "Intervalo", "Observação"
            };

            for (int i = 0; i < cabecalhos.Length; i++)
            {
                var cell = ws.Cell(linha, i + 1);
                cell.Value = cabecalhos[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            linha++;

            // Dados
            foreach (var l in linhas)
            {
                ws.Cell(linha, 1).Value = l.DataHora.ToString("dd/MM/yyyy HH:mm:ss");
                ws.Cell(linha, 2).Value = l.Operador;
                ws.Cell(linha, 3).Value = l.Produto;
                ws.Cell(linha, 4).Value = l.Umidade;
                ws.Cell(linha, 5).Value = l.Status;
                ws.Cell(linha, 6).Value = l.Secador;
                ws.Cell(linha, 7).Value = l.GrauSecador?.ToString("F1") ?? "—";
                ws.Cell(linha, 8).Value = l.Equipamento;
                ws.Cell(linha, 9).Value = l.SiloDestino;
                ws.Cell(linha, 10).Value = l.Intervalo;
                ws.Cell(linha, 11).Value = l.Observacao;

                var corLinha = l.IsRetrabalho
                    ? XLColor.FromHtml("#EDE7F6")
                    : l.Status switch
                    {
                        "Ideal" => XLColor.FromHtml("#C8E6C9"),
                        "Atenção" => XLColor.FromHtml("#FFE0B2"),
                        "Crítico" => XLColor.FromHtml("#FFCDD2"),
                        _ => XLColor.White
                    };

                ws.Range(linha, 1, linha, TOTAL_COLUNAS).Style.Fill.BackgroundColor = corLinha;

                if (l.IsRetrabalho)
                    ws.Range(linha, 1, linha, TOTAL_COLUNAS).Style.Font.Bold = true;

                linha++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(caminho);
        }

        // ═══ PDF ═══
        public static void ExportarPdf(
            List<RelatorioLinhaDto> linhas,
            string caminho,
            RelatorioFiltroDto filtro,
            string resumoRodizio = "")
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Seedry — Relatório de Medições")
                                    .FontSize(15).Bold()
                                    .FontColor(Color.FromHex("#2E7D32"));
                                c.Item().Text($"Empresa: {SessaoUsuario.Atual?.NomeEmpresa}")
                                    .FontSize(10).FontColor(Color.FromHex("#555555"));
                                c.Item().Text($"Período: {filtro.DataInicio:dd/MM/yyyy} até {filtro.DataFim:dd/MM/yyyy}")
                                    .FontSize(9);
                            });
                            row.ConstantItem(160).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(9).FontColor(Color.FromHex("#777777"));
                                c.Item().Text($"Total: {linhas.Count} medições")
                                    .FontSize(9).Bold();
                            });
                        });
                        col.Item().PaddingTop(4).LineHorizontal(1)
                            .LineColor(Color.FromHex("#2E7D32"));
                    });

                    page.Content().Column(col =>
                    {
                        // Resumo Rodízio
                        if (!string.IsNullOrEmpty(resumoRodizio))
                        {
                            col.Item().PaddingVertical(6).Background(Color.FromHex("#F3E5F5"))
                                .Padding(8).Column(rc =>
                                {
                                    rc.Item().Text("RESUMO RODÍZIO")
                                        .Bold().FontSize(9).FontColor(Color.FromHex("#6A1B9A"));
                                    foreach (var r in resumoRodizio.Split('\n'))
                                        rc.Item().Text(r.Replace("🔄 ", "• "))
                                            .FontSize(8).FontColor(Color.FromHex("#4A148C"));
                                });
                        }

                        // Tabela
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            // Definição de colunas
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(60);   // Data/Hora
                                c.RelativeColumn(1.2f); // Operador
                                c.RelativeColumn(1.0f); // Produto
                                c.ConstantColumn(45);   // Umidade
                                c.ConstantColumn(50);   // Status
                                c.RelativeColumn(1.0f); // Secador
                                c.ConstantColumn(42);   // Grau °C
                                c.RelativeColumn(0.9f); // Equipamento
                                c.RelativeColumn(1.0f); // Silo Destino
                                c.ConstantColumn(42);   // Intervalo
                                c.RelativeColumn(1.2f); // Observação
                            });

                            // Cabeçalho
                            static IContainer HeaderStyle(IContainer c) =>
                                c.Background(Color.FromHex("#2E7D32")).Padding(4);

                            table.Header(h =>
                            {
                                foreach (var cab in new[]
                                {
                                    "Data/Hora","Operador","Produto","Umid.%",
                                    "Status","Secador","Grau°C","Equipamento",
                                    "Silo Destino","Interv.","Observação"
                                })
                                {
                                    h.Cell().Element(HeaderStyle)
                                        .Text(cab).Bold().FontSize(8)
                                        .FontColor(Colors.White);
                                }
                            });

                            // Linhas de dados
                            int idx = 0;
                            foreach (var l in linhas)
                            {
                                idx++;
                                var bg = l.IsRetrabalho
                                    ? Color.FromHex("#EDE7F6")
                                    : idx % 2 == 0
                                        ? Color.FromHex("#FFFFFF")
                                        : Color.FromHex("#F5F5F5");

                                IContainer CellStyle(IContainer c) => c.Background(bg).Padding(3);

                                var corStatus = l.Status switch
                                {
                                    "Ideal" => Color.FromHex("#2E7D32"),
                                    "Atenção" => Color.FromHex("#E65100"),
                                    "Crítico" => Color.FromHex("#C62828"),
                                    _ => Color.FromHex("#000000")
                                };

                                table.Cell().Element(CellStyle).Text(l.DataHora.ToString("dd/MM HH:mm")).FontSize(8);
                                table.Cell().Element(CellStyle).Text(l.Operador).FontSize(8);
                                table.Cell().Element(CellStyle).Text(l.Produto).FontSize(8);
                                table.Cell().Element(CellStyle).Text($"{l.Umidade:F1}%").Bold().FontSize(8);
                                table.Cell().Element(CellStyle).Text(l.Status).FontColor(corStatus).Bold().FontSize(8);
                                table.Cell().Element(CellStyle).Text(l.Secador).FontSize(8);
                                table.Cell().Element(CellStyle)
                                    .Text(l.GrauSecador.HasValue ? $"{l.GrauSecador.Value:F1}°C" : "—")
                                    .FontSize(8);
                                table.Cell().Element(CellStyle).Text(l.Equipamento).FontSize(8);
                                table.Cell().Element(CellStyle).Text(l.SiloDestino).FontSize(8);
                                table.Cell().Element(CellStyle).Text(l.Intervalo).FontSize(8);
                                table.Cell().Element(CellStyle).Text(l.Observacao).FontSize(8);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ").FontSize(8);
                        x.CurrentPageNumber().FontSize(8);
                        x.Span(" de ").FontSize(8);
                        x.TotalPages().FontSize(8);
                    });
                });
            }).GeneratePdf(caminho);
        }
    }
}