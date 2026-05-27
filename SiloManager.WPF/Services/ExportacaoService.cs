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
        // ═══ EXCEL ═══
        public static void ExportarExcel(List<RelatorioLinhaDto> linhas, string caminho)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Medições");

            // Cabeçalho
            var cabecalhos = new[]
            {
                "Data/Hora", "Operador", "Produto", "Umidade (%)",
                "Status", "Secador", "Equipamento", "Silo Destino", "Intervalo", "Observação"
            };

            for (int i = 0; i < cabecalhos.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = cabecalhos[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Dados
            for (int i = 0; i < linhas.Count; i++)
            {
                var l = linhas[i];
                var row = i + 2;

                ws.Cell(row, 1).Value = l.DataHora.ToString("dd/MM/yyyy HH:mm:ss");
                ws.Cell(row, 2).Value = l.Operador;
                ws.Cell(row, 3).Value = l.Produto;
                ws.Cell(row, 4).Value = l.Umidade;
                ws.Cell(row, 5).Value = l.Status;
                ws.Cell(row, 6).Value = l.Secador;
                ws.Cell(row, 7).Value = l.Equipamento;
                ws.Cell(row, 8).Value = l.SiloDestino;
                ws.Cell(row, 9).Value = l.Intervalo;
                ws.Cell(row, 10).Value = l.Observacao;

                // Cor por status
                var corStatus = l.Status switch
                {
                    "Ideal" => XLColor.FromHtml("#C8E6C9"),
                    "Seco" => XLColor.FromHtml("#FFF9C4"),
                    "Atenção" => XLColor.FromHtml("#FFE0B2"),
                    "Crítico" => XLColor.FromHtml("#FFCDD2"),
                    _ => XLColor.White
                };
                ws.Row(row).Style.Fill.BackgroundColor = corStatus;

                // Retrabalho em negrito
                if (l.IsRetrabalho)
                    ws.Row(row).Style.Font.Bold = true;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(caminho);
        }

        // ═══ PDF ═══
        public static void ExportarPdf(
            List<RelatorioLinhaDto> linhas,
            string caminho,
            RelatorioFiltroDto filtro)
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
                                    .FontSize(16).Bold()
                                    .FontColor(Color.FromHex("#2E7D32"));
                                c.Item().Text($"Empresa: {SessaoUsuario.Atual?.NomeEmpresa}")
                                    .FontSize(10);
                                c.Item().Text(
                                    $"Período: {filtro.DataInicio:dd/MM/yyyy} até {filtro.DataFim:dd/MM/yyyy}")
                                    .FontSize(10);
                            });
                            row.ConstantItem(120).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(9).FontColor(Color.FromHex("#666666"));
                                c.Item().Text($"Total: {linhas.Count} medições")
                                    .FontSize(9);
                            });
                        });
                        col.Item().PaddingTop(4).LineHorizontal(1)
                            .LineColor(Color.FromHex("#2E7D32"));
                    });

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(80);  // Data/Hora
                            c.RelativeColumn(2);   // Operador
                            c.RelativeColumn(2);   // Produto
                            c.ConstantColumn(55);  // Umidade
                            c.ConstantColumn(50);  // Status
                            c.RelativeColumn(2);   // Secador
                            c.RelativeColumn(2);   // Equipamento
                            c.RelativeColumn(2);   // Silo
                            c.ConstantColumn(50);  // Intervalo
                            c.RelativeColumn(3);   // Observação
                        });

                        // Cabeçalho
                        static IContainer CabStyle(IContainer c) => c
                            .Background(Color.FromHex("#2E7D32"))
                            .Padding(4);

                        table.Header(h =>
                        {
                            foreach (var cab in new[]
                            {
                                "Data/Hora","Operador","Produto","Umidade",
                                "Status","Secador","Equipamento","Silo","Intervalo","Observação"
                            })
                            {
                                h.Cell().Element(CabStyle)
                                    .Text(cab).Bold().FontColor(Colors.White);
                            }
                        });

                        // Linhas
                        foreach (var (l, idx) in linhas.Select((l, i) => (l, i)))
                        {
                            var bg = idx % 2 == 0
                                ? Color.FromHex("#FFFFFF")
                                : Color.FromHex("#F5F5F5");

                            IContainer CellStyle(IContainer c) => c
                                .Background(bg).Padding(4);

                            var corStatus = l.Status switch
                            {
                                "Ideal" => Color.FromHex("#388E3C"),
                                "Seco" => Color.FromHex("#F57F17"),
                                "Atenção" => Color.FromHex("#E65100"),
                                "Crítico" => Color.FromHex("#C62828"),
                                _ => Color.FromHex("#000000")
                            };

                            table.Cell().Element(CellStyle)
                                .Text(l.DataHora.ToString("dd/MM HH:mm:ss"));
                            table.Cell().Element(CellStyle).Text(l.Operador);
                            table.Cell().Element(CellStyle).Text(l.Produto);
                            table.Cell().Element(CellStyle)
                                .Text($"{l.Umidade:F1}%").Bold();
                            table.Cell().Element(CellStyle)
                                .Text(l.Status).FontColor(corStatus).Bold();
                            table.Cell().Element(CellStyle).Text(l.Secador);
                            table.Cell().Element(CellStyle).Text(l.Equipamento);
                            table.Cell().Element(CellStyle).Text(l.SiloDestino);
                            table.Cell().Element(CellStyle).Text(l.Intervalo);
                            table.Cell().Element(CellStyle).Text(l.Observacao);
                        }
                    });

                    page.Footer().AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                });
            }).GeneratePdf(caminho);
        }
    }
}