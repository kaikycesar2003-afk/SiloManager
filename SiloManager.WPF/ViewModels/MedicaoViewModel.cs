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
        private readonly MedicaoService _medicaoService;
        private readonly SerialService _serialService;
        private readonly IConfiguracaoRepository _configRepo;

        private DispatcherTimer _timer = new();
        private DateTime? _liberadoEm;
        private int _intervaloSegundos = 900;
        private LeituraSerialDto? _leituraAtual;

        // ═══ Equipamento ═══
        [ObservableProperty] private Equipamento? _equipamentoSelecionado;
        [ObservableProperty] private bool _naoConectado = true;
        [ObservableProperty] private string _textoBotaoConectar = "CONECTAR";
        [ObservableProperty] private string _iconeConectar = "SerialPort";
        [ObservableProperty] private Brush _corBotaoConectar = new SolidColorBrush(Color.FromRgb(46, 125, 50));

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

        // ═══ Timer ═══
        [ObservableProperty] private string _timerDisplay = "00:00";
        [ObservableProperty] private string _statusTimer = "Carregando...";
        [ObservableProperty] private double _progressoTimer;
        [ObservableProperty] private Brush _corTimer = Brushes.Gray;

        public ObservableCollection<Equipamento> Equipamentos { get; } = new();
        public ObservableCollection<Silo> SilosDisponiveis { get; } = new();
        public ObservableCollection<Medicao> UltimasMedicoes { get; } = new();

        public MedicaoViewModel(
            IEquipamentoRepository equipRepo,
            ISiloRepository siloRepo,
            IProdutoRepository produtoRepo,
            IMedicaoRepository medicaoRepo,
            MedicaoService medicaoService,
            SerialService serialService,
            IConfiguracaoRepository configRepo)
        {
            _equipRepo = equipRepo;
            _siloRepo = siloRepo;
            _produtoRepo = produtoRepo;
            _medicaoRepo = medicaoRepo;
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

            // Carrega configuração do timer
            var config = await _configRepo.GetByEmpresaAsync(empresaId);
            _intervaloSegundos = config?.IntervaloMinimoSegundos ?? 900;

            // Carrega equipamentos
            var equips = await _equipRepo.GetByEmpresaAsync(empresaId);
            Equipamentos.Clear();
            foreach (var e in equips) Equipamentos.Add(e);

            // Carrega últimas medições
            await CarregarUltimasMedicoesAsync();

            // Inicia timer
            IniciarTimer();
        }

        private void IniciarTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += TimerTick;
            _timer.Start();
        }

        private async void TimerTick(object? s, EventArgs e)
        {
            var restante = await _medicaoService.VerificarTimerAsync(SessaoUsuario.Atual!.EmpresaId);

            if (restante == null)
            {
                // Liberado
                TimerDisplay = "LIVRE";
                StatusTimer = "✅ Medição liberada";
                ProgressoTimer = 100;
                CorTimer = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else
            {
                var ts = restante.Value;
                TimerDisplay = $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
                StatusTimer = "⏳ Aguarde para medir";
                ProgressoTimer = 100 - (ts.TotalSeconds / _intervaloSegundos * 100);
                CorTimer = ts.TotalMinutes < 2
                    ? new SolidColorBrush(Color.FromRgb(245, 124, 0))
                    : new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
        }

        [RelayCommand]
        private void Conectar()
        {
            if (_serialService.Conectado)
            {
                _serialService.Desconectar();
                NaoConectado = true;
                TextoBotaoConectar = "CONECTAR";
                IconeConectar = "SerialPort";
                CorBotaoConectar = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                StatusDisplay = "Desconectado.";
            }
            else
            {
                if (EquipamentoSelecionado == null)
                {
                    MessageBox.Show("Selecione um equipamento.", "Atenção",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    _serialService.Conectar(
                        EquipamentoSelecionado.PortaCOM,
                        EquipamentoSelecionado.BaudRate);

                    NaoConectado = false;
                    TextoBotaoConectar = "DESCONECTAR";
                    IconeConectar = "Close";
                    CorBotaoConectar = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                    StatusDisplay = $"✅ Conectado em {EquipamentoSelecionado.PortaCOM} — Aguardando leitura...";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao conectar: {ex.Message}", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void OnLeituraRecebida(LeituraSerialDto dto)
        {
            // Roda na UI thread
            await WpfApp.Current.Dispatcher.InvokeAsync(async () =>
            {
                // Enriquece com dados do banco
                dto = await _medicaoService.EnriquecerLeituraAsync(dto);

                // Verifica equipamento pelo nº de série
                var equipEncontrado = await _equipRepo.GetByNumeroSerieAsync(dto.NumeroSerieEquipamento);

                if (equipEncontrado != null && equipEncontrado.Id != EquipamentoSelecionado?.Id)
                {
                    // Correção automática
                    EquipamentoSelecionado = equipEncontrado;
                    StatusDisplay = $"⚠️ Equipamento corrigido para: {equipEncontrado.Nome}";
                }
                else if (equipEncontrado == null && !string.IsNullOrWhiteSpace(dto.NumeroSerieEquipamento))
                {
                    // Equipamento não cadastrado
                    var r = MessageBox.Show(
                        $"Equipamento não reconhecido.\nSérie: {dto.NumeroSerieEquipamento}\n\nDeseja continuar mesmo assim?",
                        "Equipamento não cadastrado",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (r != MessageBoxResult.Yes) return;
                }

                _leituraAtual = dto;

                // Atualiza UI
                UmidadeDisplay = $"{dto.Umidade:F1}%";
                ProdutoDetectado = dto.NomeProduto;
                EquipamentoDetectado = EquipamentoSelecionado?.Nome ?? $"Não identificado ({dto.NumeroSerieEquipamento})";
                HoraSistema = DateTime.Now.ToString("HH:mm:ss");
                HoraEquipamento = dto.DataHoraEquipamento.ToString("HH:mm:ss");

                // Semáforo
                AtualizarSemaforo(dto.Status);

                // Carrega silos disponíveis para o produto
                await CarregarSilosAsync(dto.NomeProduto);

                LeituraRecebida = true;
            });
        }

        private void AtualizarSemaforo(StatusUmidade status)
        {
            (CorSemaforo, StatusDisplay) = status switch
            {
                StatusUmidade.Ideal => (new SolidColorBrush(Color.FromRgb(46, 125, 50)), "✅ IDEAL"),
                StatusUmidade.Seco => (new SolidColorBrush(Color.FromRgb(245, 124, 0)), "⚠️ SECO"),
                StatusUmidade.Atencao => (new SolidColorBrush(Color.FromRgb(245, 124, 0)), "⚠️ ATENÇÃO"),
                StatusUmidade.Critico => (new SolidColorBrush(Color.FromRgb(211, 47, 47)), "🔴 CRÍTICO — Retrabalho"),
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

            // Se crítico, pré-seleciona retrabalho
            if (_leituraAtual?.Status == StatusUmidade.Critico)
                SiloDestinoSelecionado = SilosDisponiveis.FirstOrDefault(s => s.IsRetrabalho);
            else
                SiloDestinoSelecionado = SilosDisponiveis.FirstOrDefault(s => !s.IsRetrabalho);
        }

        [RelayCommand]
        private async Task Confirmar()
        {
            if (_leituraAtual == null)
            {
                MessageBox.Show("Nenhuma leitura recebida.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SiloDestinoSelecionado == null)
            {
                MessageBox.Show("Selecione o destino do grão.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verifica timer
            var restante = await _medicaoService.VerificarTimerAsync(SessaoUsuario.Atual!.EmpresaId);
            if (restante != null)
            {
                var ts = restante.Value;
                MessageBox.Show(
                    $"Aguarde {(int)ts.TotalMinutes:00}:{ts.Seconds:00} para a próxima medição.",
                    "Timer ativo", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                equipamentoId: EquipamentoSelecionado!.Id,
                siloDestinoId: SiloDestinoSelecionado.Id,
                umidade: _leituraAtual.Umidade,
                isRetrabalho: SiloDestinoSelecionado.IsRetrabalho,
                dadosBrutos: _leituraAtual.DadosBrutos,
                dataHoraEquipamento: _leituraAtual.DataHoraEquipamento,
                observacao: Observacao);

            MessageBox.Show("✅ Medição registrada com sucesso!", "Sucesso",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // Limpa para próxima medição
            LimparLeitura();
            await CarregarUltimasMedicoesAsync();
        }

        private async Task CarregarUltimasMedicoesAsync()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;
            var lista = await _medicaoRepo.GetByFiltroAsync(
                empresaId,
                DateTime.Today,
                DateTime.Now);

            UltimasMedicoes.Clear();
            foreach (var m in lista.Take(10)) UltimasMedicoes.Add(m);
        }

        private void LimparLeitura()
        {
            _leituraAtual = null;
            UmidadeDisplay = "-- %";
            StatusDisplay = "Aguardando próxima leitura...";
            ProdutoDetectado = "—";
            EquipamentoDetectado = "—";
            HoraSistema = "—";
            HoraEquipamento = "—";
            CorSemaforo = Brushes.Gray;
            LeituraRecebida = false;
            Observacao = string.Empty;
            SiloDestinoSelecionado = null;
            SilosDisponiveis.Clear();
        }

        private void OnErroSerial(string erro) =>
            WpfApp.Current.Dispatcher.Invoke(() =>
                StatusDisplay = $"⚠️ Erro serial: {erro}");
    }
}