using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Threading.Tasks;
using System;

namespace MeowTextReader
{
    public sealed partial class MainPage : Page
    {
        private MainViewModel ViewModel { get; set; } = new MainViewModel();

        public MainPage()
        {
            this.InitializeComponent();
            this.RootGrid.DataContext = ViewModel;
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
            if (e.ClickedItem is FileItem fileItem && fileItem.IsFolder)
            {
                ViewModel.FolderItemClickCommand.Execute(fileItem);
            }
        }
    }
}
