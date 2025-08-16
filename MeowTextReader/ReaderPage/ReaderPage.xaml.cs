using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MeowTextReader.ReaderPage;

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

        private void UpdateSliderPercentText()
        {
            if (_scrollViewer != null && ScrollSlider.Maximum > 0)
            {
                double percent = (ScrollSlider.Value / ScrollSlider.Maximum) * 100.0;
                if (percent < 0) percent = 0;
                if (percent > 100) percent = 100;
                SliderPercentText.Text = $"({percent:0.00}%)";
            }
            else
            {
                SliderPercentText.Text = "0%";
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
                UpdateSliderPercentText();
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
                    UpdateSliderPercentText();
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
                UpdateSliderPercentText();
                _isScrollViewerUpdating = false;
            }
        }

        private void ScrollSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_scrollViewer != null && !_isScrollViewerUpdating)
            {
                _isSliderUpdating = true;
                _scrollViewer.ChangeView(null, e.NewValue, null, true);
                UpdateSliderPercentText();
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

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Content = new SettingsDialog(),
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}