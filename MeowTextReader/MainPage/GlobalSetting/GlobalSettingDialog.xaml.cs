using MeowTextReader.Repo;
using Microsoft.UI.Xaml.Controls;

namespace MeowTextReader.MainPage.GlobalSetting
{
    /// <summary>
    /// 不綁定單一檔案的全域設定。目前只有章節抓取設定，單一檔案沒有專屬設定時以此為預設值。
    /// </summary>
    public sealed partial class GlobalSettingDialog : ContentDialog
    {
        public GlobalSettingDialog()
        {
            this.InitializeComponent();
            ChapterRegexControl.Initialize(MainRepo.Instance.GlobalChapterSetting);
        }

        private void SaveButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Regex 寫錯就把對話框留著，讓使用者照著錯誤訊息修正。
            if (!ChapterRegexControl.TrySave(out var result))
            {
                args.Cancel = true;
                return;
            }
            MainRepo.Instance.GlobalChapterSetting = result;
        }
    }
}
