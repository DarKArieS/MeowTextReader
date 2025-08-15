using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace MeowTextReader.ReaderPage
{
    public sealed partial class ReaderPage : Page
    {
        private ReaderPageViewModel ViewModel { get; set; } = new ReaderPageViewModel();

        public ReaderPage()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // ¦^¨ì MainPage
            Frame.Navigate(typeof(MeowTextReader.MainPage));
        }
    }
}