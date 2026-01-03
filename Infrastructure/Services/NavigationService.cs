using System.Windows.Controls;
using ImageAIRenamer.Application.ViewModels;
using ImageAIRenamer.Domain.Interfaces;
using ImageAIRenamer.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageAIRenamer.Infrastructure.Services;

public class NavigationService : INavigationService
{
    private Frame? _navigationFrame;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;
    private IDisposable? _currentViewModel;

    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void SetNavigationFrame(Frame frame)
    {
        _navigationFrame = frame;
    }

    public void NavigateToWelcome()
    {
        try
        {
            DisposeCurrentViewModel();
            var viewModel = _serviceProvider.GetRequiredService<WelcomeViewModel>();
            _currentViewModel = viewModel as IDisposable;
            _navigationFrame?.Navigate(new WelcomePage(viewModel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to welcome page");
            throw;
        }
    }

    public void NavigateToSearch()
    {
        try
        {
            DisposeCurrentViewModel();
            var viewModel = _serviceProvider.GetRequiredService<ImageSearchViewModel>();
            _currentViewModel = viewModel as IDisposable;
            _navigationFrame?.Navigate(new ImageSearchPage(viewModel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to search page");
            throw;
        }
    }

    public void NavigateToRename()
    {
        try
        {
            DisposeCurrentViewModel();
            var viewModel = _serviceProvider.GetRequiredService<ImageRenameViewModel>();
            _currentViewModel = viewModel as IDisposable;
            _navigationFrame?.Navigate(new ImageRenamePage(viewModel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to rename page");
            throw;
        }
    }

    private void DisposeCurrentViewModel()
    {
        try
        {
            _currentViewModel?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing current view model");
        }
        finally
        {
            _currentViewModel = null;
        }
    }
}
