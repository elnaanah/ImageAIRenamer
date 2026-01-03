using System.Windows;
using ImageAIRenamer.Application.ViewModels;
using ImageAIRenamer.Domain.Interfaces;

namespace ImageAIRenamer;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly INavigationService _navigationService;

    public MainWindow(MainViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _navigationService = navigationService;
        
        DataContext = _viewModel;
        
        _navigationService.SetNavigationFrame(MainFrame);
    }
}
