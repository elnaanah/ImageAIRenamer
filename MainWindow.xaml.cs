using System.Windows;
using ImageAIRenamer.Pages;

namespace ImageAIRenamer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            NavigateToWelcome();
        }

        public void NavigateToWelcome()
        {
            MainFrame.Navigate(new WelcomePage());
        }

        public void NavigateToSearch()
        {
            MainFrame.Navigate(new ImageSearchPage());
        }

        public void NavigateToRename()
        {
            MainFrame.Navigate(new ImageRenamePage());
        }
    }
}
