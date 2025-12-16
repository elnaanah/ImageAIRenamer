using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageAIRenamer.Pages
{
    public partial class WelcomePage : Page
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        private void OpenAISudio_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://aistudio.google.com/",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void NavigateToSearch_Click(object sender, MouseButtonEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.NavigateToSearch();
            }
        }

        private void NavigateToRename_Click(object sender, MouseButtonEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.NavigateToRename();
            }
        }
    }
}
