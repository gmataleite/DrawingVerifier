namespace DrawingVerifier.UI.Views;

using System.Windows;
using DrawingVerifier.UI.ViewModels;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseAction = Close;
    }
}
