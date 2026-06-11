using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SiloManager.Application.Services;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Enums;
using SiloManager.Domain.Interfaces.Repositories;
using System.Collections.ObjectModel;
using System.Windows;

namespace SiloManager.WPF.ViewModels
{
    public partial class EmpresaViewModel : ObservableObject
    {
        private readonly IEmpresaRepository _repo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IConfiguracaoRepository _configuracaoRepo;

        [ObservableProperty] private string _nomeEmpresa = string.Empty;
        [ObservableProperty] private string _cnpjEmpresa = string.Empty;
        [ObservableProperty] private bool _mostrarInativos;
        private int? _editandoId;

        public ObservableCollection<Empresa> Empresas { get; } = new();

        public EmpresaViewModel(
            IEmpresaRepository repo,
            IUsuarioRepository usuarioRepo,
            IConfiguracaoRepository configuracaoRepo)
        {
            _repo = repo;
            _usuarioRepo = usuarioRepo;
            _configuracaoRepo = configuracaoRepo;
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
                // ── Edição: só atualiza nome e CNPJ ──
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
                // ── Nova empresa: cria empresa + admin padrão + configuração ──
                var novaEmpresa = new Empresa
                {
                    Nome = NomeEmpresa,
                    CNPJ = CnpjEmpresa,
                    Ativo = true
                };
                await _repo.AddAsync(novaEmpresa);
                await _repo.SaveChangesAsync(); // salva para obter o Id gerado

                // Admin padrão — login: admin / senha: admin123
                await _usuarioRepo.AddAsync(new Usuario
                {
                    EmpresaId = novaEmpresa.Id,
                    Nome = "Administrador",
                    Login = "admin",
                    SenhaHash = AuthService.GerarHash("admin123"),
                    Nivel = NivelAcesso.Administrador,
                    Ativo = true
                });

                // Configuração padrão (timer de 15 minutos)
                await _configuracaoRepo.AddAsync(new Configuracao
                {
                    EmpresaId = novaEmpresa.Id,
                    IntervaloMinimoSegundos = 900
                });

                await _usuarioRepo.SaveChangesAsync();

                MessageBox.Show(
                    $"✅ Empresa '{novaEmpresa.Nome}' criada!\n\n" +
                    $"Usuário padrão criado:\n" +
                    $"  Login: admin\n" +
                    $"  Senha: admin123\n\n" +
                    $"Troque a senha após o primeiro acesso.",
                    "Empresa criada",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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