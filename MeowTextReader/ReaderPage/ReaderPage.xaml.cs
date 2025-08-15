using Microsoft.UI.Xaml.Controls;

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
    }
}