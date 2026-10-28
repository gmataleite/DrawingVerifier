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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        SeedDatabase(_serviceProvider);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
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
                $"Failed to seed the default skills: {ex.Message}",
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
