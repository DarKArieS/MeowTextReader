using MeowTextReader.Repo;
using MeowTextReader.Repo.Model;
using Microsoft.UI.Xaml.Controls;

namespace MeowTextReader.MainPage.GlobalSetting
{
    /// <summary>
    /// 不綁定單一檔案的全域設定。目前有外觀（淺色／深色主題）與章節抓取設定，
    /// 單一檔案沒有專屬設定時以章節設定為預設值。
    /// </summary>
    public sealed partial class GlobalSettingDialog : ContentDialog
    {
        public GlobalSettingDialog()
        {
            this.InitializeComponent();
            ThemeHelper.ApplyCurrentTheme(this);
            ChapterRegexControl.Initialize(MainRepo.Instance.GlobalChapterSetting);

            var radio = MainRepo.Instance.Theme switch
            {
                AppTheme.Light => ThemeLightRadio,
                AppTheme.Dark => ThemeDarkRadio,
                _ => ThemeDefaultRadio
            };
            radio.IsChecked = true;
        }

        private void ThemeRadio_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // 主題切換即時套用，不需要等按「儲存」。
            if (ReferenceEquals(sender, ThemeLightRadio))
            {
                MainRepo.Instance.Theme = AppTheme.Light;
            }
            else if (ReferenceEquals(sender, ThemeDarkRadio))
            {
                MainRepo.Instance.Theme = AppTheme.Dark;
            }
            else
            {
                MainRepo.Instance.Theme = AppTheme.Default;
            }

            // 這個對話框本身也是用 Popup 呈現，不會自動跟著主視窗即時換膚，要自己套用。
            ThemeHelper.ApplyCurrentTheme(this);
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
