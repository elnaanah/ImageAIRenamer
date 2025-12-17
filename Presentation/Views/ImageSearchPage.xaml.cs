using System.Windows.Controls;
using ImageAIRenamer.Application.ViewModels;

namespace ImageAIRenamer.Presentation.Views;

/// <summary>
/// Interaction logic for ImageSearchPage.xaml
/// </summary>
public partial class ImageSearchPage : Page
{
    public ImageSearchPage(ImageSearchViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
