using System.Windows.Controls;
using ImageAIRenamer.Application.ViewModels;

namespace ImageAIRenamer.Presentation.Views;

/// <summary>
/// Interaction logic for ImageRenamePage.xaml
/// </summary>
public partial class ImageRenamePage : Page
{
    public ImageRenamePage(ImageRenameViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
