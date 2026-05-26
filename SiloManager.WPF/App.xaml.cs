using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SiloManager.Application.Services;
using SiloManager.Domain.Interfaces.Repositories;
using SiloManager.Infrastructure.Data;
using SiloManager.Infrastructure.Repositories;
using SiloManager.WPF.ViewModels;
using SiloManager.WPF.Views;
using System.IO;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace SiloManager.WPF
{
    public partial class App : WpfApp
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show($"Erro: {ex.Exception.Message}\n\n{ex.Exception.InnerException?.Message}",
                    "Erro na inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                Services = services.BuildServiceProvider();

                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
                DbSeeder.Seed(db);

                var loginScope = Services.CreateScope();
                var login = loginScope.ServiceProvider.GetRequiredService<LoginWindow>();
                login.Closed += (_, _) => loginScope.Dispose();
                login.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar:\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SiloManager",
                "silomanager.db"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseSqlite($"Data Source={dbPath}"));

            // Repositories
            services.AddScoped<IEmpresaRepository, EmpresaRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IProdutoRepository, ProdutoRepository>();
            services.AddScoped<ISiloRepository, SiloRepository>();
            services.AddScoped<IEquipamentoRepository, EquipamentoRepository>();
            services.AddScoped<IMedicaoRepository, MedicaoRepository>();
            services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();

            // Services
            services.AddScoped<AuthService>();
            services.AddScoped<MedicaoService>();
            services.AddSingleton<SerialService>();

            // ViewModels e Windows
            services.AddTransient<EmpresaViewModel>();
            services.AddTransient<UsuarioViewModel>();
            services.AddTransient<EquipamentoViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<ProdutoViewModel>();
            services.AddTransient<SiloViewModel>();
        }
    }
}