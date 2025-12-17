using ImageAIRenamer.Application.Common;
using ImageAIRenamer.Domain.Interfaces;

namespace ImageAIRenamer.Application.ViewModels;

/// <summary>
/// ViewModel for the main window
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    /// <summary>
    /// Initializes the view model by navigating to welcome page
    /// </summary>
    public void Initialize()
    {
        _navigationService.NavigateToWelcome();
    }
}
