using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SiloManager.Application.Services;
using SiloManager.Domain.Interfaces.Repositories;
using System.Collections.ObjectModel;
using WpfApp = System.Windows.Application;

namespace SiloManager.WPF.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _auth;
        private readonly IEmpresaRepository _empresaRepo;

        [ObservableProperty] private int _empresaIdSelecionada;
        [ObservableProperty] private string _login = string.Empty;
        [ObservableProperty] private string _erro = string.Empty;
        [ObservableProperty] private bool _carregando;

        public ObservableCollection<EmpresaItem> Empresas { get; } = new();

        public LoginViewModel(AuthService auth, IEmpresaRepository empresaRepo)
        {
            _auth = auth;
            _empresaRepo = empresaRepo;
        }

        public async Task CarregarEmpresasAsync()
        {
            var lista = await _empresaRepo.GetAtivasAsync();
            Empresas.Clear();
            foreach (var e in lista)
                Empresas.Add(new EmpresaItem(e.Id, e.Nome));

            if (Empresas.Count > 0)
                EmpresaIdSelecionada = Empresas[0].Id;
        }

        [RelayCommand]
        public async Task EntrarAsync(string senha)
        {
            Erro = string.Empty;

            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(senha))
            {
                Erro = "Informe login e senha.";
                return;
            }

            Carregando = true;
            try
            {
                var ok = await _auth.LoginAsync(EmpresaIdSelecionada, Login, senha);
                if (ok)
                    AbrirMainWindow();
                else
                    Erro = "Login ou senha inválidos.";
            }
            finally
            {
                Carregando = false;
            }
        }

        private static void AbrirMainWindow()
        {
            var main = new Views.MainWindow();
            WpfApp.Current.MainWindow = main;
            WpfApp.Current.ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
            main.Show();

            // Fecha a janela de login (a primeira da lista que não é a main)
            foreach (System.Windows.Window w in WpfApp.Current.Windows)
                if (w != main) { w.Close(); break; }
        }
    }

    public record EmpresaItem(int Id, string Nome);
}