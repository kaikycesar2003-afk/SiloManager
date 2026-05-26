using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;
using System.Windows;

namespace SiloManager.WPF.ViewModels
{
    public partial class ConfiguracaoViewModel : ObservableObject
    {
        private readonly IConfiguracaoRepository _repo;

        [ObservableProperty] private double _intervaloMinutos = 15;
        [ObservableProperty] private string _configuracaoAtual = "Carregando...";
        [ObservableProperty] private string _previewTimer = "15 min";

        public ConfiguracaoViewModel(IConfiguracaoRepository repo)
        {
            _repo = repo;
            _ = CarregarAsync();
        }

        partial void OnIntervaloMinutosChanged(double value)
        {
            var min = (int)value;
            var seg = (int)((value - min) * 60);
            PreviewTimer = seg > 0
                ? $"{min}min {seg}s"
                : $"{min} min";
        }

        private async Task CarregarAsync()
        {
            var empresaId = SessaoUsuario.Atual!.EmpresaId;
            var config = await _repo.GetByEmpresaAsync(empresaId);

            if (config != null)
            {
                IntervaloMinutos = config.IntervaloMinimoSegundos / 60.0;
                ConfiguracaoAtual = $"{config.IntervaloMinimoSegundos / 60} min";
            }
        }

        [RelayCommand]
        private async Task Salvar()
        {
            if (IntervaloMinutos < 1)
            {
                MessageBox.Show("O intervalo mínimo é de 1 minuto.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var empresaId = SessaoUsuario.Atual!.EmpresaId;
            var config = await _repo.GetByEmpresaAsync(empresaId);

            if (config != null)
            {
                config.IntervaloMinimoSegundos = (int)(IntervaloMinutos * 60);
                await _repo.UpdateAsync(config);
            }
            else
            {
                await _repo.AddAsync(new Configuracao
                {
                    EmpresaId = empresaId,
                    IntervaloMinimoSegundos = (int)(IntervaloMinutos * 60)
                });
            }

            await _repo.SaveChangesAsync();
            ConfiguracaoAtual = $"{(int)IntervaloMinutos} min";

            MessageBox.Show("✅ Configuração salva!", "Sucesso",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}