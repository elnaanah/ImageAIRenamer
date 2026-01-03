using System.Windows.Controls;

namespace ImageAIRenamer.Domain.Interfaces;

public interface INavigationService
{
    void SetNavigationFrame(Frame frame);
    void NavigateToWelcome();
    void NavigateToSearch();
    void NavigateToRename();
}
