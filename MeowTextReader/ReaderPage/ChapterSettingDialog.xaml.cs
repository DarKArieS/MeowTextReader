using MeowTextReader.Repo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MeowTextReader.ReaderPage
{
    /// <summary>
    /// 單一檔案專屬的章節抓取設定。內容與 GlobalSettingDialog 共用同一個
    /// ChapterRegexSettingControl，差別只在存檔目標是這個檔案的 HistoryItem 而非全域預設值。
    /// </summary>
    public sealed partial class ChapterSettingDialog : ContentDialog
    {
        private readonly string _fileName;

        public ChapterSettingDialog(string fileName)
        {
            this.InitializeComponent();
            _fileName = fileName;

            // 編輯畫面永遠顯示這個檔案已經存起來的自訂設定（不管開關狀態），
            // 使用者關掉「使用預設值」時才不會看到空白或全域預設值蓋掉原本填的內容。
            ChapterRegexControl.Initialize(MainRepo.Instance.GetStoredChapterSetting(fileName));

            bool useDefault = MainRepo.Instance.GetHistoryItem(fileName)?.UseDefaultChapterSetting ?? true;
            UseDefaultCheckBox.IsChecked = useDefault;
            ChapterRegexControl.Visibility = useDefault ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UseDefaultCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ChapterRegexControl.Visibility =
                UseDefaultCheckBox.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SaveButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (UseDefaultCheckBox.IsChecked == true)
            {
                // 只切換開關，不動底下已經存的自訂設定，之後取消勾選還能拿回原本填的值。
                MainRepo.Instance.SetChapterSettingUseDefault(_fileName, true);
                return;
            }

            // Regex 寫錯就把對話框留著，讓使用者照著錯誤訊息修正。
            if (!ChapterRegexControl.TrySave(out var result))
            {
                args.Cancel = true;
                return;
            }
            MainRepo.Instance.SetChapterSetting(_fileName, result);
        }
    }
}
