using SiloManager.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SiloManager.WPF.Views.Cadastros
{
    public partial class UsuarioView : UserControl
    {
        public UsuarioView()
        {
            InitializeComponent();
        }

        private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsuarioViewModel vm)
                await vm.SalvarUsuarioAsync(SenhaBox.Password);
        }
    }
}