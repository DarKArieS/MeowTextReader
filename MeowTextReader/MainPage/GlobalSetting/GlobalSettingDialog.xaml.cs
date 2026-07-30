using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MeowTextReader.MainPage.GlobalSetting
{
    /// <summary>
    /// 不綁定單一檔案的全域設定。目前只有章節抓取用的 Regex 清單。
    /// </summary>
    public sealed partial class GlobalSettingDialog : ContentDialog
    {
        private GlobalSettingDialogViewModel ViewModel { get; } = new GlobalSettingDialogViewModel();

        public GlobalSettingDialog()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        private void AddRegex_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Add();
        }

        private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RestoreDefaults();
        }

        private void SaveButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Regex 寫錯就把對話框留著，讓使用者照著錯誤訊息修正。
            if (!ViewModel.TrySave())
                args.Cancel = true;
        }
    }
}
