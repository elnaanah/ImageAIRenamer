using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using ImageAIRenamer.Application.Common;
using ImageAIRenamer.Domain.Interfaces;

namespace ImageAIRenamer.Application.ViewModels;

/// <summary>
/// ViewModel for the welcome page
/// </summary>
public class WelcomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public WelcomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    /// <summary>
    /// Command to navigate to AI Studio website
    /// </summary>
    public IRelayCommand OpenAiStudioCommand => new RelayCommand(() =>
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://aistudio.google.com/",
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently fail - could log here
        }
    });

    /// <summary>
    /// Command to navigate to search page
    /// </summary>
    public IRelayCommand NavigateToSearchCommand => new RelayCommand(() =>
    {
        _navigationService.NavigateToSearch();
    });

    /// <summary>
    /// Command to navigate to rename page
    /// </summary>
    public IRelayCommand NavigateToRenameCommand => new RelayCommand(() =>
    {
        _navigationService.NavigateToRename();
    });
}
