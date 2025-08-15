using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MeowTextReader
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            MainFrame.Navigate(typeof(MainPage));
        }
    }
}
