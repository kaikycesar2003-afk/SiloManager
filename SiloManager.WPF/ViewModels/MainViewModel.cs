using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SiloManager.Application.Session;
using SiloManager.Domain.Enums;
using WpfApp = System.Windows.Application;

namespace SiloManager.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] private object? _paginaAtual;
        [ObservableProperty] private string _nomeUsuario = string.Empty;
        [ObservableProperty] private string _nomeEmpresa = string.Empty;
        [ObservableProperty] private bool _isAdmin;
        [ObservableProperty] private bool _isAdminOuGerente;

        public MainViewModel()
        {
            var sessao = SessaoUsuario.Atual!;
            NomeUsuario = sessao.Nome;
            NomeEmpresa = sessao.NomeEmpresa;
            IsAdmin = sessao.Nivel == NivelAcesso.Administrador;
            IsAdminOuGerente = sessao.Nivel >= NivelAcesso.Gerente;

            Navegar("Medicao");
        }

        [RelayCommand]
        private void Nav(string pagina) => Navegar(pagina);

        [RelayCommand]
        private void Sair()
        {
            // Para o SerialService antes de encerrar sessão
            var serial = App.Services.GetRequiredService<Application.Services.SerialService>();
            serial.Desconectar();

            SessaoUsuario.Encerrar();

            var loginScope = App.Services.CreateScope();
            var login = loginScope.ServiceProvider.GetRequiredService<Views.LoginWindow>();
            login.Closed += (_, _) => loginScope.Dispose();

            WpfApp.Current.MainWindow = login;
            WpfApp.Current.ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
            login.Show();

            foreach (System.Windows.Window w in WpfApp.Current.Windows.Cast<System.Windows.Window>().ToList())
                if (w != login) w.Close();
        }

        private void Navegar(string pagina)
        {
            var sp = App.Services;

            PaginaAtual = pagina switch
            {
                "Medicao" => new Views.MedicaoView
                { DataContext = sp.GetRequiredService<MedicaoViewModel>() },

                "Relatorio" => new Views.RelatorioView
                { DataContext = sp.GetRequiredService<RelatorioViewModel>() },

                "Produto" => new Views.Cadastros.ProdutoView
                { DataContext = sp.GetRequiredService<ProdutoViewModel>() },

                "Silo" => new Views.Cadastros.SiloView
                { DataContext = sp.GetRequiredService<SiloViewModel>() },

                "Equipamento" => new Views.Cadastros.EquipamentoView
                { DataContext = sp.GetRequiredService<EquipamentoViewModel>() },

                "Usuario" => new Views.Cadastros.UsuarioView
                { DataContext = sp.GetRequiredService<UsuarioViewModel>() },

                "Empresa" => new Views.Cadastros.EmpresaView
                { DataContext = sp.GetRequiredService<EmpresaViewModel>() },

                "Configuracao" => new Views.ConfiguracaoView
                { DataContext = sp.GetRequiredService<ConfiguracaoViewModel>() },

                _ => new System.Windows.Controls.TextBlock
                { Text = "Selecione uma opção no menu" }
            };
        }
    }
}