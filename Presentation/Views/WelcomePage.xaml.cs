using System.Windows.Controls;
using ImageAIRenamer.Application.ViewModels;

namespace ImageAIRenamer.Presentation.Views;

/// <summary>
/// Interaction logic for WelcomePage.xaml
/// </summary>
public partial class WelcomePage : Page
{
    public WelcomePage(WelcomeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
