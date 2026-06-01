using SiloManager.Domain.Entities;
using System.Windows;

namespace SiloManager.WPF.Views
{
    public partial class EdicaoMedicaoWindow : Window
    {
        public double? UmidadeEditada { get; private set; }
        public Produto? ProdutoEditado { get; private set; }
        public Silo? SiloEditado { get; private set; }
        public Secador? SecadorEditado { get; private set; }
        public string ObservacaoEditada { get; private set; } = string.Empty;
        public bool Confirmado { get; private set; }

        public EdicaoMedicaoWindow(
            double umidadeAtual,
            string observacaoAtual,
            IEnumerable<Produto> produtos,
            IEnumerable<Silo> silos,
            IEnumerable<Secador> secadores,
            Produto? produtoAtual,
            Silo? siloAtual,
            Secador? secadorAtual)
        {
            InitializeComponent();

            TxtUmidade.Text = umidadeAtual.ToString("F2");
            TxtObservacao.Text = observacaoAtual;

            CboProduto.ItemsSource = produtos.ToList();
            CboSilo.ItemsSource = silos.ToList();
            CboSecador.ItemsSource = secadores.ToList();

            CboProduto.SelectedItem = produtoAtual;
            CboSilo.SelectedItem = siloAtual;
            CboSecador.SelectedItem = secadorAtual;
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(TxtUmidade.Text, out var umidade))
            {
                MessageBox.Show("Umidade inválida.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UmidadeEditada = umidade;
            ProdutoEditado = CboProduto.SelectedItem as Produto;
            SiloEditado = CboSilo.SelectedItem as Silo;
            SecadorEditado = CboSecador.SelectedItem as Secador;
            ObservacaoEditada = TxtObservacao.Text;
            Confirmado = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();
    }
}