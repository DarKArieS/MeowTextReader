using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using System.Threading;

namespace MeowTextReader.ReaderPage
{
    public sealed partial class ReaderPage : Page
    {
        private ReaderPageViewModel ViewModel { get; set; } = new ReaderPageViewModel();
        private ScrollViewer? _scrollViewer;
        private Timer? _debounceTimer;
        private const int DebounceMilliseconds = 1000;

        public ReaderPage()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
            this.Loaded += ReaderPage_Loaded;
            this.Unloaded += ReaderPage_Unloaded;
        }

        private void ReaderPage_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindScrollViewer(FileListView);
            if (_scrollViewer != null)
            {
                // 還原歷史捲動位置
                var offset = ViewModel.GetSavedScrollOffset();
                if (offset.HasValue)
                {
                    _scrollViewer.ChangeView(null, offset.Value, null);
                }

                _scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            }
        }

        private void ReaderPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_scrollViewer != null)
                _scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
            _debounceTimer?.Dispose();
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // Reset debounce timer
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                var offset = _scrollViewer?.VerticalOffset ?? 0;
                ViewModel.SaveScrollOffset(offset);
            }, null, DebounceMilliseconds, Timeout.Infinite);
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
    }
}