namespace ImageAIRenamer.Domain.Interfaces;

/// <summary>
/// Service interface for navigation between pages
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the welcome page
    /// </summary>
    void NavigateToWelcome();

    /// <summary>
    /// Navigates to the image search page
    /// </summary>
    void NavigateToSearch();

    /// <summary>
    /// Navigates to the image rename page
    /// </summary>
    void NavigateToRename();
}
