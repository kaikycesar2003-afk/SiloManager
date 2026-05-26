using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using System.Collections.ObjectModel;
using System.Windows;

namespace SiloManager.WPF.ViewModels
{
    public partial class EmpresaViewModel : ObservableObject
    {
        private readonly IEmpresaRepository _repo;

        [ObservableProperty] private string _nomeEmpresa = string.Empty;
        [ObservableProperty] private string _cnpjEmpresa = string.Empty;
        [ObservableProperty] private bool _mostrarInativos;
        private int? _editandoId;

        public ObservableCollection<Empresa> Empresas { get; } = new();

        public EmpresaViewModel(IEmpresaRepository repo)
        {
            _repo = repo;
            _ = CarregarAsync();
        }

        partial void OnMostrarInativosChanged(bool value) => _ = CarregarAsync();

        private async Task CarregarAsync()
        {
            IEnumerable<Empresa> lista = MostrarInativos
                ? await _repo.GetAllAsync()
                : await _repo.GetAtivasAsync();

            Empresas.Clear();
            foreach (var e in lista) Empresas.Add(e);
        }

        [RelayCommand]
        private async Task SalvarEmpresa()
        {
            if (string.IsNullOrWhiteSpace(NomeEmpresa))
            {
                MessageBox.Show("Informe o nome da empresa.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verifica duplicidade de nome
            var todas = await _repo.GetAllAsync();
            var duplicado = todas.FirstOrDefault(e =>
                e.Nome.ToLower() == NomeEmpresa.ToLower() && e.Id != _editandoId);

            if (duplicado != null)
            {
                MessageBox.Show($"Já existe uma empresa com o nome '{NomeEmpresa}'.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_editandoId.HasValue)
            {
                var empresa = await _repo.GetByIdAsync(_editandoId.Value);
                if (empresa != null)
                {
                    empresa.Nome = NomeEmpresa;
                    empresa.CNPJ = CnpjEmpresa;
                    await _repo.UpdateAsync(empresa);
                    await _repo.SaveChangesAsync();
                }
                _editandoId = null;
            }
            else
            {
                await _repo.AddAsync(new Empresa
                {
                    Nome = NomeEmpresa,
                    CNPJ = CnpjEmpresa,
                    Ativo = true
                });
                await _repo.SaveChangesAsync();
            }

            LimparFormulario();
            await CarregarAsync();
        }

        [RelayCommand]
        private void EditarEmpresa(Empresa e)
        {
            _editandoId = e.Id;
            NomeEmpresa = e.Nome;
            CnpjEmpresa = e.CNPJ;
        }

        [RelayCommand]
        private async Task DesativarEmpresa(Empresa e)
        {
            if (e.Id == SessaoUsuario.Atual!.EmpresaId)
            {
                MessageBox.Show("Não é possível desativar a empresa atual.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var r = MessageBox.Show(
                $"Desativar '{e.Nome}'?\n\nIsso impedirá o acesso de todos os usuários desta empresa.",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;

            e.Ativo = false;
            await _repo.UpdateAsync(e);
            await _repo.SaveChangesAsync();
            await CarregarAsync();
        }

        [RelayCommand]
        private async Task ReativarEmpresa(Empresa e)
        {
            e.Ativo = true;
            await _repo.UpdateAsync(e);
            await _repo.SaveChangesAsync();
            await CarregarAsync();
        }

        private void LimparFormulario()
        {
            NomeEmpresa = string.Empty;
            CnpjEmpresa = string.Empty;
            _editandoId = null;
        }
    }
}