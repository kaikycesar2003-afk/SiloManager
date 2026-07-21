using SiloManager.Domain.Entities;
using System.Globalization;
using System.Windows;

namespace SiloManager.WPF.Views
{
    public partial class EdicaoMedicaoWindow : Window
    {
        public double? UmidadeEditada { get; private set; }
        public double? GrauSecadorEditado { get; private set; }
        public Produto? ProdutoEditado { get; private set; }
        public Silo? SiloEditado { get; private set; }
        public Secador? SecadorEditado { get; private set; }
        public string ObservacaoEditada { get; private set; } = string.Empty;
        public bool Confirmado { get; private set; }

        public EdicaoMedicaoWindow(
            double umidadeAtual,
            double? grauSecadorAtual,
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
            TxtGrauSecador.Text = grauSecadorAtual?.ToString("F1") ?? string.Empty;
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

            double? grauSecador = null;
            if (!string.IsNullOrWhiteSpace(TxtGrauSecador.Text))
            {
                if (!double.TryParse(
                        TxtGrauSecador.Text.Replace(',', '.'),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var g) || g <= 0)
                {
                    MessageBox.Show("Grau do secador inválido. Informe um número positivo (ex: 80.5).",
                        "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                grauSecador = g;
            }

            UmidadeEditada = umidade;
            GrauSecadorEditado = grauSecador;
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