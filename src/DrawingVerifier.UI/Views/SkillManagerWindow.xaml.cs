namespace DrawingVerifier.UI.Views;

using System.Windows;
using DrawingVerifier.UI.ViewModels;

/// <summary>
/// Interaction logic for SkillManagerWindow.xaml
/// </summary>
public partial class SkillManagerWindow : Window
{
    public SkillManagerWindow(SkillManagerViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
