namespace DrawingVerifier.UI;

using System.Windows;
using DrawingVerifier.Application.Interfaces;
using DrawingVerifier.Application.Services;
using DrawingVerifier.Application.UseCases;
using DrawingVerifier.Domain.Interfaces;
using DrawingVerifier.Infrastructure.Data;
using DrawingVerifier.Infrastructure.Gemini;
using DrawingVerifier.Infrastructure.Services;
using DrawingVerifier.UI.ViewModels;
using DrawingVerifier.UI.Views;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show(
                $"A fatal error occurred:\n\n{ex?.Message}\n\n{ex?.StackTrace}",
                "Drawing Verifier - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            MessageBox.Show(
                $"An unhandled application error occurred:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "Drawing Verifier - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();

            SeedDatabase(_serviceProvider);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start Drawing Verifier:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Drawing Verifier - Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, WindowsSettingsService>();
        services.AddSingleton<ISkillRepository>(_ => new SkillLiteDbRepository());
        services.AddTransient<IRuleEvaluator, NCalcRuleEvaluator>();
        services.AddHttpClient<IGeminiApiService, GeminiApiHttpClient>();
        services.AddTransient<IReportGenerator, QuestPdfReportGenerator>();
        services.AddTransient<AnalyzeDrawingUseCase>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<SkillManagerViewModel>();
        services.AddTransient<SkillManagerWindow>();
    }

    private static void SeedDatabase(IServiceProvider serviceProvider)
    {
        try
        {
            var repository = serviceProvider.GetRequiredService<ISkillRepository>();
            DatabaseSeeder.SeedDefaultSkills(repository);
        }
        catch (Exception ex)
        {
            // Seeding is a convenience; the app must still start if the database is unavailable.
            MessageBox.Show(
                $"Failed to seed default skills: {ex.Message}",
                "Drawing Verifier",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
