namespace DrawingVerifier.UI;

using System.Windows;
using DrawingVerifier.Application.Services;
using DrawingVerifier.Application.UseCases;
using DrawingVerifier.Domain.Interfaces;
using DrawingVerifier.Infrastructure.Data;
using DrawingVerifier.Infrastructure.Gemini;
using DrawingVerifier.UI.ViewModels;
using Microsoft.Extensions.Configuration;
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

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddSingleton<ISkillRepository>(_ => new SkillLiteDbRepository());
        services.AddTransient<IRuleEvaluator, NCalcRuleEvaluator>();
        services.AddHttpClient<IGeminiApiService, GeminiApiHttpClient>();
        services.AddTransient<AnalyzeDrawingUseCase>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
