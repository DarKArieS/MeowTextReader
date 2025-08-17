using Microsoft.UI.Xaml;

namespace MeowTextReader
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            // �ھ� appConfig �O���������M�w�Ұʭ�
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
