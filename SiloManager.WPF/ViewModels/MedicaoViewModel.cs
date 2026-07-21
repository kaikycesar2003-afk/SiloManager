using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using SiloManager.Application.DTOs;
using SiloManager.Application.Enums;
using SiloManager.Application.Services;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using System.Collections.ObjectModel;
using System.Globalization;
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

        // ─── Cores fixas ────────────────────────────────────────────────────
        private static readonly Brush CorVerde = new SolidColorBrush(Color.FromRgb(46, 125, 50));
        private static readonly Brush CorLaranja = new SolidColorBrush(Color.FromRgb(245, 124, 0));
        private static readonly Brush CorVermelha = new SolidColorBrush(Color.FromRgb(211, 47, 47));

        // ═══ Equipamento (fixo) ═══
        [ObservableProperty] private Equipamento? _equipamentoFixo;

        // ═══ Secador ═══
        [ObservableProperty] private Secador? _secadorSelecionado;
        [ObservableProperty] private bool _temMaisDeUmSecador;

        // ═══ Estado do botão CAPTURAR ═══
        [ObservableProperty] private bool _capturarHabilitado;
        [ObservableProperty] private bool _aguardandoCaptura;
        [ObservableProperty] private string _statusCapturar = "Selecione um secador";

        // ═══ Texto e ícone do botão ═══
        [ObservableProperty] private string _textoBotaoCapturar = "CAPTURAR";
        [ObservableProperty] private PackIconKind _iconeBotaoCapturar = PackIconKind.Radar;

        // ═══ Controle do ComboBox de Secador ═══
        [ObservableProperty] private bool _secadorHabilitado = true;

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

        // ═══ Grau do Secador (v1.2.0) — obrigatório na confirmação ═══
        [ObservableProperty] private string _grauSecadorTexto = string.Empty;

        // ═══ Timer visual ═══
        [ObservableProperty] private string _timerDisplay = "00:00";
        [ObservableProperty] private string _statusTimer = "Selecione um secador";
        [ObservableProperty] private double _progressoTimer;
        [ObservableProperty] private Brush _corTimer = Brushes.Gray;

        public ObservableCollection<Equipamento> Equipamentos { get; } = new();
        public ObservableCollection<Silo> SilosDisponiveis { get; } = new();
        public ObservableCollection<Medicao> UltimasMedicoes { get; } = new();
        public ObservableCollection<Secador> Secadores { get; } = new();

        // CanExecute: habilitado sempre que secador estiver selecionado OU aguardando
        // (timer não bloqueia mais o botão — exibe aviso quando antecipado)
        private bool PodeCapturar => SecadorSelecionado != null || AguardandoCaptura;

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

        // ═══════════════════════════════════════════════════════════════════
        //  INICIALIZAÇÃO
        // ═══════════════════════════════════════════════════════════════════

        private async Task InicializarAsync()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            var config = await _configRepo.GetByEmpresaAsync(empresaId);
            _intervaloSegundos = config?.IntervaloMinimoSegundos ?? 900;

            var equips = await _equipRepo.GetByEmpresaAsync(empresaId);
            Equipamentos.Clear();
            foreach (var e in equips) Equipamentos.Add(e);
            EquipamentoFixo = Equipamentos.FirstOrDefault();

            if (EquipamentoFixo != null)
            {
                try { _serialService.Conectar(EquipamentoFixo.PortaCOM, EquipamentoFixo.BaudRate); }
                catch { /* porta não disponível */ }
            }

            var secadores = await _secadorRepo.GetByEmpresaAsync(empresaId);
            Secadores.Clear();
            foreach (var s in secadores) Secadores.Add(s);

            TemMaisDeUmSecador = Secadores.Count > 1;

            if (Secadores.Count == 1)
                SecadorSelecionado = Secadores[0];

            await CarregarUltimasMedicoesAsync();
            IniciarTimer();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  TIMER DO SECADOR
        // ═══════════════════════════════════════════════════════════════════

        partial void OnSecadorSelecionadoChanged(Secador? value)
        {
            _ = AvaliarTimerSecadorAsync();
            CapturarCommand.NotifyCanExecuteChanged(); // secador agora faz parte do CanExecute
        }

        private async Task AvaliarTimerSecadorAsync()
        {
            if (AguardandoCaptura) return;

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
                CorTimer = CorVerde;
            }
            else
            {
                var ts = restante.Value;
                CapturarHabilitado = false;
                StatusCapturar = $"⏳ Aguarde {(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
                TimerDisplay = $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
                StatusTimer = $"⏳ {SecadorSelecionado.Nome} bloqueado";
                ProgressoTimer = 100 - (ts.TotalSeconds / _intervaloSegundos * 100);
                CorTimer = ts.TotalMinutes < 2 ? CorLaranja : CorVermelha;
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

        partial void OnCapturarHabilitadoChanged(bool value)
            => CapturarCommand.NotifyCanExecuteChanged();

        partial void OnAguardandoCapturaChanged(bool value)
            => CapturarCommand.NotifyCanExecuteChanged();

        // ═══════════════════════════════════════════════════════════════════
        //  COMANDO: CAPTURAR / DESCONECTAR
        // ═══════════════════════════════════════════════════════════════════

        [RelayCommand(CanExecute = nameof(PodeCapturar))]
        private void Capturar()
        {
            // ── Modo DESCONECTAR ────────────────────────────────────────────
            if (AguardandoCaptura)
            {
                AguardandoCaptura = false;
                _escutandoSerial = false;
                StatusDisplay = "Aguardando leitura...";
                TextoBotaoCapturar = "CAPTURAR";
                IconeBotaoCapturar = PackIconKind.Radar;
                SecadorHabilitado = true;
                return;
            }

            // ── Leitura pendente: alerta antes de descartar ─────────────────
            if (LeituraRecebida)
            {
                var r = MessageBox.Show(
                    "Há uma leitura aguardando confirmação.\n\n" +
                    "Iniciar nova captura irá descartar a leitura atual.\n\n" +
                    "Deseja continuar?",
                    "Leitura pendente",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (r != MessageBoxResult.Yes) return;

                LimparLeitura(manterSecador: true);
            }

            // ── Timer não concluído: confirmação do operador ────────────────
            if (!CapturarHabilitado)
            {
                var r = MessageBox.Show(
                    $"O intervalo mínimo ainda não foi concluído.\n\n" +
                    $"{StatusCapturar}\n\n" +
                    "Registrar com antecedência pode comprometer a precisão.\n" +
                    "Deseja realizar a medição assim mesmo?",
                    "Intervalo não concluído",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (r != MessageBoxResult.Yes) return;
            }

            // ── Verifica conexão serial ─────────────────────────────────────
            if (!_serialService.Conectado)
            {
                if (EquipamentoFixo != null)
                {
                    try { _serialService.Conectar(EquipamentoFixo.PortaCOM, EquipamentoFixo.BaudRate); }
                    catch
                    {
                        StatusDisplay = $"❌ Equipamento não encontrado em {EquipamentoFixo?.PortaCOM}.";
                        return;
                    }
                }
                else
                {
                    StatusDisplay = "❌ Nenhum equipamento cadastrado.";
                    return;
                }
            }

            // ── Inicia captura ──────────────────────────────────────────────
            AguardandoCaptura = true;
            TextoBotaoCapturar = "DESCONECTAR";
            IconeBotaoCapturar = PackIconKind.Stop;
            SecadorHabilitado = false;

            StatusDisplay = $"🎯 Aguardando leitura do {SecadorSelecionado?.Nome}...";
            LimparLeitura(manterSecador: true);
            _escutandoSerial = true;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  RECEBE LEITURA DA SERIAL
        // ═══════════════════════════════════════════════════════════════════

        private async void OnLeituraRecebida(LeituraSerialDto dto)
        {
            if (!_escutandoSerial) return;

            await WpfApp.Current.Dispatcher.InvokeAsync(async () =>
            {
                _escutandoSerial = false;
                AguardandoCaptura = false;
                TextoBotaoCapturar = "CAPTURAR";
                IconeBotaoCapturar = PackIconKind.Radar;

                dto = await _medicaoService.EnriquecerLeituraAsync(dto);

                var equipEncontrado = await _equipRepo.GetByNumeroSerieAsync(dto.NumeroSerieEquipamento);
                if (equipEncontrado != null && equipEncontrado.Id != EquipamentoFixo?.Id)
                    EquipamentoFixo = equipEncontrado;

                _leituraAtual = dto;

                UmidadeDisplay = $"{dto.Umidade:F1}%";
                ProdutoDetectado = dto.NomeProduto;
                EquipamentoDetectado = EquipamentoFixo?.Nome ?? "—";
                HoraSistema = DateTime.Now.ToString("HH:mm:ss");
                HoraEquipamento = dto.DataHoraEquipamento.ToString("HH:mm:ss");

                CorSemaforo = dto.Status switch
                {
                    StatusUmidade.Ideal => CorVerde,
                    StatusUmidade.Atencao => CorLaranja,
                    StatusUmidade.Critico => CorVermelha,
                    _ => Brushes.Gray
                };

                StatusDisplay = dto.Status switch
                {
                    StatusUmidade.Ideal => "🟢 Ideal",
                    StatusUmidade.Atencao => "🟡 Atenção — Úmido",
                    StatusUmidade.Critico => "🔴 Crítico — Muito seco",
                    _ => "—"
                };

                LeituraRecebida = true;

                // Carrega silos disponíveis para o produto detectado
                var empresaId = SessaoUsuario.Atual!.EmpresaId;
                var produto = await _produtoRepo.GetByNomeAsync(dto.NomeProduto);

                SilosDisponiveis.Clear();
                if (produto != null)
                {
                    var silos = await _siloRepo.GetDestinosDisponiveisAsync(empresaId, produto.Id);
                    foreach (var s in silos) SilosDisponiveis.Add(s);
                }

                // Auto-seleciona: atenção → retrabalho; ideal/crítico → normal
                if (dto.Status == StatusUmidade.Atencao)
                    SiloDestinoSelecionado = SilosDisponiveis.FirstOrDefault(s => s.IsRetrabalho);
                else
                    SiloDestinoSelecionado = SilosDisponiveis.FirstOrDefault(s => !s.IsRetrabalho);
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        //  COMANDO: CONFIRMAR MEDIÇÃO
        // ═══════════════════════════════════════════════════════════════════

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

            // Validação do Grau do Secador (obrigatório)
            if (string.IsNullOrWhiteSpace(GrauSecadorTexto))
            {
                MessageBox.Show("Informe o grau do secador (ex: 80 ou 80.5).", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(
                    GrauSecadorTexto.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double grauSecador) || grauSecador <= 0)
            {
                MessageBox.Show("Grau do secador inválido. Informe um número positivo (ex: 80.5).",
                    "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                secadorId: SecadorSelecionado?.Id,
                grauSecador: grauSecador);

            MessageBox.Show("✅ Medição registrada com sucesso!", "Sucesso",
                MessageBoxButton.OK, MessageBoxImage.Information);

            LimparLeitura(manterSecador: true);
            SecadorHabilitado = true;

            await CarregarUltimasMedicoesAsync();
            await AvaliarTimerSecadorAsync();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  COMANDO: CANCELAR LEITURA
        // ═══════════════════════════════════════════════════════════════════

        [RelayCommand]
        private void Cancelar()
        {
            LimparLeitura(manterSecador: true);
            SecadorHabilitado = true;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  AUXILIARES
        // ═══════════════════════════════════════════════════════════════════

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
            GrauSecadorTexto = string.Empty;
            SiloDestinoSelecionado = null;
            SilosDisponiveis.Clear();

            if (!manterSecador)
            {
                SecadorSelecionado = Secadores.Count == 1 ? Secadores[0] : null;
                SecadorHabilitado = true;
            }
        }

        private void OnErroSerial(string erro) =>
            WpfApp.Current.Dispatcher.Invoke(() =>
                StatusDisplay = $"⚠️ Erro serial: {erro}");
    }
}