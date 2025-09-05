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
            ReaderTextListView.Loaded += ReaderTextListView_Loaded;
            MeowTextReader.MainRepo.Instance.LastPage = AppPage.ReaderPage;
        }

        private void ReaderPage_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindScrollViewer(ReaderTextListView);
            KeyDown += ReaderTextListView_KeyDown;
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

        private void ReaderTextListView_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindScrollViewer(ReaderTextListView);

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
                    ReaderTextListView.IsEnabled = false;
                    await Task.Delay(1000);
                    _scrollViewer.ChangeView(null, (double)offset.Value, null, true);
                    UpdateTitlePercentText();
                    LoadingRing.IsActive = false;
                    LoadingRing.Visibility = Visibility.Collapsed;
                    ReaderTextListView.IsEnabled = true;
                });
            }
        }

        private void ReaderPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_scrollViewer != null)
                _scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
            _debounceTimer?.Dispose();
            KeyDown -= ReaderTextListView_KeyDown;
            ReaderTextListView.Loaded -= ReaderTextListView_Loaded;
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
            Frame.Navigate(typeof(MeowTextReader.MainPage.MainPage));
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

        private void ReaderTextListView_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ToggleTopPanel();
            e.Handled = true;
        }

        private void ToggleTopPanel() {
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
            TopPanel.Visibility = Visibility.Collapsed;
            var tt = TopPanel.RenderTransform as TranslateTransform;
            if (tt != null) tt.Y = 0; // reset for next show
            var slideOut = (Storyboard)this.Resources["SlideOutTopPanel"];
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
                ToggleTopPanel();
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
            if (sender is FrameworkElement fe && fe.DataContext is string lineText)
            {
                // Find index of the line in the collection
                int lineNumber = ViewModel.FileLines.IndexOf(lineText) + 1; // 1-based

                var flyout = new Flyout
                {
                    Content = new TextBlock
                    {
                        Text = $"Line: {lineNumber}",
                        Margin = new Thickness(8,4,8,4)
                    }
                };
                flyout.ShowAt(fe);
            }
            e.Handled = true;
        }
    }
}