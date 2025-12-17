using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace ImageAIRenamer.Application.Common;

/// <summary>
/// Base class for all ViewModels with common functionality
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Shows an error message to the user
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="caption">Dialog caption</param>
    protected void ShowError(string message, string caption = "خطأ")
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    /// <summary>
    /// Shows an information message to the user
    /// </summary>
    /// <param name="message">Information message</param>
    /// <param name="caption">Dialog caption</param>
    protected void ShowInfo(string message, string caption = "معلومات")
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    /// <summary>
    /// Shows a warning message to the user
    /// </summary>
    /// <param name="message">Warning message</param>
    /// <param name="caption">Dialog caption</param>
    protected void ShowWarning(string message, string caption = "تنبيه")
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }
}
