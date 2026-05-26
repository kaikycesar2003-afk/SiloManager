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
    public partial class UsuarioViewModel : ObservableObject
    {
        private readonly IUsuarioRepository _repo;

        [ObservableProperty] private string _nome = string.Empty;
        [ObservableProperty] private string _login = string.Empty;
        [ObservableProperty] private NivelAcesso _nivelSelecionado = NivelAcesso.Operador;
        [ObservableProperty] private bool _mostrarInativos;
        [ObservableProperty] private bool _isEdicao;
        private int? _editandoId;

        public ObservableCollection<Usuario> Usuarios { get; } = new();
        public IEnumerable<NivelAcesso> NiveisAcesso => Enum.GetValues<NivelAcesso>();

        public UsuarioViewModel(IUsuarioRepository repo)
        {
            _repo = repo;
            _ = CarregarAsync();
        }

        partial void OnMostrarInativosChanged(bool value) => _ = CarregarAsync();

        private async Task CarregarAsync()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            IEnumerable<Usuario> lista;
            if (MostrarInativos)
            {
                var todos = await _repo.GetAllAsync();
                lista = todos.Where(u => u.EmpresaId == empresaId);
            }
            else
                lista = await _repo.GetByEmpresaAsync(empresaId);

            Usuarios.Clear();
            foreach (var u in lista) Usuarios.Add(u);
        }

        public async Task SalvarUsuarioAsync(string senha)
        {
            if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Login))
            {
                MessageBox.Show("Preencha nome e login.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_editandoId.HasValue && string.IsNullOrWhiteSpace(senha))
            {
                MessageBox.Show("Informe uma senha para o novo usuário.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var empresaId = SessaoUsuario.Atual!.EmpresaId;

            // Verifica duplicidade de login na empresa
            var existente = await _repo.GetByLoginAsync(empresaId, Login);
            if (existente != null && existente.Id != _editandoId)
            {
                MessageBox.Show($"Já existe um usuário com o login '{Login}'.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_editandoId.HasValue)
            {
                var usuario = await _repo.GetByIdAsync(_editandoId.Value);
                if (usuario != null)
                {
                    usuario.Nome = Nome;
                    usuario.Login = Login;
                    usuario.Nivel = NivelSelecionado;

                    // Só atualiza a senha se foi informada
                    if (!string.IsNullOrWhiteSpace(senha))
                        usuario.SenhaHash = AuthService.GerarHash(senha);

                    await _repo.UpdateAsync(usuario);
                    await _repo.SaveChangesAsync();
                }
                _editandoId = null;
            }
            else
            {
                await _repo.AddAsync(new Usuario
                {
                    EmpresaId = empresaId,
                    Nome = Nome,
                    Login = Login,
                    SenhaHash = AuthService.GerarHash(senha),
                    Nivel = NivelSelecionado,
                    Ativo = true
                });
                await _repo.SaveChangesAsync();
            }

            LimparFormulario();
            await CarregarAsync();
        }

        [RelayCommand]
        private void EditarUsuario(Usuario u)
        {
            _editandoId = u.Id;
            Nome = u.Nome;
            Login = u.Login;
            NivelSelecionado = u.Nivel;
            IsEdicao = true;
        }

        [RelayCommand]
        private async Task DesativarUsuario(Usuario u)
        {
            if (u.Id == SessaoUsuario.Atual!.UsuarioId)
            {
                MessageBox.Show("Você não pode desativar o próprio usuário.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var r = MessageBox.Show($"Desativar '{u.Nome}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            u.Ativo = false;
            await _repo.UpdateAsync(u);
            await _repo.SaveChangesAsync();
            await CarregarAsync();
        }

        [RelayCommand]
        private async Task ReativarUsuario(Usuario u)
        {
            u.Ativo = true;
            await _repo.UpdateAsync(u);
            await _repo.SaveChangesAsync();
            await CarregarAsync();
        }

        private void LimparFormulario()
        {
            Nome = string.Empty;
            Login = string.Empty;
            NivelSelecionado = NivelAcesso.Operador;
            IsEdicao = false;
            _editandoId = null;
        }
    }
}