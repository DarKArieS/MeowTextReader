using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MeowTextReader
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private MainViewModel ViewModel { get; set; } = new MainViewModel();

        public MainWindow()
        {
            this.InitializeComponent();
            RootGrid.DataContext = ViewModel;
        }

        private async void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                ViewModel.FolderPath = folder.Path;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.BackCommand.Execute(null);
        }

        private void ListViewItem_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is FileItem fileItem && fileItem.IsFolder)
            {
                ViewModel.FolderItemClickCommand.Execute(fileItem);
            }
        }

        private void FolderListView_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var originalSource = e.OriginalSource as Microsoft.UI.Xaml.FrameworkElement;
            while (originalSource != null && originalSource.DataContext is not FileItem)
            {
                originalSource = originalSource.Parent as Microsoft.UI.Xaml.FrameworkElement;
            }
            if (originalSource?.DataContext is FileItem fileItem && fileItem.IsFolder)
            {
                ViewModel.FolderItemClickCommand.Execute(fileItem);
            }
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
