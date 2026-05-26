using SiloManager.WPF.ViewModels;
using System.Windows;

namespace SiloManager.WPF.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _vm;

        public LoginWindow(LoginViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.CarregarEmpresasAsync();
        }

        private async void BtnEntrar_Click(object sender, RoutedEventArgs e)
        {
            await _vm.EntrarAsync(SenhaBox.Password);
        }
    }
}