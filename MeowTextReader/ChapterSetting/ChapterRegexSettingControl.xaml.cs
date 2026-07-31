using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MeowTextReader.Repo.Chapter;

namespace MeowTextReader.ChapterSetting
{
    /// <summary>
    /// 章節抓取設定區塊（Regex 清單、標題字數上限、開頭跳過行數）。
    /// 由 GlobalSettingDialog（編輯全域預設值）與 ChapterSettingDialog（編輯單一檔案的專屬設定）共用。
    /// </summary>
    public sealed partial class ChapterRegexSettingControl : UserControl
    {
        public ChapterRegexSettingViewModel ViewModel { get; private set; } = null!;

        public ChapterRegexSettingControl()
        {
            this.InitializeComponent();
        }

        /// <summary>用初始設定建立編輯用的 ViewModel。呼叫端決定初始值來自全域預設值還是單一檔案。</summary>
        public void Initialize(ChapterRegexSetting initial)
        {
            ViewModel = new ChapterRegexSettingViewModel(initial);
            this.DataContext = ViewModel;
        }

        /// <summary>驗證並取出編輯結果，Regex 寫錯就回傳 false。</summary>
        public bool TrySave(out ChapterRegexSetting result) => ViewModel.TrySave(out result);

        private void AddRegex_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Add();
        }

        private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RestoreDefaults();
        }
    }
}
