using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.UI;

namespace MeowTextReader.ReaderPage
{
    public sealed partial class SettingsDialog : UserControl
    {
        public SettingsDialog()
        {
            this.InitializeComponent();
            this.DataContext = new SettingsDialogViewModel();
        }

        private void DecreaseFontSize_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (DataContext is SettingsDialogViewModel vm)
            {
                if (vm.FontSize > 1)
                    vm.FontSize -= 1;
            }
        }

        private void IncreaseFontSize_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (DataContext is SettingsDialogViewModel vm)
            {
                vm.FontSize += 1;
            }
        }

        private void FontSizeTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // 只允許數字、刪除、方向鍵
            if (!(e.Key >= Windows.System.VirtualKey.Number0 && e.Key <= Windows.System.VirtualKey.Number9) &&
                !(e.Key >= Windows.System.VirtualKey.NumberPad0 && e.Key <= Windows.System.VirtualKey.NumberPad9) &&
                e.Key != Windows.System.VirtualKey.Back &&
                e.Key != Windows.System.VirtualKey.Delete &&
                e.Key != Windows.System.VirtualKey.Left &&
                e.Key != Windows.System.VirtualKey.Right &&
                e.Key != Windows.System.VirtualKey.Tab)
            {
                e.Handled = true;
            }
        }

        private Color? getColorFromString(string? colorStr)
        {
            if (!string.IsNullOrWhiteSpace(colorStr) && colorStr.StartsWith("#") && colorStr.Length == 7)
            {
                try
                {
                    byte r = Convert.ToByte(colorStr.Substring(1, 2), 16);
                    byte g = Convert.ToByte(colorStr.Substring(3, 2), 16);
                    byte b = Convert.ToByte(colorStr.Substring(5, 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
                catch { }
            }
            return null;
        }

        private async void PickColor_Click(object sender, RoutedEventArgs e)
        {
            var colorPicker = new ColorPicker();
            SettingsDialogViewModel? vm = DataContext as SettingsDialogViewModel;
            if (vm != null)
            {
                var color = getColorFromString(vm.CustomBackgroundColorText);
                if (color != null)
                {
                    colorPicker.Color = color.Value;
                }
            }
            var dialog = new ContentDialog
            {
                Title = "選擇顏色",
                Content = colorPicker,
                PrimaryButtonText = "確定",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var color = colorPicker.Color;
                string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                if (vm != null)
                {
                    vm.CustomBackgroundColorText = hex;
                }
            }
        }

        private async void PickTextColor_Click(object sender, RoutedEventArgs e)
        {
            var colorPicker = new ColorPicker();
            SettingsDialogViewModel? vm = DataContext as SettingsDialogViewModel;
            if (vm != null)
            {
                var color = getColorFromString(vm.CustomTextColorText);
                if (color != null)
                {
                    colorPicker.Color = color.Value;
                }
            }
            var dialog = new ContentDialog
            {
                Title = "選擇文字顏色",
                Content = colorPicker,
                PrimaryButtonText = "確定",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var color = colorPicker.Color;
                string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                if (vm != null)
                {
                    vm.CustomTextColorText = hex;
                }
            }
        }
    }
}
