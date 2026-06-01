using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SiloManager.Application.Services;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using SiloManager.WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace SiloManager.WPF.ViewModels
{
    public partial class RelatorioViewModel : ObservableObject
    {
        private readonly IMedicaoRepository _medicaoRepo;
        private readonly IProdutoRepository _produtoRepo;
        private readonly ISiloRepository _siloRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly ISecadorRepository _secadorRepo;

        [ObservableProperty] private DateTime _dataInicio = DateTime.Today;
        [ObservableProperty] private DateTime _dataFim = DateTime.Today;
        [ObservableProperty] private Produto? _produtoFiltro;
        [ObservableProperty] private Silo? _siloFiltro;
        [ObservableProperty] private Usuario? _usuarioFiltro;
        [ObservableProperty] private Secador? _secadorFiltro;
        [ObservableProperty] private string _statusFiltro = "Todos";

        [ObservableProperty] private int _totalMedicoes;
        [ObservableProperty] private int _totalIdeais;
        [ObservableProperty] private int _totalAtencao;
        [ObservableProperty] private int _totalCritico;
        [ObservableProperty] private int _totalRodizio;
        [ObservableProperty] private string _mediaIntervalo = "—";
        [ObservableProperty] private string _resumoRodizio = string.Empty;
        [ObservableProperty] private bool _temResumoRodizio;
        [ObservableProperty] private bool _resumoExpandido = true;
        [ObservableProperty] private bool _temProdutoFiltro;
        [ObservableProperty] private bool _temSiloFiltro;
        [ObservableProperty] private bool _temSecadorFiltro;
        [ObservableProperty] private bool _temOperadorFiltro;

        partial void OnProdutoFiltroChanged(Produto? value) => TemProdutoFiltro = value != null;
        partial void OnSiloFiltroChanged(Silo? value) => TemSiloFiltro = value != null;
        partial void OnSecadorFiltroChanged(Secador? value) => TemSecadorFiltro = value != null;
        partial void OnUsuarioFiltroChanged(Usuario? value) => TemOperadorFiltro = value != null;

        [RelayCommand] private void ToggleResumo() => ResumoExpandido = !ResumoExpandido;

        public ObservableCollection<RelatorioLinhaDto> Linhas { get; } = new();
        public ObservableCollection<Produto> Produtos { get; } = new();
        public ObservableCollection<Silo> Silos { get; } = new();
        public ObservableCollection<Usuario> Usuarios { get; } = new();
        public ObservableCollection<Secador> Secadores { get; } = new();

        public IEnumerable<string> OpcoesStatus => new[]
            { "Todos", "Ideal", "Atenção", "Crítico", "Rodízio" };

        private List<RelatorioLinhaDto> _linhasCompletas = new();

        // ═══ Admin — Edição e Exclusão ═══
        [ObservableProperty] private bool _isAdmin;

        public RelatorioViewModel(
            IMedicaoRepository medicaoRepo,
            IProdutoRepository produtoRepo,
            ISiloRepository siloRepo,
            IUsuarioRepository usuarioRepo,
            ISecadorRepository secadorRepo)
        {
            _medicaoRepo = medicaoRepo;
            _produtoRepo = produtoRepo;
            _siloRepo = siloRepo;
            _usuarioRepo = usuarioRepo;
            _secadorRepo = secadorRepo;

            IsAdmin = SessaoUsuario.Atual?.Nivel == SiloManager.Domain.Enums.NivelAcesso.Administrador;

            _ = CarregarFiltrosAsync();
        }

        private async Task CarregarFiltrosAsync()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;
            var prods = await _produtoRepo.GetAtivosAsync();
            var silos = await _siloRepo.GetByEmpresaAsync(empresaId);
            var users = await _usuarioRepo.GetByEmpresaAsync(empresaId);
            var secadores = await _secadorRepo.GetByEmpresaAsync(empresaId);

            Produtos.Clear(); foreach (var p in prods) Produtos.Add(p);
            Silos.Clear(); foreach (var s in silos) Silos.Add(s);
            Usuarios.Clear(); foreach (var u in users) Usuarios.Add(u);
            Secadores.Clear(); foreach (var s in secadores) Secadores.Add(s);
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

            if (SecadorFiltro != null)
                _linhasCompletas = _linhasCompletas
                    .Where(l => l.Secador == SecadorFiltro.Nome).ToList();

            AtualizarTabela();
            AtualizarTotalizadores();
            AtualizarResumoRodizio();
        }

        // Limpa filtros opcionais
        [RelayCommand] private void LimparSecador() => SecadorFiltro = null;
        [RelayCommand] private void LimparOperador() => UsuarioFiltro = null;
        [RelayCommand] private void LimparProduto() => ProdutoFiltro = null;
        [RelayCommand] private void LimparSilo() => SiloFiltro = null;

        private void AtualizarTabela()
        {
            var linhas = StatusFiltro switch
            {
                "Ideal" => _linhasCompletas.Where(l => l.Status == "Ideal").ToList(),
                "Atenção" => _linhasCompletas.Where(l => l.Status == "Atenção").ToList(),
                "Crítico" => _linhasCompletas.Where(l => l.Status == "Crítico").ToList(),
                "Rodízio" => _linhasCompletas.Where(l => l.IsRetrabalho).ToList(),
                _ => _linhasCompletas
            };

            Linhas.Clear();
            foreach (var l in linhas) Linhas.Add(l);
        }

        private void AtualizarTotalizadores()
        {
            TotalMedicoes = _linhasCompletas.Count;
            TotalIdeais = _linhasCompletas.Count(l => l.Status == "Ideal");
            TotalAtencao = _linhasCompletas.Count(l => l.Status == "Atenção");
            TotalCritico = _linhasCompletas.Count(l => l.Status == "Crítico");
            TotalRodizio = _linhasCompletas.Count(l => l.IsRetrabalho);
            MediaIntervalo = RelatorioService.CalcularMediaIntervalo(_linhasCompletas);
        }

        private void AtualizarResumoRodizio()
        {
            var rodizios = _linhasCompletas
                .Where(l => l.IsRetrabalho)
                .OrderBy(l => l.DataHora)
                .ToList();

            if (rodizios.Count == 0)
            {
                ResumoRodizio = string.Empty;
                TemResumoRodizio = false;
                return;
            }

            // Agrupa por Secador
            var resumos = new List<string>();

            foreach (var grupo in rodizios.GroupBy(l => l.Secador))
            {
                var lista = grupo.OrderBy(l => l.DataHora).ToList();
                var inicio = lista.First().DataHora;
                var fim = lista.Last().DataHora;

                // Busca quando saiu do rodízio (próxima medição do secador que não é rodízio)
                var saida = _linhasCompletas
                    .Where(l => l.Secador == grupo.Key && !l.IsRetrabalho && l.DataHora > fim)
                    .OrderBy(l => l.DataHora)
                    .FirstOrDefault();

                var secadorLabel = grupo.Key != "—" ? grupo.Key : "Sem secador";

                if (saida != null)
                {
                    var duracao = saida.DataHora - inicio;
                    resumos.Add(
                        $"🔄 {secadorLabel}: {inicio:HH:mm} → {saida.DataHora:HH:mm} " +
                        $"({FormatarDuracao(duracao)}) — {lista.Count} medições");
                }
                else
                {
                    var duracao = DateTime.Now - inicio;
                    resumos.Add(
                        $"🔄 {secadorLabel}: {inicio:HH:mm} → ainda em rodízio " +
                        $"({FormatarDuracao(duracao)}) — {lista.Count} medições");
                }
            }

            ResumoRodizio = string.Join("\n", resumos);
            TemResumoRodizio = resumos.Count > 0;
        }

        private static string FormatarDuracao(TimeSpan ts)
        {
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}min";
            return $"{(int)ts.TotalMinutes}min";
        }

        partial void OnStatusFiltroChanged(string value) => AtualizarTabela();

        [RelayCommand]
        private async Task EditarMedicao(RelatorioLinhaDto dto)
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;
            var medicao = await _medicaoRepo.GetByIdAsync(dto.Id);
            if (medicao == null) return;

            var produtos = await _produtoRepo.GetAtivosAsync();
            var silos = await _siloRepo.GetByEmpresaAsync(empresaId);
            var secadores = await _secadorRepo.GetByEmpresaAsync(empresaId);

            var produtoAtual = produtos.FirstOrDefault(p => p.Nome == dto.Produto);
            var siloAtual = silos.FirstOrDefault(s => s.Nome == dto.SiloDestino);
            var secadorAtual = secadores.FirstOrDefault(s => s.Nome == dto.Secador);

            var janela = new Views.EdicaoMedicaoWindow(
                dto.Umidade, dto.Observacao,
                produtos, silos, secadores,
                produtoAtual, siloAtual, secadorAtual);

            janela.Owner = WpfApp.Current.MainWindow;
            janela.ShowDialog();

            if (!janela.Confirmado) return;

            medicao.Umidade = janela.UmidadeEditada ?? medicao.Umidade;
            medicao.ProdutoId = janela.ProdutoEditado?.Id ?? medicao.ProdutoId;
            medicao.SiloDestinoId = janela.SiloEditado?.Id ?? medicao.SiloDestinoId;
            medicao.SecadorId = janela.SecadorEditado?.Id;
            medicao.IsRetrabalho = janela.SiloEditado?.IsRetrabalho ?? medicao.IsRetrabalho;
            medicao.Observacao = janela.ObservacaoEditada;

            await _medicaoRepo.UpdateAsync(medicao);
            await _medicaoRepo.SaveChangesAsync();

            await Buscar();
        }

        [RelayCommand]
        private async Task ExcluirMedicao(RelatorioLinhaDto dto)
        {
            var r = MessageBox.Show(
                $"Excluir medição de {dto.DataHora:dd/MM HH:mm} — {dto.Produto} {dto.Umidade:F1}%?\n\nEsta ação não pode ser desfeita.",
                "Confirmar exclusão",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (r != MessageBoxResult.Yes) return;

            var medicao = await _medicaoRepo.GetByIdAsync(dto.Id);
            if (medicao == null) return;

            await _medicaoRepo.DeleteAsync(medicao);
            await _medicaoRepo.SaveChangesAsync();

            await Buscar();
        }

        private RelatorioFiltroDto MontarFiltro() => new()
        {
            DataInicio = DataInicio,
            DataFim = DataFim,
            ProdutoId = ProdutoFiltro?.Id,
            SiloId = SiloFiltro?.Id,
            UsuarioId = UsuarioFiltro?.Id,
            ProdutoNome = ProdutoFiltro?.Nome,
            SiloNome = SiloFiltro?.Nome,
            SecadorNome = SecadorFiltro?.Nome,
            UsuarioNome = UsuarioFiltro?.Nome,
            StatusFiltro = StatusFiltro != "Todos" ? StatusFiltro : null
        };

        [RelayCommand]
        private void ExportarExcel()
        {
            if (_linhasCompletas.Count == 0)
            {
                MessageBox.Show("Nenhum dado. Clique em Buscar primeiro.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
                ExportacaoService.ExportarExcel(Linhas.ToList(), dlg.FileName, MontarFiltro(), ResumoRodizio);
                MessageBox.Show("✅ Excel exportado!", "Sucesso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ExportarPdf()
        {
            if (_linhasCompletas.Count == 0)
            {
                MessageBox.Show("Nenhum dado. Clique em Buscar primeiro.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
                ExportacaoService.ExportarPdf(Linhas.ToList(), dlg.FileName, MontarFiltro(), ResumoRodizio);
                MessageBox.Show("✅ PDF exportado!", "Sucesso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}