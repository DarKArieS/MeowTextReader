using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace MeowTextReader
{
    public sealed partial class MainWindow : Window
    {
        // 拖曳／縮放過程中 AppWindow.Changed 會連續觸發，延遲寫檔避免頻繁 I/O。
        private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(500);

        private readonly DispatcherQueueTimer _savePlacementTimer;

        /// <summary>
        /// 最近一次「非最大化」的視窗位置與大小。最大化時 AppWindow 回報的是全螢幕範圍，
        /// 直接存起來會讓下次還原視窗時佔滿整個螢幕，所以另外記住還原用的尺寸。
        /// </summary>
        private RectInt32? _restoreBounds;

        public MainWindow()
        {
            this.InitializeComponent();

            _savePlacementTimer = DispatcherQueue.CreateTimer();
            _savePlacementTimer.Interval = SaveDelay;
            _savePlacementTimer.IsRepeating = false;
            _savePlacementTimer.Tick += (_, _) => SaveWindowPlacement();

            RestoreWindowPlacement();
            AppWindow.Changed += OnAppWindowChanged;
            Closed += OnClosed;

            // 根據 appConfig 記錄的頁面決定啟動頁
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

        private void RestoreWindowPlacement()
        {
            var placement = MainRepo.Instance.WindowPlacement;
            if (placement == null || placement.Width <= 0 || placement.Height <= 0)
            {
                return;
            }

            var bounds = ClampToVisibleDisplay(
                new RectInt32(placement.X, placement.Y, placement.Width, placement.Height));

            _restoreBounds = bounds;
            AppWindow.MoveAndResize(bounds);

            if (placement.IsMaximized && AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }

        /// <summary>
        /// 螢幕數量或解析度可能在兩次開啟之間改變，把視窗拉回還看得到的工作區內。
        /// </summary>
        private static RectInt32 ClampToVisibleDisplay(RectInt32 bounds)
        {
            var display = DisplayArea.GetFromRect(bounds, DisplayAreaFallback.Nearest);
            if (display == null)
            {
                return bounds;
            }

            var area = display.WorkArea;
            var width = Math.Min(bounds.Width, area.Width);
            var height = Math.Min(bounds.Height, area.Height);
            var x = Math.Clamp(bounds.X, area.X, area.X + area.Width - width);
            var y = Math.Clamp(bounds.Y, area.Y, area.Y + area.Height - height);
            return new RectInt32(x, y, width, height);
        }

        private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (!args.DidPositionChange && !args.DidSizeChange && !args.DidPresenterChange)
            {
                return;
            }

            CaptureRestoreBounds();
            _savePlacementTimer.Start(); // 重新計時，達到防抖效果
        }

        private void CaptureRestoreBounds()
        {
            if (GetPresenterState() != OverlappedPresenterState.Restored)
            {
                return;
            }

            var position = AppWindow.Position;
            var size = AppWindow.Size;
            if (size.Width <= 0 || size.Height <= 0)
            {
                return;
            }

            _restoreBounds = new RectInt32(position.X, position.Y, size.Width, size.Height);
        }

        private void SaveWindowPlacement()
        {
            var state = GetPresenterState();
            if (state == OverlappedPresenterState.Minimized || _restoreBounds is not { } bounds)
            {
                return; // 最小化時的座標沒有還原價值
            }

            MainRepo.Instance.WindowPlacement = new WindowPlacement
            {
                X = bounds.X,
                Y = bounds.Y,
                Width = bounds.Width,
                Height = bounds.Height,
                IsMaximized = state == OverlappedPresenterState.Maximized
            };
        }

        private OverlappedPresenterState? GetPresenterState()
        {
            return (AppWindow.Presenter as OverlappedPresenter)?.State;
        }

        private void OnClosed(object sender, WindowEventArgs args)
        {
            _savePlacementTimer.Stop();
            CaptureRestoreBounds();
            SaveWindowPlacement();
        }
    }
}
