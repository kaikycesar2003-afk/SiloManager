using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.Win32;
using SiloManager.Application.Services;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using SiloManager.WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SiloManager.WPF.ViewModels
{
    public partial class RelatorioViewModel : ObservableObject
    {
        private readonly IMedicaoRepository _medicaoRepo;
        private readonly IProdutoRepository _produtoRepo;
        private readonly ISiloRepository _siloRepo;
        private readonly IUsuarioRepository _usuarioRepo;

        // Filtros
        [ObservableProperty] private DateTime _dataInicio = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime _dataFim = DateTime.Today;
        [ObservableProperty] private Produto? _produtoFiltro;
        [ObservableProperty] private Silo? _siloFiltro;
        [ObservableProperty] private Usuario? _usuarioFiltro;

        // Totalizadores
        [ObservableProperty] private int _totalMedicoes;
        [ObservableProperty] private int _totalIdeais;
        [ObservableProperty] private int _totalAtencao;
        [ObservableProperty] private int _totalRetrabalho;

        public ObservableCollection<RelatorioLinhaDto> Linhas { get; } = new();
        public ObservableCollection<Produto> Produtos { get; } = new();
        public ObservableCollection<Silo> Silos { get; } = new();
        public ObservableCollection<Usuario> Usuarios { get; } = new();

        private List<RelatorioLinhaDto> _linhasCompletas = new();

        public RelatorioViewModel(
            IMedicaoRepository medicaoRepo,
            IProdutoRepository produtoRepo,
            ISiloRepository siloRepo,
            IUsuarioRepository usuarioRepo)
        {
            _medicaoRepo = medicaoRepo;
            _produtoRepo = produtoRepo;
            _siloRepo = siloRepo;
            _usuarioRepo = usuarioRepo;

            _ = CarregarFiltrosAsync();
        }

        private async Task CarregarFiltrosAsync()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            var prods = await _produtoRepo.GetAtivosAsync();
            var silos = await _siloRepo.GetByEmpresaAsync(empresaId);
            var users = await _usuarioRepo.GetByEmpresaAsync(empresaId);

            Produtos.Clear();
            foreach (var p in prods) Produtos.Add(p);

            Silos.Clear();
            foreach (var s in silos) Silos.Add(s);

            Usuarios.Clear();
            foreach (var u in users) Usuarios.Add(u);
        }

        [RelayCommand]
        private async Task Buscar()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            var medicoes = await _medicaoRepo.GetByFiltroAsync(
                empresaId: empresaId,
                dataInicio: DataInicio,
                dataFim: DataFim.Date.AddDays(1).AddSeconds(-1),
                produtoId: ProdutoFiltro?.Id,
                usuarioId: UsuarioFiltro?.Id,
                siloId: SiloFiltro?.Id);

            _linhasCompletas = RelatorioService.Converter(medicoes);

            AtualizarTabela();
            AtualizarTotalizadores();
        }

        private void AtualizarTabela()
        {
            Linhas.Clear();
            foreach (var l in _linhasCompletas) Linhas.Add(l);
        }

        private void AtualizarTotalizadores()
        {
            TotalMedicoes = _linhasCompletas.Count;
            TotalIdeais = _linhasCompletas.Count(l => l.Status == "Ideal");
            TotalAtencao = _linhasCompletas.Count(l => l.Status is "Atenção" or "Seco");
            TotalRetrabalho = _linhasCompletas.Count(l => l.IsRetrabalho);
        }

        [RelayCommand]
        private void ExportarExcel()
        {
            if (_linhasCompletas.Count == 0)
            {
                MessageBox.Show("Nenhum dado para exportar. Clique em Buscar primeiro.",
                    "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Salvar Excel",
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = $"Medicoes_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                ExportacaoService.ExportarExcel(_linhasCompletas, dlg.FileName);
                MessageBox.Show("✅ Excel exportado com sucesso!", "Sucesso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ExportarPdf()
        {
            if (_linhasCompletas.Count == 0)
            {
                MessageBox.Show("Nenhum dado para exportar. Clique em Buscar primeiro.",
                    "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Salvar PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"Medicoes_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var filtro = new RelatorioFiltroDto
                {
                    DataInicio = DataInicio,
                    DataFim = DataFim,
                    ProdutoId = ProdutoFiltro?.Id,
                    SiloId = SiloFiltro?.Id,
                    UsuarioId = UsuarioFiltro?.Id
                };

                ExportacaoService.ExportarPdf(_linhasCompletas, dlg.FileName, filtro);
                MessageBox.Show("✅ PDF exportado com sucesso!", "Sucesso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}