using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SiloManager.Application.DTOs;
using SiloManager.Application.Enums;
using SiloManager.Application.Services;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApp = System.Windows.Application;

namespace SiloManager.WPF.ViewModels
{
    public partial class MedicaoViewModel : ObservableObject
    {
        private readonly IEquipamentoRepository _equipRepo;
        private readonly ISiloRepository _siloRepo;
        private readonly IProdutoRepository _produtoRepo;
        private readonly IMedicaoRepository _medicaoRepo;
        private readonly ISecadorRepository _secadorRepo;
        private readonly MedicaoService _medicaoService;
        private readonly SerialService _serialService;
        private readonly IConfiguracaoRepository _configRepo;

        private DispatcherTimer _timer = new();
        private int _intervaloSegundos = 900;
        private LeituraSerialDto? _leituraAtual;
        private bool _escutandoSerial;

        // ═══ Equipamento (fixo) ═══
        [ObservableProperty] private Equipamento? _equipamentoFixo;

        // ═══ Secador ═══
        [ObservableProperty] private Secador? _secadorSelecionado;
        [ObservableProperty] private bool _temMaisDeUmSecador;

        // ═══ Estado do botão CAPTURAR ═══
        [ObservableProperty] private bool _capturarHabilitado;
        [ObservableProperty] private string _statusCapturar = "Selecione um secador";

        // ═══ Leitura ═══
        [ObservableProperty] private string _umidadeDisplay = "-- %";
        [ObservableProperty] private string _statusDisplay = "Aguardando leitura...";
        [ObservableProperty] private string _produtoDetectado = "—";
        [ObservableProperty] private string _equipamentoDetectado = "—";
        [ObservableProperty] private string _horaSistema = "—";
        [ObservableProperty] private string _horaEquipamento = "—";
        [ObservableProperty] private Brush _corSemaforo = Brushes.Gray;
        [ObservableProperty] private bool _leituraRecebida;

        // ═══ Destino ═══
        [ObservableProperty] private Silo? _siloDestinoSelecionado;
        [ObservableProperty] private string _observacao = string.Empty;

        // ═══ Timer visual ═══
        [ObservableProperty] private string _timerDisplay = "00:00";
        [ObservableProperty] private string _statusTimer = "Selecione um secador";
        [ObservableProperty] private double _progressoTimer;
        [ObservableProperty] private Brush _corTimer = Brushes.Gray;

        public ObservableCollection<Equipamento> Equipamentos { get; } = new();
        public ObservableCollection<Silo> SilosDisponiveis { get; } = new();
        public ObservableCollection<Medicao> UltimasMedicoes { get; } = new();
        public ObservableCollection<Secador> Secadores { get; } = new();

        public MedicaoViewModel(
            IEquipamentoRepository equipRepo,
            ISiloRepository siloRepo,
            IProdutoRepository produtoRepo,
            IMedicaoRepository medicaoRepo,
            ISecadorRepository secadorRepo,
            MedicaoService medicaoService,
            SerialService serialService,
            IConfiguracaoRepository configRepo)
        {
            _equipRepo = equipRepo;
            _siloRepo = siloRepo;
            _produtoRepo = produtoRepo;
            _medicaoRepo = medicaoRepo;
            _secadorRepo = secadorRepo;
            _medicaoService = medicaoService;
            _serialService = serialService;
            _configRepo = configRepo;

            _serialService.LeituraRecebida += OnLeituraRecebida;
            _serialService.ErroRecebido += OnErroSerial;

            _ = InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            var config = await _configRepo.GetByEmpresaAsync(empresaId);
            _intervaloSegundos = config?.IntervaloMinimoSegundos ?? 900;

            // Carrega equipamentos e fixa o primeiro
            var equips = await _equipRepo.GetByEmpresaAsync(empresaId);
            Equipamentos.Clear();
            foreach (var e in equips) Equipamentos.Add(e);
            EquipamentoFixo = Equipamentos.FirstOrDefault();

            // Conecta automaticamente se tiver equipamento
            if (EquipamentoFixo != null)
            {
                try
                {
                    _serialService.Conectar(EquipamentoFixo.PortaCOM, EquipamentoFixo.BaudRate);
                }
                catch { /* porta não disponível no momento */ }
            }

            // Carrega secadores
            var secadores = await _secadorRepo.GetByEmpresaAsync(empresaId);
            Secadores.Clear();
            foreach (var s in secadores) Secadores.Add(s);

            TemMaisDeUmSecador = Secadores.Count > 1;

            // Auto-seleciona se só 1 secador
            if (Secadores.Count == 1)
                SecadorSelecionado = Secadores[0];

            await CarregarUltimasMedicoesAsync();
            IniciarTimer();
        }

        // Ao trocar de secador → reavalia timer imediatamente
        partial void OnSecadorSelecionadoChanged(Secador? value)
            => _ = AvaliarTimerSecadorAsync();

        private async Task AvaliarTimerSecadorAsync()
        {
            if (SecadorSelecionado == null)
            {
                CapturarHabilitado = false;
                StatusCapturar = "Selecione um secador";
                TimerDisplay = "--:--";
                StatusTimer = "Selecione um secador";
                CorTimer = Brushes.Gray;
                return;
            }

            var restante = await _medicaoService.VerificarTimerAsync(
                SessaoUsuario.Atual!.EmpresaId,
                SecadorSelecionado.Id);

            if (restante == null)
            {
                CapturarHabilitado = true;
                StatusCapturar = "✅ Pronto para capturar";
                TimerDisplay = "LIVRE";
                StatusTimer = $"✅ {SecadorSelecionado.Nome} liberado";
                ProgressoTimer = 100;
                CorTimer = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else
            {
                var ts = restante.Value;
                CapturarHabilitado = false;
                StatusCapturar = $"⏳ Aguarde {(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
                TimerDisplay = $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
                StatusTimer = $"⏳ {SecadorSelecionado.Nome} bloqueado";
                ProgressoTimer = 100 - (ts.TotalSeconds / _intervaloSegundos * 100);
                CorTimer = ts.TotalMinutes < 2
                    ? new SolidColorBrush(Color.FromRgb(245, 124, 0))
                    : new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
        }

        private void IniciarTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += async (s, e) =>
            {
                if (SessaoUsuario.Atual == null) { _timer.Stop(); return; }
                await AvaliarTimerSecadorAsync();
            };
            _timer.Start();
        }

        // ═══ Capturar — inicia escuta da serial ═══
        [RelayCommand(CanExecute = nameof(CapturarHabilitado))]
        private void Capturar()
        {
            _escutandoSerial = true;
            StatusDisplay = $"🎯 Aguardando leitura do {SecadorSelecionado?.Nome}...";
            LimparLeitura(manterSecador: true);
        }

        partial void OnCapturarHabilitadoChanged(bool value)
            => CapturarCommand.NotifyCanExecuteChanged();

        // ═══ Recebe leitura da serial ═══
        private async void OnLeituraRecebida(LeituraSerialDto dto)
        {
            // Ignora se não está no modo de captura
            if (!_escutandoSerial) return;

            await WpfApp.Current.Dispatcher.InvokeAsync(async () =>
            {
                _escutandoSerial = false;

                dto = await _medicaoService.EnriquecerLeituraAsync(dto);

                var equipEncontrado = await _equipRepo.GetByNumeroSerieAsync(dto.NumeroSerieEquipamento);
                if (equipEncontrado != null && equipEncontrado.Id != EquipamentoFixo?.Id)
                    EquipamentoFixo = equipEncontrado;

                _leituraAtual = dto;

                UmidadeDisplay = $"{dto.Umidade:F1}%";
                ProdutoDetectado = dto.NomeProduto;
                EquipamentoDetectado = EquipamentoFixo?.Nome
                                       ?? $"Não identificado ({dto.NumeroSerieEquipamento})";
                HoraSistema = DateTime.Now.ToString("HH:mm:ss");
                HoraEquipamento = dto.DataHoraEquipamento.ToString("HH:mm:ss");

                AtualizarSemaforo(dto.Status);
                await CarregarSilosAsync(dto.NomeProduto);

                LeituraRecebida = true;
            });
        }

        private void AtualizarSemaforo(StatusUmidade status)
        {
            (CorSemaforo, StatusDisplay) = status switch
            {
                StatusUmidade.Ideal => (new SolidColorBrush(Color.FromRgb(46, 125, 50)), "✅ IDEAL"),
                StatusUmidade.Critico => (new SolidColorBrush(Color.FromRgb(211, 47, 47)), "🔴 CRÍTICO — Muito seco"),
                StatusUmidade.Atencao => (new SolidColorBrush(Color.FromRgb(245, 124, 0)), "⚠️ ATENÇÃO — Úmido"),
                _ => (Brushes.Gray, "Aguardando leitura...")
            };
        }

        private async Task CarregarSilosAsync(string nomeProduto)
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;
            var produto = await _produtoRepo.GetByNomeAsync(nomeProduto);

            SilosDisponiveis.Clear();
            if (produto != null)
            {
                var silos = await _siloRepo.GetDestinosDisponiveisAsync(empresaId, produto.Id);
                foreach (var s in silos) SilosDisponiveis.Add(s);
            }

            // Atenção (úmido) → pré-seleciona Retrabalho
            // Crítico (seco) e Ideal → pré-seleciona primeiro Silo normal
            if (_leituraAtual?.Status == StatusUmidade.Atencao)
                SiloDestinoSelecionado = SilosDisponiveis.FirstOrDefault(s => s.IsRetrabalho);
            else
                SiloDestinoSelecionado = SilosDisponiveis.FirstOrDefault(s => !s.IsRetrabalho);
        }

        [RelayCommand]
        private async Task Confirmar()
        {
            if (_leituraAtual == null)
            {
                MessageBox.Show("Nenhuma leitura capturada.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SiloDestinoSelecionado == null)
            {
                MessageBox.Show("Selecione o destino do grão.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Secadores.Count > 0 && SecadorSelecionado == null)
            {
                MessageBox.Show("Selecione o secador.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var empresaId = SessaoUsuario.Atual!.EmpresaId;
            var produto = await _produtoRepo.GetByNomeAsync(_leituraAtual.NomeProduto);

            if (produto == null)
            {
                MessageBox.Show($"Produto '{_leituraAtual.NomeProduto}' não cadastrado.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _medicaoService.SalvarMedicaoAsync(
                empresaId: empresaId,
                produtoId: produto.Id,
                equipamentoId: EquipamentoFixo!.Id,
                siloDestinoId: SiloDestinoSelecionado.Id,
                umidade: _leituraAtual.Umidade,
                isRetrabalho: SiloDestinoSelecionado.IsRetrabalho,
                dadosBrutos: _leituraAtual.DadosBrutos,
                dataHoraEquipamento: _leituraAtual.DataHoraEquipamento,
                observacao: Observacao,
                secadorId: SecadorSelecionado?.Id);

            MessageBox.Show("✅ Medição registrada com sucesso!", "Sucesso",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // Reseta tudo e trava CAPTURAR
            LimparLeitura(manterSecador: true);
            await CarregarUltimasMedicoesAsync();
            await AvaliarTimerSecadorAsync();
        }

        private async Task CarregarUltimasMedicoesAsync()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;
            var lista = await _medicaoRepo.GetByFiltroAsync(
                empresaId, DateTime.Today, DateTime.Now);

            UltimasMedicoes.Clear();
            foreach (var m in lista.Take(10)) UltimasMedicoes.Add(m);
        }

        private void LimparLeitura(bool manterSecador = false)
        {
            _leituraAtual = null;
            _escutandoSerial = false;
            UmidadeDisplay = "-- %";
            StatusDisplay = "Aguardando leitura...";
            ProdutoDetectado = "—";
            EquipamentoDetectado = "—";
            HoraSistema = "—";
            HoraEquipamento = "—";
            CorSemaforo = Brushes.Gray;
            LeituraRecebida = false;
            Observacao = string.Empty;
            SiloDestinoSelecionado = null;
            SilosDisponiveis.Clear();

            if (!manterSecador)
                SecadorSelecionado = Secadores.Count == 1 ? Secadores[0] : null;
        }

        private void OnErroSerial(string erro) =>
            WpfApp.Current.Dispatcher.Invoke(() =>
                StatusDisplay = $"⚠️ Erro serial: {erro}");
    }
}