using Microsoft.UI.Xaml;

namespace MeowTextReader
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            // 根據 appConfig 記錄的頁面決定啟動頁
            var lastPage = MainRepo.Instance.LastPage;
            if (lastPage == AppPage.ReaderPage)
            {
                MainFrame.Navigate(typeof(MeowTextReader.ReaderPage.ReaderPage));
            }
            else
            {
                MainFrame.Navigate(typeof(MainPage.MainPage));
            }
        }
    }
}
