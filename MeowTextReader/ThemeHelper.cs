using MeowTextReader.Repo;
using MeowTextReader.Repo.Model;
using Microsoft.UI.Xaml;

namespace MeowTextReader
{
    /// <summary>
    /// ContentDialog 等以 Popup 呈現的控制項不會自動繼承視窗根元素的 RequestedTheme，
    /// 需要另外套用，否則主題切換時會維持系統預設外觀，跟主視窗不一致。
    /// </summary>
    public static class ThemeHelper
    {
        public static ElementTheme ToElementTheme(AppTheme theme) => theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        public static void ApplyCurrentTheme(FrameworkElement element)
        {
            element.RequestedTheme = ToElementTheme(MainRepo.Instance.Theme);
        }
    }
}
