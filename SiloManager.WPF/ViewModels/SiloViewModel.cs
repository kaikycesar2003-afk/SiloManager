using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using System.Collections.ObjectModel;
using System.Windows;

namespace SiloManager.WPF.ViewModels
{
    public partial class SiloViewModel : ObservableObject
    {
        private readonly ISiloRepository _siloRepo;
        private readonly IProdutoRepository _produtoRepo;

        [ObservableProperty] private string _nomeSilo = string.Empty;
        [ObservableProperty] private int? _produtoIdSelecionado;
        [ObservableProperty] private bool _isRetrabalho;
        [ObservableProperty] private bool _mostrarInativos;
        private int? _editandoId;

        public ObservableCollection<Silo> Silos { get; } = new();
        public ObservableCollection<Produto> Produtos { get; } = new();

        public SiloViewModel(ISiloRepository siloRepo, IProdutoRepository produtoRepo)
        {
            _siloRepo = siloRepo;
            _produtoRepo = produtoRepo;
            _ = CarregarAsync();
        }

        partial void OnMostrarInativosChanged(bool value) => _ = CarregarAsync();

        private async Task CarregarAsync()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            var prods = await _produtoRepo.GetAtivosAsync();
            Produtos.Clear();
            foreach (var p in prods) Produtos.Add(p);

            IEnumerable<Silo> silos;
            if (MostrarInativos)
            {
                var todos = await _siloRepo.GetAllAsync();
                silos = todos.Where(s => s.EmpresaId == empresaId);
            }
            else
            {
                silos = await _siloRepo.GetByEmpresaAsync(empresaId);
            }

            Silos.Clear();
            foreach (var s in silos) Silos.Add(s);
        }

        [RelayCommand]
        private async Task SalvarSilo()
        {
            if (string.IsNullOrWhiteSpace(NomeSilo))
            {
                MessageBox.Show("Informe o nome do silo.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            // Verifica duplicidade de nome dentro da empresa
            var silosExistentes = await _siloRepo.GetByEmpresaAsync(empresaId);
            var duplicado = silosExistentes.FirstOrDefault(s =>
                s.Nome.ToLower() == NomeSilo.ToLower() && s.Id != _editandoId);

            if (duplicado != null)
            {
                MessageBox.Show($"Já existe um silo com o nome '{NomeSilo}'.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_editandoId.HasValue)
            {
                var silo = await _siloRepo.GetByIdAsync(_editandoId.Value);
                if (silo != null)
                {
                    silo.Nome = NomeSilo;
                    silo.ProdutoId = ProdutoIdSelecionado;
                    silo.IsRetrabalho = IsRetrabalho;
                    await _siloRepo.UpdateAsync(silo);
                    await _siloRepo.SaveChangesAsync();
                }
                _editandoId = null;
            }
            else
            {
                await _siloRepo.AddAsync(new Silo
                {
                    EmpresaId = empresaId,
                    Nome = NomeSilo,
                    ProdutoId = ProdutoIdSelecionado,
                    IsRetrabalho = IsRetrabalho,
                    Ativo = true
                });
                await _siloRepo.SaveChangesAsync();
            }

            LimparFormulario();
            await CarregarAsync();
        }

        [RelayCommand]
        private void EditarSilo(Silo s)
        {
            _editandoId = s.Id;
            NomeSilo = s.Nome;
            ProdutoIdSelecionado = s.ProdutoId;
            IsRetrabalho = s.IsRetrabalho;
        }

        [RelayCommand]
        private async Task DesativarSilo(Silo s)
        {
            var r = MessageBox.Show($"Desativar '{s.Nome}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            s.Ativo = false;
            await _siloRepo.UpdateAsync(s);
            await _siloRepo.SaveChangesAsync();
            await CarregarAsync();
        }

        [RelayCommand]
        private async Task ReativarSilo(Silo s)
        {
            s.Ativo = true;
            await _siloRepo.UpdateAsync(s);
            await _siloRepo.SaveChangesAsync();
            await CarregarAsync();
        }

        private void LimparFormulario()
        {
            NomeSilo = string.Empty;
            ProdutoIdSelecionado = null;
            IsRetrabalho = false;
            _editandoId = null;
        }
    }
}