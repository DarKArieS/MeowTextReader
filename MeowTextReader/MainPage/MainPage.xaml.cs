using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Threading.Tasks;
using System;
using MeowTextReader.ReaderPage;

namespace MeowTextReader
{
    public sealed partial class MainPage : Page
    {
        private MainPageViewModel ViewModel { get; set; } = new MainPageViewModel();

        public MainPage()
        {
            this.InitializeComponent();
            this.RootGrid.DataContext = ViewModel;
            MainRepo.Instance.LastPage = AppPage.MainPage;
        }

        private async void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = await picker.PickSingleFolderAsync().AsTask();
            if (folder != null)
            {
                ViewModel.FolderPath = folder.Path;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.BackCommand.Execute(null);
        }

        private void FolderListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is FileItem fileItem)
            {
                if (fileItem.IsFolder)
                {
                    ViewModel.FolderItemClickCommand.Execute(fileItem);
                }
                else if (fileItem.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    // 將完整路徑存入 MainRepo appConfig.json
                    var filePath = System.IO.Path.Combine(ViewModel.FolderPath ?? string.Empty, fileItem.Name);
                    MainRepo.Instance.SetOpenFilePath(filePath);
                    // 跳轉到 ReaderPage
                    Frame.Navigate(typeof(MeowTextReader.ReaderPage.ReaderPage));
                }
            }
        }
    }
}
