using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Input;
using Windows.System;

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

        public ReaderPage()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
            this.Loaded += ReaderPage_Loaded;
            this.Unloaded += ReaderPage_Unloaded;
            FileListView.Loaded += FileListView_Loaded;
            MeowTextReader.MainRepo.Instance.LastPage = AppPage.ReaderPage;
        }

        private void ReaderPage_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindScrollViewer(FileListView);
        }

        private void UpdateTitlePercentText()
        {
            if (_scrollViewer != null && ScrollSlider.Maximum > 0)
            {
                double percent = (ScrollSlider.Value / ScrollSlider.Maximum) * 100.0;
                if (percent < 0) percent = 0;
                if (percent > 100) percent = 100;
                TitleText.Text = ViewModel.FileName + $"({percent:0.00}%)";
            }
            else
            {
                TitleText.Text = "0%";
            }
        }

        private void FileListView_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindScrollViewer(FileListView);

            if (_scrollViewer != null)
            {
                ScrollSlider.Minimum = 0;
                ScrollSlider.Maximum = _scrollViewer.ScrollableHeight > 0 ? _scrollViewer.ScrollableHeight : 1;
                ScrollSlider.Value = _scrollViewer.VerticalOffset;
                _scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
                UpdateTitlePercentText();
            }

            var offset = ViewModel.GetSavedScrollOffset();
            if (_scrollViewer != null && offset.HasValue)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingRing.IsActive = true;
                    LoadingRing.Visibility = Visibility.Visible;
                    FileListView.IsEnabled = false;
                    await Task.Delay(1000);
                    _scrollViewer.ChangeView(null, (double)offset.Value, null, true);
                    UpdateTitlePercentText();
                    LoadingRing.IsActive = false;
                    LoadingRing.Visibility = Visibility.Collapsed;
                    FileListView.IsEnabled = true;
                });
            }
        }

        private void ReaderPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_scrollViewer != null)
                _scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
            _debounceTimer?.Dispose();
            FileListView.Loaded -= FileListView_Loaded;
        }

        private void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                var offset = _scrollViewer?.VerticalOffset ?? 0;
                ViewModel.SaveScrollOffset(offset);
            }, null, DebounceMilliseconds, Timeout.Infinite);

            if (_scrollViewer != null && !_isSliderUpdating)
            {
                _isScrollViewerUpdating = true;
                ScrollSlider.Maximum = _scrollViewer.ScrollableHeight > 0 ? _scrollViewer.ScrollableHeight : 1;
                ScrollSlider.Value = _scrollViewer.VerticalOffset;
                UpdateTitlePercentText();
                _isScrollViewerUpdating = false;
            }
        }

        private void ScrollSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_scrollViewer != null && !_isScrollViewerUpdating)
            {
                _isSliderUpdating = true;
                _scrollViewer.ChangeView(null, e.NewValue, null, true);
                UpdateTitlePercentText();
                _isSliderUpdating = false;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MeowTextReader.MainPage));
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
            SettingsTeachingTip.IsOpen = true;
        }

        private void FileListView_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (TopPanel.Visibility == Visibility.Visible)
            {
                var slideOut = (Storyboard)this.Resources["SlideOutTopPanel"];
                // 設定動畫 To 為 -TopPanel.ActualHeight
                var anim = (DoubleAnimation)slideOut.Children[0];
                anim.To = -TopPanel.ActualHeight;
                slideOut.Completed += SlideOut_Completed;
                slideOut.Begin();
            }
            else
            {
                // 先將 TopPanel 移到畫面外
                var tt = TopPanel.RenderTransform as TranslateTransform;
                if (tt != null) tt.Y = -TopPanel.ActualHeight;
                TopPanel.Visibility = Visibility.Visible;
                var slideIn = (Storyboard)this.Resources["SlideInTopPanel"];
                var anim = (DoubleAnimation)slideIn.Children[0];
                anim.From = -TopPanel.ActualHeight;
                anim.To = 0;
                slideIn.Begin();
            }
        }

        private void SlideOut_Completed(object? sender, object e)
        {
            TopPanel.Visibility = Visibility.Collapsed;
            var tt = TopPanel.RenderTransform as TranslateTransform;
            if (tt != null) tt.Y = 0; // reset for next show
            var slideOut = (Storyboard)this.Resources["SlideOutTopPanel"];
            slideOut.Completed -= SlideOut_Completed;
        }

        private void FileListView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_scrollViewer == null) return;
            double offset = _scrollViewer.VerticalOffset;
            double max = _scrollViewer.ScrollableHeight;
            const double delta = 300;
            if (e.Key == VirtualKey.D)
            {
                double newOffset = Math.Min(offset + delta, max);
                _scrollViewer.ChangeView(null, newOffset, null, false);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.U)
            {
                double newOffset = Math.Max(offset - delta, 0);
                _scrollViewer.ChangeView(null, newOffset, null, false);
                e.Handled = true;
            }
        }
    }
}