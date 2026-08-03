using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using MeowTextReader.Repo;
using MeowTextReader.Repo.Chapter;
using MeowTextReader.Repo.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;

namespace MeowTextReader.ReaderPage
{
    public sealed partial class ReaderPage : Page
    {
        private ReaderPageViewModel ViewModel { get; set; } = new ReaderPageViewModel();
        private ScrollViewer? _scrollViewer;
        private Timer? _debounceTimer;
        private const int DebounceMilliseconds = 500;
        private bool _isSliderUpdating = false;
        private bool _isScrollViewerUpdating = false;

        /// <summary>
        /// 還原閱讀位置的期間為 true。還原本身會觸發 ViewChanged，
        /// 若不擋掉就會把中途（可能被夾住）的位置寫回設定檔，蓋掉正確紀錄。
        /// </summary>
        private bool _isRestoring = false;

        private bool _hasRestored = false;

        public ReaderPage()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
            this.Loaded += ReaderPage_Loaded;
            this.Unloaded += ReaderPage_Unloaded;
            ReaderTextListView.Loaded += ReaderTextListView_Loaded;
            MainRepo.Instance.LastPage = AppPage.ReaderPage;

            ChapterChooseControl.Attach(ViewModel);
            ChapterChooseControl.CurrentLineIndexProvider = () => GetFirstVisibleIndex() ?? 0;
            ChapterChooseControl.CurrentLineFractionProvider = () => GetLineFraction(GetFirstVisibleIndex() ?? 0);
            ChapterChooseControl.ScrollToLineAction = ScrollToLine;
            ChapterChooseControl.ChapterSelected += ChapterChooseControl_ChapterSelected;
        }

        private void ReaderPage_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindScrollViewer(ReaderTextListView);
            KeyDown += ReaderTextListView_KeyDown;
        }

        private ItemsStackPanel? ItemsPanel => ReaderTextListView.ItemsPanelRoot as ItemsStackPanel;

        /// <summary>
        /// 目前最上方可見行的索引。這是虛擬化下唯一可靠的位置來源：
        /// ScrollViewer 的 VerticalOffset / ExtentHeight 都只是依已實體化項目推估出來的。
        /// </summary>
        private int? GetFirstVisibleIndex()
        {
            int index = ItemsPanel?.FirstVisibleIndex ?? -1;
            return index >= 0 ? index : null;
        }

        private int? GetLastVisibleIndex()
        {
            int index = ItemsPanel?.LastVisibleIndex ?? -1;
            return index >= 0 ? index : null;
        }

        /// <summary>
        /// 算出指定行已經捲出視窗頂端的比例，範圍 [0, 1)。
        /// </summary>
        private double GetLineFraction(int lineIndex)
        {
            if (_scrollViewer == null) return 0;
            if (ReaderTextListView.ContainerFromIndex(lineIndex) is not FrameworkElement container) return 0;

            double height = container.ActualHeight;
            if (height <= 0) return 0;

            // 相對於 ScrollViewer 的座標即視窗座標；已捲過頂端的部分為負值。
            double top = container.TransformToVisual(_scrollViewer).TransformPoint(new Point(0, 0)).Y;
            return Math.Clamp(-top / height, 0, 0.999);
        }

        private void UpdateProgressUi()
        {
            int total = ViewModel.FileLines.Count;
            if (total == 0)
            {
                TitleText.Text = ViewModel.FileName;
                return;
            }

            if (!_isSliderUpdating)
            {
                _isScrollViewerUpdating = true;
                ScrollSlider.Maximum = Math.Max(total - 1, 1);
                ScrollSlider.Value = Math.Clamp(GetFirstVisibleIndex() ?? 0, 0, ScrollSlider.Maximum);
                _isScrollViewerUpdating = false;
            }

            // 以「已讀到第幾行」計算，捲到底時剛好是 100%。
            int readLines = (GetLastVisibleIndex() ?? 0) + 1;
            double percent = Math.Clamp((double)readLines / total * 100.0, 0, 100);
            TitleText.Text = ViewModel.FileName + $"({percent:0.00}%)";
        }

        private void ReaderTextListView_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindScrollViewer(ReaderTextListView);
            if (_scrollViewer == null) return;

            _scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
            _scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

            _isScrollViewerUpdating = true;
            ScrollSlider.Minimum = 0;
            ScrollSlider.Maximum = Math.Max(ViewModel.FileLines.Count - 1, 1);
            _isScrollViewerUpdating = false;

            if (!_hasRestored)
            {
                _hasRestored = true;
                RestoreReadingPosition();
            }

            UpdateProgressUi();
        }

        private void RestoreReadingPosition()
        {
            var saved = ViewModel.GetSavedPosition();
            if (saved == null || ViewModel.FileLines.Count == 0) return;

            if (saved.LineIndex is int lineIndex)
            {
                ScrollToLine(lineIndex, saved.LineFraction);
            }
            else if (saved.ScrollOffset > 0)
            {
                RestoreLegacyPixelOffset(saved.ScrollOffset);
            }
        }

        /// <summary>
        /// 捲到指定行。ScrollIntoView 由 ListView 自行處理虛擬化，
        /// 不必等 extent 估算完成，也不會被夾在還沒實體化的範圍內。
        /// </summary>
        private void ScrollToLine(int lineIndex, double lineFraction)
        {
            if (ViewModel.FileLines.Count == 0) return;
            lineIndex = Math.Clamp(lineIndex, 0, ViewModel.FileLines.Count - 1);

            _isRestoring = true;
            ReaderTextListView.ScrollIntoView(ViewModel.FileLines[lineIndex], ScrollIntoViewAlignment.Leading);

            // 容器要等下一次 layout 才就位，行內微調與解除旗標都得排在其後。
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                ApplyLineFraction(lineIndex, lineFraction);
                _isRestoring = false;
                UpdateProgressUi();
            });
        }

        private void ApplyLineFraction(int lineIndex, double lineFraction)
        {
            if (_scrollViewer == null || lineFraction <= 0) return;
            if (ReaderTextListView.ContainerFromIndex(lineIndex) is not FrameworkElement container) return;

            double top = container.TransformToVisual(_scrollViewer).TransformPoint(new Point(0, 0)).Y;
            double target = _scrollViewer.VerticalOffset + top + container.ActualHeight * lineFraction;
            _scrollViewer.ChangeView(null, target, null, true);
        }

        /// <summary>
        /// 一次性遷移：舊設定檔存的是像素偏移量，只能沿用原本「等 extent 估算完成」的做法，
        /// 還原後立刻改存行索引，之後就不會再走這條路徑。
        /// </summary>
        private void RestoreLegacyPixelOffset(int scrollOffset)
        {
            if (_scrollViewer == null) return;

            _isRestoring = true; // 同步設定，避免等待期間的 ViewChanged 先把位置寫回去
            DispatcherQueue.TryEnqueue(async () =>
            {
                LoadingRing.IsActive = true;
                LoadingRing.Visibility = Visibility.Visible;
                ReaderTextListView.IsEnabled = false;

                await Task.Delay(1000);
                _scrollViewer.ChangeView(null, scrollOffset, null, true);
                await Task.Delay(100);

                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;
                ReaderTextListView.IsEnabled = true;
                _isRestoring = false;

                SaveCurrentPosition();
                UpdateProgressUi();
            });
        }

        private void SaveCurrentPosition()
        {
            // 頁面不在視覺樹上時量不到容器座標，行內比例會被算成 0，所以這種狀態下不存。
            if (_isRestoring || !IsLoaded) return;
            if (GetFirstVisibleIndex() is not int lineIndex) return;

            // 已讀行數以最下方可見行為準，與標題上顯示的百分比一致。
            int readLines = (GetLastVisibleIndex() ?? lineIndex) + 1;
            ViewModel.SaveReadingPosition(lineIndex, GetLineFraction(lineIndex), readLines);
        }

        /// <summary>
        /// 離開頁面前立即寫入，否則最後一次捲動若落在 debounce 視窗內就會遺失。
        /// 必須在這裡做而不是 Unloaded：Unloaded 時頁面已脫離視覺樹。
        /// </summary>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SaveCurrentPosition();
            base.OnNavigatingFrom(e);
        }

        private void ReaderPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_scrollViewer != null)
                _scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;

            _debounceTimer?.Dispose();
            _debounceTimer = null;

            KeyDown -= ReaderTextListView_KeyDown;
            ReaderTextListView.Loaded -= ReaderTextListView_Loaded;
        }

        private void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            UpdateProgressUi();

            if (_isRestoring) return;

            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                // Timer callback 跑在執行緒集區，UI 物件只能在 UI 執行緒上讀取。
                DispatcherQueue.TryEnqueue(SaveCurrentPosition);
            }, null, DebounceMilliseconds, Timeout.Infinite);
        }

        private void ScrollSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_isScrollViewerUpdating || ViewModel.FileLines.Count == 0) return;

            int lineIndex = Math.Clamp((int)Math.Round(e.NewValue), 0, ViewModel.FileLines.Count - 1);
            _isSliderUpdating = true;
            ReaderTextListView.ScrollIntoView(ViewModel.FileLines[lineIndex], ScrollIntoViewAlignment.Leading);
            _isSliderUpdating = false;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage.MainPage));
        }

        private ScrollViewer? FindScrollViewer(DependencyObject parent)
        {
            if (parent is ScrollViewer sv)
                return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsTeachingTip.IsOpen)
            {
                SettingsTeachingTip.IsOpen = false;

            }
            else
            {
                SettingsTeachingTip.IsOpen = true;
            }
        }

        private void ChapterFlyout_Opening(object? sender, object e)
        {
            ChapterChooseControl.PrepareOpening();
        }

        private void ChapterFlyout_Opened(object? sender, object e)
        {
            ChapterChooseControl.OnOpened();
        }

        private void ChapterChooseControl_ChapterSelected(ChapterItem chapter)
        {
            ChapterFlyout.Hide();
            ScrollToLine(chapter.LineIndex, 0);

            // ScrollToLine 期間 _isRestoring 為 true，捲動不會被寫回設定檔；
            // 這個 callback 排在它的解旗標之後，跳轉後的位置才存得起來。
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, SaveCurrentPosition);
        }

        private void ReaderTextListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleBottomPanel();
            e.Handled = true;
        }

        private void ToggleBottomPanel() {
            if (BottomPanel.Visibility == Visibility.Visible)
            {
                var slideOut = (Storyboard)this.Resources["SlideOutBottomPanel"];
                // 設定動畫 To 為 BottomPanel.ActualHeight
                var anim = (DoubleAnimation)slideOut.Children[0];
                anim.To = BottomPanel.ActualHeight;
                slideOut.Completed += SlideOut_Completed;
                slideOut.Begin();
            }
            else
            {
                // 先將 BottomPanel 移到畫面外
                var tt = BottomPanel.RenderTransform as TranslateTransform;
                if (tt != null) tt.Y = BottomPanel.ActualHeight;
                BottomPanel.Visibility = Visibility.Visible;
                var slideIn = (Storyboard)this.Resources["SlideInBottomPanel"];
                var anim = (DoubleAnimation)slideIn.Children[0];
                anim.From = BottomPanel.ActualHeight;
                anim.To = 0;
                slideIn.Begin();
            }
        }

        private const double scrollDelta = 300;
        private void ScrollDown()
        {
            if (_scrollViewer == null) return;
            double offset = _scrollViewer.VerticalOffset;
            double max = _scrollViewer.ScrollableHeight;
            double newOffset = Math.Min(offset + scrollDelta, max);
            _scrollViewer.ChangeView(null, newOffset, null, false);
        }

        private void ScrollUp()
        {
            if (_scrollViewer == null) return;
            double offset = _scrollViewer.VerticalOffset;
            double newOffset = Math.Max(offset - scrollDelta, 0);
            _scrollViewer.ChangeView(null, newOffset, null, false);
        }

        private void SlideOut_Completed(object? sender, object e)
        {
            BottomPanel.Visibility = Visibility.Collapsed;
            var tt = BottomPanel.RenderTransform as TranslateTransform;
            if (tt != null) tt.Y = 0; // reset for next show
            var slideOut = (Storyboard)this.Resources["SlideOutBottomPanel"];
            slideOut.Completed -= SlideOut_Completed;
        }

        private void ReaderTextListView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.D)
            {
                ScrollDown();
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.U)
            {
                ScrollUp();
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Escape)
            {
                ToggleBottomPanel();
                e.Handled = true;
            }
        }

        private void LeftOverlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ScrollUp();
            e.Handled = false;
        }

        private void RightOverlay_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ScrollDown();
            e.Handled = false;
        }

        private void LineTextBlock_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is LineItem line)
            {
                var editItem = new MenuFlyoutItem { Text = "編輯此行" };
                editItem.Click += (_, _) =>
                {
                    var path = ViewModel.FilePath;
                    if (!string.IsNullOrEmpty(path))
                    {
                        ExternalEditorLauncher.OpenFileAtLine(path, line.LineNumber);
                    }
                };

                var flyout = new MenuFlyout();
                flyout.Items.Add(new MenuFlyoutItem { Text = $"Line: {line.LineNumber}", IsEnabled = false });
                flyout.Items.Add(new MenuFlyoutSeparator());
                flyout.Items.Add(editItem);

                // 有翻譯前的原文檔案時，才多給對照原文的選項。
                if (ViewModel.HasRawFile)
                {
                    var viewRawItem = new MenuFlyoutItem { Text = "查看原文" };
                    viewRawItem.Click += (_, _) =>
                    {
                        var rawPath = ViewModel.RawFilePath;
                        if (!string.IsNullOrEmpty(rawPath))
                        {
                            ExternalEditorLauncher.OpenFileAtLine(rawPath, line.LineNumber);
                        }
                    };

                    var copyRawItem = new MenuFlyoutItem { Text = "複製原文" };
                    copyRawItem.Click += (_, _) => CopyRawLine(line.Index);

                    flyout.Items.Add(new MenuFlyoutSeparator());
                    flyout.Items.Add(viewRawItem);
                    flyout.Items.Add(copyRawItem);
                }

                flyout.ShowAt(fe);
            }
            e.Handled = true;
        }

        /// <summary>
        /// 把原文檔案對應那一行複製到剪貼簿。原文行數不足（取不到那一行）時什麼都不做。
        /// </summary>
        private void CopyRawLine(int lineIndex)
        {
            var text = ViewModel.GetRawLineText(lineIndex);
            if (text == null) return;

            var package = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
            package.SetText(text);
            Clipboard.SetContent(package);
        }
    }
}