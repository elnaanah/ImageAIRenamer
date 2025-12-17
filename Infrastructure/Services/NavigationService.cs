using System.Windows.Controls;
using ImageAIRenamer.Application.ViewModels;
using ImageAIRenamer.Domain.Interfaces;
using ImageAIRenamer.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ImageAIRenamer.Infrastructure.Services;

/// <summary>
/// Service for navigating between pages in the application
/// </summary>
public class NavigationService : INavigationService
{
    private Frame? _navigationFrame;
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Sets the navigation frame to use for navigation
    /// </summary>
    /// <param name="frame">The Frame control to navigate with</param>
    public void SetNavigationFrame(Frame frame)
    {
        _navigationFrame = frame;
    }

    /// <inheritdoc/>
    public void NavigateToWelcome()
    {
        var viewModel = _serviceProvider.GetRequiredService<WelcomeViewModel>();
        _navigationFrame?.Navigate(new WelcomePage(viewModel));
    }

    /// <inheritdoc/>
    public void NavigateToSearch()
    {
        var viewModel = _serviceProvider.GetRequiredService<ImageSearchViewModel>();
        _navigationFrame?.Navigate(new ImageSearchPage(viewModel));
    }

    /// <inheritdoc/>
    public void NavigateToRename()
    {
        var viewModel = _serviceProvider.GetRequiredService<ImageRenameViewModel>();
        _navigationFrame?.Navigate(new ImageRenamePage(viewModel));
    }
}
