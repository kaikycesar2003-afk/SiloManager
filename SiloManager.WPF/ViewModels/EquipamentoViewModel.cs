using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SiloManager.Application.Services;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows;

namespace SiloManager.WPF.ViewModels
{
    public partial class EquipamentoViewModel : ObservableObject
    {
        private readonly IEquipamentoRepository _repo;

        [ObservableProperty] private string _nomeEquipamento = string.Empty;
        [ObservableProperty] private string _modelo = string.Empty;
        [ObservableProperty] private string _numeroSerie = string.Empty;
        [ObservableProperty] private string _portaCOM = string.Empty;
        [ObservableProperty] private string _baudRate = "9600";
        [ObservableProperty] private bool _mostrarInativos;
        private int? _editandoId;

        public ObservableCollection<Equipamento> Equipamentos { get; } = new();
        public ObservableCollection<string> PortasDisponiveis { get; } = new();

        public EquipamentoViewModel(IEquipamentoRepository repo)
        {
            _repo = repo;
            CarregarPortas();
            _ = CarregarAsync();
        }

        private void CarregarPortas()
        {
            PortasDisponiveis.Clear();
            foreach (var p in SerialPort.GetPortNames().OrderBy(x => x))
                PortasDisponiveis.Add(p);

            if (PortasDisponiveis.Count > 0)
                PortaCOM = PortasDisponiveis[0];
        }

        partial void OnMostrarInativosChanged(bool value) => _ = CarregarAsync();

        private async Task CarregarAsync()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            IEnumerable<Equipamento> lista;
            if (MostrarInativos)
            {
                var todos = await _repo.GetAllAsync();
                lista = todos.Where(e => e.EmpresaId == empresaId);
            }
            else
            {
                lista = await _repo.GetByEmpresaAsync(empresaId);
            }

            Equipamentos.Clear();
            foreach (var e in lista) Equipamentos.Add(e);
        }

        [RelayCommand]
        private async Task SalvarEquipamento()
        {
            if (string.IsNullOrWhiteSpace(NomeEquipamento) ||
                string.IsNullOrWhiteSpace(PortaCOM))
            {
                MessageBox.Show("Informe pelo menos o nome e a porta COM.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(BaudRate, out var baud))
            {
                MessageBox.Show("Baud Rate inválido.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            // Verifica duplicidade de número de série
            if (!string.IsNullOrWhiteSpace(NumeroSerie))
            {
                var existente = await _repo.GetByNumeroSerieAsync(NumeroSerie);
                if (existente != null && existente.Id != _editandoId)
                {
                    MessageBox.Show($"Já existe um equipamento com o número de série '{NumeroSerie}'.",
                        "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (_editandoId.HasValue)
            {
                var eq = await _repo.GetByIdAsync(_editandoId.Value);
                if (eq != null)
                {
                    eq.Nome = NomeEquipamento;
                    eq.Modelo = Modelo;
                    eq.NumeroSerie = NumeroSerie;
                    eq.PortaCOM = PortaCOM;
                    eq.BaudRate = baud;
                    await _repo.UpdateAsync(eq);
                    await _repo.SaveChangesAsync();
                }
                _editandoId = null;
            }
            else
            {
                await _repo.AddAsync(new Equipamento
                {
                    EmpresaId = empresaId,
                    Nome = NomeEquipamento,
                    Modelo = Modelo,
                    NumeroSerie = NumeroSerie,
                    PortaCOM = PortaCOM,
                    BaudRate = baud,
                    Ativo = true
                });
                await _repo.SaveChangesAsync();
            }

            LimparFormulario();
            await CarregarAsync();
        }

        [RelayCommand]
        private void EditarEquipamento(Equipamento e)
        {
            _editandoId = e.Id;
            NomeEquipamento = e.Nome;
            Modelo = e.Modelo;
            NumeroSerie = e.NumeroSerie;
            PortaCOM = e.PortaCOM;
            BaudRate = e.BaudRate.ToString();
        }

        [RelayCommand]
        private async Task DesativarEquipamento(Equipamento e)
        {
            var r = MessageBox.Show($"Desativar '{e.Nome}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            e.Ativo = false;
            await _repo.UpdateAsync(e);
            await _repo.SaveChangesAsync();
            await CarregarAsync();
        }

        [RelayCommand]
        private async Task ReativarEquipamento(Equipamento e)
        {
            e.Ativo = true;
            await _repo.UpdateAsync(e);
            await _repo.SaveChangesAsync();
            await CarregarAsync();
        }

        private void LimparFormulario()
        {
            NomeEquipamento = string.Empty;
            Modelo = string.Empty;
            NumeroSerie = string.Empty;
            BaudRate = "9600";
            _editandoId = null;
            CarregarPortas();
        }
    }
}