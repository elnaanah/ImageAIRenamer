using System.Windows;
using ImageAIRenamer.Application.ViewModels;
using ImageAIRenamer.Domain.Interfaces;
using ImageAIRenamer.Infrastructure.Services;

namespace ImageAIRenamer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly NavigationService _navigationService;

    public MainWindow(MainViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _navigationService = navigationService as NavigationService ?? throw new ArgumentException("NavigationService must be of type NavigationService");
        
        DataContext = _viewModel;
        
        // Set the navigation frame
        _navigationService.SetNavigationFrame(MainFrame);
    }
}
