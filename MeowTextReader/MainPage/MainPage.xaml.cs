using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using MeowTextReader.MainPage.GlobalSetting;
using MeowTextReader.Repo;
using MeowTextReader.Repo.Model;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;

namespace MeowTextReader.MainPage
{
    public sealed partial class MainPage : Page
    {
        private MainPageViewModel ViewModel { get; set; } = new MainPageViewModel();
        private Timer? _debounceTimer;
        private const int DebounceMilliseconds = 1000;

        /// <summary>
        /// 目前位置要存到哪個資料夾底下。切換資料夾時 ViewModel.FolderPath 會先被改掉，
        /// 所以存檔時不能直接讀它。
        /// </summary>
        private string? _currentFolderKey;

        private bool _isRestoring;

        public MainPage()
        {
            this.InitializeComponent();
            this.RootGrid.DataContext = ViewModel;
            MainRepo.Instance.LastPage = AppPage.MainPage;

            _currentFolderKey = ViewModel.FolderPath;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            this.Loaded += MainPage_Loaded;
            this.Unloaded += MainPage_Unloaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            FolderScrollViewer.ViewChanged += FolderScrollViewer_ViewChanged;
            RestoreScrollPosition();
        }

        /// <summary>
        /// 離開頁面前立即寫入，否則最後一次捲動若落在 debounce 視窗內就會遺失。
        /// 必須在這裡做而不是 Unloaded：Unloaded 時頁面已脫離視覺樹，量不到正確座標。
        /// </summary>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SaveScrollPosition();
            base.OnNavigatingFrom(e);
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            FolderScrollViewer.ViewChanged -= FolderScrollViewer_ViewChanged;
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainPageViewModel.FolderPath)) return;

            // 這裡跑在 LoadFolderItems 之前，清單還是舊資料夾的內容，正好可以存位置。
            SaveScrollPosition();
            _debounceTimer?.Dispose();
            _debounceTimer = null;

            _currentFolderKey = ViewModel.FolderPath;
            RestoreScrollPosition();
        }

        private void FolderScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            if (_isRestoring) return;

            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                // Timer callback 跑在執行緒集區，UI 物件只能在 UI 執行緒上讀取。
                DispatcherQueue.TryEnqueue(SaveScrollPosition);
            }, null, DebounceMilliseconds, Timeout.Infinite);
        }

        /// <summary>
        /// 最上方可見項目的索引。ListView 包在 ScrollViewer 裡不會虛擬化，
        /// 所有容器都已產生，直接比對座標即可；找不到任何容器時回傳 null，
        /// 以免在清單還沒就緒時把位置覆蓋成 0。
        /// </summary>
        private int? GetFirstVisibleIndex()
        {
            int count = ViewModel.FolderItems.Count;
            bool hasContainer = false;

            for (int i = 0; i < count; i++)
            {
                if (FolderListView.ContainerFromIndex(i) is not FrameworkElement container) continue;
                hasContainer = true;

                double top = container.TransformToVisual(FolderScrollViewer).TransformPoint(new Point(0, 0)).Y;
                if (top + container.ActualHeight > 0.5) return i;
            }

            return hasContainer ? count - 1 : null;
        }

        private void SaveScrollPosition()
        {
            // 頁面不在視覺樹上時所有座標都會量成 0，第一個可見項目會誤判成 0，
            // 反而把正確的位置蓋掉，所以這種狀態下寧可不存。
            if (_isRestoring || !IsLoaded || string.IsNullOrEmpty(_currentFolderKey)) return;
            if (GetFirstVisibleIndex() is not int itemIndex) return;

            ViewModel.SaveScrollPosition(_currentFolderKey, itemIndex, FolderScrollViewer.HorizontalOffset);
        }

        private void RestoreScrollPosition()
        {
            var saved = ViewModel.GetSavedScrollPosition(_currentFolderKey);

            _isRestoring = true;
            // 換資料夾時清單要等 LoadFolderItems 與版面配置完成，容器才產生得出來。
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                FolderListView.UpdateLayout();
                if (saved != null)
                {
                    ScrollToItem(saved.ItemIndex, saved.HorizontalOffset);
                }
                else
                {
                    FolderScrollViewer.ChangeView(0, 0, null, true); // 沒紀錄的資料夾從頂端開始
                }
                _isRestoring = false;
            });
        }

        private void ScrollToItem(int itemIndex, double horizontalOffset)
        {
            int count = ViewModel.FolderItems.Count;
            if (count == 0)
            {
                FolderScrollViewer.ChangeView(0, 0, null, true);
                return;
            }

            itemIndex = Math.Clamp(itemIndex, 0, count - 1);
            if (FolderListView.ContainerFromIndex(itemIndex) is not FrameworkElement container)
            {
                FolderScrollViewer.ChangeView(horizontalOffset, null, null, true);
                return;
            }

            // 轉成視窗座標後再加回目前的捲動量，得到內容座標。
            double top = container.TransformToVisual(FolderScrollViewer).TransformPoint(new Point(0, 0)).Y;
            FolderScrollViewer.ChangeView(horizontalOffset, top + FolderScrollViewer.VerticalOffset, null, true);
        }

        private async void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);
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

        private async void GlobalSetting_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new GlobalSettingDialog { XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
        }

        private void RawConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 平常是背景延遲寫入，先同步落地一次，確保編輯器看到的是最新內容
                // （順帶保證檔案存在，不必再自己寫一份空的 "{}" 上去）。
                MainRepo.Instance.Flush();
                Process.Start(new ProcessStartInfo
                {
                    FileName = MainRepo.Instance.SaveFilePath,
                    UseShellExecute = true
                });
            }
            catch { }
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
                    var filePath = Path.Combine(ViewModel.FolderPath ?? string.Empty, fileItem.Name);
                    MainRepo.Instance.SetOpenFilePath(filePath);
                    // 跳轉到 ReaderPage
                    Frame.Navigate(typeof(ReaderPage.ReaderPage));
                }
            }
        }
    }
}
