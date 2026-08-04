using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using MeowTextReader.Repo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using WinRT.Interop;

namespace MeowTextReader.MainPage.ConfigSetting
{
    /// <summary>
    /// 設定檔位置的管理面板：顯示目前路徑、變更位置、用預設程式打開檔案或所在資料夾。
    /// </summary>
    public sealed partial class ConfigSettingFlyout : Flyout
    {
        public ConfigSettingFlyout()
        {
            InitializeComponent();
        }

        private void Flyout_Opening(object? sender, object e)
        {
            RefreshPath();
        }

        private void RefreshPath()
        {
            var path = MainRepo.Instance.SaveFilePath;
            PathTextBox.Text = path;
            DefaultHintText.Visibility = ConfigLocation.IsDefault(path)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            try
            {
                // 平常是背景延遲寫入，先同步落地一次，確保編輯器看到的是最新內容
                // （順帶保證檔案存在）。
                MainRepo.Instance.Flush();
                Process.Start(new ProcessStartInfo
                {
                    FileName = MainRepo.Instance.SaveFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConfigSetting] 打開設定檔失敗: {ex}");
                _ = ShowMessageAsync("打開失敗", $"無法打開設定檔：\n{ex.Message}");
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            try
            {
                MainRepo.Instance.Flush();
                var path = MainRepo.Instance.SaveFilePath;

                // /select 會打開資料夾並且把設定檔選起來，比單純打開資料夾好找。
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConfigSetting] 打開資料夾失敗: {ex}");
                _ = ShowMessageAsync("打開失敗", $"無法打開資料夾：\n{ex.Message}");
            }
        }

        private async void ChangeLocation_Click(object sender, RoutedEventArgs e)
        {
            Hide();

            var currentPath = MainRepo.Instance.SaveFilePath;

            var picker = new FileSavePicker();
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindowInstance));
            picker.FileTypeChoices.Add("JSON 設定檔", new List<string> { ".json" });
            picker.SuggestedFileName = Path.GetFileNameWithoutExtension(currentPath);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var file = await picker.PickSaveFileAsync();
            if (file == null) return;

            var newPath = file.Path;
            if (string.Equals(Path.GetFullPath(newPath), Path.GetFullPath(currentPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                return; // 選到同一個檔案，什麼都不用做
            }

            // FileSavePicker 選到不存在的檔名時會先建立一個 0 byte 的空檔，
            // 所以不能用 File.Exists 判斷，要看長度才知道那裡本來有沒有東西。
            var info = new FileInfo(newPath);
            bool hasContent = info.Exists && info.Length > 0;

            if (!hasContent)
            {
                MoveTo(newPath);
                return;
            }

            if (!MainRepo.IsValidConfigFile(newPath))
            {
                var overwrite = await ShowChoiceAsync(
                    "目標檔案不是設定檔",
                    $"{newPath}\n\n這個檔案已經有內容，但不是能解析的設定檔。要用目前的設定覆蓋它嗎？",
                    "覆蓋");
                if (overwrite) MoveTo(newPath);
                return;
            }

            // 目標已經是一份有效的設定檔：載入它（要重啟）還是拿目前的蓋掉，由使用者決定。
            var dialog = new ContentDialog
            {
                Title = "目標已經有一份設定檔",
                // 按鈕文字要短：ContentDialog 三顆按鈕平分寬度，字一多就會被截掉，
                // 所以「會重新啟動」寫在內文而不是按鈕上。
                Content = $"{newPath}\n\n這個位置已經有一份可用的設定檔，要改用它嗎？\n改用會重新啟動程式。",
                PrimaryButtonText = "改用那一份",
                SecondaryButtonText = "改用目前設定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRootOrNull()
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                SwitchToAndRestart(newPath);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                MoveTo(newPath);
            }
        }

        private void MoveTo(string newPath)
        {
            try
            {
                MainRepo.Instance.MoveSaveLocationTo(newPath);
                RefreshPath();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConfigSetting] 變更設定檔位置失敗: {ex}");
                _ = ShowMessageAsync("變更失敗", $"設定檔位置沒有變更，原本的設定仍然保留。\n\n{ex.Message}");
            }
        }

        /// <summary>
        /// 只記下新路徑就重新啟動，刻意不把記憶體裡的設定寫過去 —— 否則會蓋掉使用者想載入的那一份。
        /// 舊位置的檔案原封不動留著。
        /// </summary>
        private void SwitchToAndRestart(string newPath)
        {
            try
            {
                ConfigLocation.Save(newPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConfigSetting] 記錄設定檔位置失敗: {ex}");
                _ = ShowMessageAsync("變更失敗", $"設定檔位置沒有變更。\n\n{ex.Message}");
                return;
            }

            AppInstance.Restart(string.Empty);
        }

        private static XamlRoot? XamlRootOrNull()
        {
            return App.MainWindowInstance?.Content?.XamlRoot;
        }

        private static async Task ShowMessageAsync(string title, string message)
        {
            var root = XamlRootOrNull();
            if (root == null) return;

            await new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "確定",
                XamlRoot = root
            }.ShowAsync();
        }

        private static async Task<bool> ShowChoiceAsync(
            string title, string message, string primaryText)
        {
            var root = XamlRootOrNull();
            if (root == null) return false;

            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = primaryText,
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = root
            };
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
    }
}
