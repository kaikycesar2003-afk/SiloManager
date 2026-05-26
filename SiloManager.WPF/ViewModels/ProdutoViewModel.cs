using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using System.Collections.ObjectModel;
using System.Windows;

namespace SiloManager.WPF.ViewModels
{
    public partial class ProdutoViewModel : ObservableObject
    {
        private readonly IProdutoRepository _repo;

        [ObservableProperty] private string _nomeProduto = string.Empty;
        [ObservableProperty] private string _umidadeMinima = string.Empty;
        [ObservableProperty] private string _umidadeIdeal = string.Empty;
        [ObservableProperty] private string _umidadeMaxima = string.Empty;
        [ObservableProperty] private bool _mostrarInativos;
        private int? _editandoId;

        public ObservableCollection<Produto> Produtos { get; } = new();

        public ProdutoViewModel(IProdutoRepository repo)
        {
            _repo = repo;
            _ = CarregarAsync();
        }

        partial void OnMostrarInativosChanged(bool value) => _ = CarregarAsync();

        private async Task CarregarAsync()
        {
            IEnumerable<Produto> lista;

            if (MostrarInativos)
            {
                var todos = await _repo.GetAllAsync();
                lista = todos;
            }
            else
            {
                lista = await _repo.GetAtivosAsync();
            }

            Produtos.Clear();
            foreach (var p in lista) Produtos.Add(p);
        }

        [RelayCommand]
        private async Task SalvarProduto()
        {
            if (string.IsNullOrWhiteSpace(NomeProduto) ||
                !double.TryParse(UmidadeMinima, out var min) ||
                !double.TryParse(UmidadeIdeal, out var ideal) ||
                !double.TryParse(UmidadeMaxima, out var max))
            {
                MessageBox.Show("Preencha todos os campos corretamente.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (min >= ideal || ideal >= max)
            {
                MessageBox.Show("Verifique os valores: Mínima < Ideal < Máxima.",
                    "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verifica duplicidade de nome
            var existente = await _repo.GetByNomeAsync(NomeProduto);
            if (existente != null && existente.Id != _editandoId)
            {
                MessageBox.Show($"Já existe um produto com o nome '{NomeProduto}'.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_editandoId.HasValue)
            {
                var produto = await _repo.GetByIdAsync(_editandoId.Value);
                if (produto != null)
                {
                    produto.Nome = NomeProduto;
                    produto.UmidadeMinima = min;
                    produto.UmidadeIdeal = ideal;
                    produto.UmidadeMaxima = max;
                    await _repo.UpdateAsync(produto);
                    await _repo.SaveChangesAsync();
                }
                _editandoId = null;
            }
            else
            {
                await _repo.AddAsync(new Produto
                {
                    Nome = NomeProduto,
                    UmidadeMinima = min,
                    UmidadeIdeal = ideal,
                    UmidadeMaxima = max,
                    Ativo = true
                });
                await _repo.SaveChangesAsync();
            }

            LimparFormulario();
            await CarregarAsync();
        }

        [RelayCommand]
        private void EditarProduto(Produto p)
        {
            _editandoId = p.Id;
            NomeProduto = p.Nome;
            UmidadeMinima = p.UmidadeMinima.ToString("F1");
            UmidadeIdeal = p.UmidadeIdeal.ToString("F1");
            UmidadeMaxima = p.UmidadeMaxima.ToString("F1");
        }

        [RelayCommand]
        private async Task DesativarProduto(Produto p)
        {
            var r = MessageBox.Show($"Desativar '{p.Nome}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            p.Ativo = false;
            await _repo.UpdateAsync(p);
            await _repo.SaveChangesAsync();
            await CarregarAsync();
        }

        [RelayCommand]
        private async Task ReativarProduto(Produto p)
        {
            p.Ativo = true;
            await _repo.UpdateAsync(p);
            await _repo.SaveChangesAsync();
            await CarregarAsync();
        }

        private void LimparFormulario()
        {
            NomeProduto = string.Empty;
            UmidadeMinima = string.Empty;
            UmidadeIdeal = string.Empty;
            UmidadeMaxima = string.Empty;
            _editandoId = null;
        }
    }
}