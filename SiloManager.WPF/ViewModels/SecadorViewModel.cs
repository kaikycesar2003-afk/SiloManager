using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using System.Collections.ObjectModel;
using System.Windows;

namespace SiloManager.WPF.ViewModels
{
    public partial class SecadorViewModel : ObservableObject
    {
        private readonly ISecadorRepository _repo;

        [ObservableProperty] private string _nomeSecador = string.Empty;
        [ObservableProperty] private bool _mostrarInativos;
        private int? _editandoId;

        public ObservableCollection<Secador> Secadores { get; } = new();

        public SecadorViewModel(ISecadorRepository repo)
        {
            _repo = repo;
            _ = CarregarAsync();
        }

        partial void OnMostrarInativosChanged(bool value) => _ = CarregarAsync();

        private async Task CarregarAsync()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            IEnumerable<Secador> lista;
            if (MostrarInativos)
            {
                var todos = await _repo.GetAllAsync();
                lista = todos.Where(s => s.EmpresaId == empresaId);
            }
            else
                lista = await _repo.GetByEmpresaAsync(empresaId);

            Secadores.Clear();
            foreach (var s in lista) Secadores.Add(s);
        }

        [RelayCommand]
        private async Task SalvarSecador()
        {
            if (string.IsNullOrWhiteSpace(NomeSecador))
            {
                MessageBox.Show("Informe o nome do secador.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            // Verifica duplicidade
            var todos = await _repo.GetAllAsync();
            var duplicado = todos.FirstOrDefault(s =>
                s.EmpresaId == empresaId &&
                s.Nome.ToLower() == NomeSecador.ToLower() &&
                s.Id != _editandoId);

            if (duplicado != null)
            {
                MessageBox.Show($"Já existe um secador com o nome '{NomeSecador}'.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_editandoId.HasValue)
            {
                var secador = await _repo.GetByIdAsync(_editandoId.Value);
                if (secador != null)
                {
                    secador.Nome = NomeSecador;
                    await _repo.UpdateAsync(secador);
                    await _repo.SaveChangesAsync();
                }
                _editandoId = null;
            }
            else
            {
                await _repo.AddAsync(new Secador
                {
                    EmpresaId = empresaId,
                    Nome = NomeSecador,
                    Ativo = true
                });
                await _repo.SaveChangesAsync();
            }

            LimparFormulario();
            await CarregarAsync();
        }

        [RelayCommand]
        private void EditarSecador(Secador s)
        {
            _editandoId = s.Id;
            NomeSecador = s.Nome;
        }

        [RelayCommand]
        private async Task DesativarSecador(Secador s)
        {
            var r = MessageBox.Show($"Desativar '{s.Nome}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            s.Ativo = false;
            await _repo.UpdateAsync(s);
            await _repo.SaveChangesAsync();
            await CarregarAsync();
        }

        [RelayCommand]
        private async Task ReativarSecador(Secador s)
        {
            s.Ativo = true;
            await _repo.UpdateAsync(s);
            await _repo.SaveChangesAsync();
            await CarregarAsync();
        }

        private void LimparFormulario()
        {
            NomeSecador = string.Empty;
            _editandoId = null;
        }
    }
}