namespace DrawingVerifier.UI;

using System.Windows;
using DrawingVerifier.UI.ViewModels;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) => await viewModel.LoadSkillsCommand.ExecuteAsync(null);
    }
}
