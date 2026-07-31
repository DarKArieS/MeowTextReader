using System;
using Windows.Foundation;
using MeowTextReader.Repo.Chapter;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;

namespace MeowTextReader.ReaderPage
{
    /// <summary>
    /// 章節選擇清單。原本是內嵌在 ReaderPage 的 Flyout 內容，抽出來獨立成檔案，
    /// 讓 ReaderPage.xaml 只需要提供 Flyout 外殼（位置、寬高限制、Opening/Opened 事件）。
    /// </summary>
    public sealed partial class ChapterChooseDialog : UserControl
    {
        private ReaderPageViewModel? _viewModel;

        /// <summary>
        /// 取得目前最上方可見行索引的方式；由 ReaderPage 提供，因為只有它知道
        /// 虛擬化清單目前的可見範圍。
        /// </summary>
        public Func<int>? CurrentLineIndexProvider { get; set; }

        /// <summary>取得目前最上方可見行的行內捲動比例；由 ReaderPage 提供。</summary>
        public Func<double>? CurrentLineFractionProvider { get; set; }

        /// <summary>捲動主要閱讀畫面到指定行；由 ReaderPage 提供，重新整理後用來還原原本的閱讀位置。</summary>
        public Action<int, double>? ScrollToLineAction { get; set; }

        /// <summary>使用者點選章節時觸發，捲動主要閱讀畫面是 ReaderPage 的責任。</summary>
        public event Action<ChapterItem>? ChapterSelected;

        public ChapterChooseDialog()
        {
            this.InitializeComponent();
        }

        public void Attach(ReaderPageViewModel viewModel)
        {
            _viewModel = viewModel;
            this.DataContext = viewModel;
        }

        /// <summary>Flyout 開啟前呼叫：確保章節已載入，並依內容決定寬度。</summary>
        public void PrepareOpening()
        {
            _viewModel?.EnsureChaptersLoaded();
            UpdateWidth();
        }

        /// <summary>Flyout 完全開啟後呼叫：捲到正在讀的章節。</summary>
        public void OnOpened()
        {
            ScrollListToCurrent();
        }

        /// <summary>
        /// 依最長的章節標題決定彈窗寬度。只量一次而不是讓 ListView 自己撐開，
        /// 否則虛擬化只量得到已實體化的項目，捲動時寬度會跳動。
        /// </summary>
        private void UpdateWidth()
        {
            if (_viewModel == null || _viewModel.Chapters.Count == 0)
            {
                RootGrid.Width = double.NaN;
                return;
            }

            string longest = string.Empty;
            foreach (var chapter in _viewModel.Chapters)
            {
                if (chapter.Title.Length > longest.Length) longest = chapter.Title;
            }

            var probe = new TextBlock { Text = longest };
            probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            // 行號欄 + 欄距 + ListViewItem padding
            const double Gutter = 96;

            // 上限要留給 presenter 的 padding/border 與垂直捲軸，否則內容比 presenter
            // 的可用寬度還寬，就會被裁掉。超過上限的標題交給 TextWrapping 換行。
            const double MaxContentWidth = 420;
            RootGrid.Width = Math.Clamp(probe.DesiredSize.Width + Gutter, 260, MaxContentWidth);
        }

        /// <summary>
        /// 標記並停在正在讀的章節：高亮讓人一眼看出目前位置，長篇文章也不必自己捲半天找。
        /// </summary>
        /// <param name="lineIndex">
        /// 用哪一行判斷目前章節；省略時才即時查詢主畫面目前的可見行。
        /// 重新整理後主畫面的捲動位置還沒還原，這時必須傳入重整前記下的行號，
        /// 否則會拿到重整瞬間（尚未捲回原位）的行號，標記到錯誤的章節。
        /// </param>
        private void ScrollListToCurrent(int? lineIndex = null)
        {
            if (_viewModel == null || !_viewModel.HasChapters) return;

            _viewModel.UpdateCurrentChapter(lineIndex ?? CurrentLineIndexProvider?.Invoke() ?? 0);
            int index = _viewModel.CurrentChapterIndex;
            if (index < 0) return;

            // 清單這時才剛產生容器，要等版面配置完成才捲得動。
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                ChapterListView.ScrollIntoView(_viewModel.Chapters[index], ScrollIntoViewAlignment.Leading);
            });
        }

        /// <summary>
        /// 重新整理：連同目前的 txt 檔案內容一併重讀，避免檔案已變更卻只是重新比對舊的行。
        /// 重讀會清空並重新填入 FileLines，主畫面的捲動位置會被打亂，所以先記下目前位置，
        /// 重讀完再捲回去。
        /// </summary>
        private void RefreshChapters_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            int lineIndex = CurrentLineIndexProvider?.Invoke() ?? 0;
            double lineFraction = CurrentLineFractionProvider?.Invoke() ?? 0;

            _viewModel.ReloadFileLines();
            _viewModel.LoadChapters(forceRefresh: true);
            UpdateWidth();
            ScrollListToCurrent(lineIndex);

            ScrollToLineAction?.Invoke(lineIndex, lineFraction);
        }

        private async void ChapterSettingButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_viewModel?.FileName)) return;

            var dialog = new ChapterSettingDialog(_viewModel.FileName) { XamlRoot = this.XamlRoot };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            _viewModel.LoadChapters(forceRefresh: true);
            UpdateWidth();
            ScrollListToCurrent();
        }

        private void ChapterListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not ChapterItem chapter) return;
            ChapterSelected?.Invoke(chapter);
        }
    }
}
