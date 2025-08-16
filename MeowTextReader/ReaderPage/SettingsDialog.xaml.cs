using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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
            // �u���\�Ʀr�B�R���B��V��
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

        private async void PickColor_Click(object sender, RoutedEventArgs e)
        {
            var colorPicker = new ColorPicker();
            var dialog = new ContentDialog
            {
                Title = "����C��",
                Content = colorPicker,
                PrimaryButtonText = "�T�w",
                CloseButtonText = "����",
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var color = colorPicker.Color;
                string hex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
                if (DataContext is SettingsDialogViewModel vm)
                {
                    vm.CustomColorText = hex;
                }
            }
        }

        private async void PickTextColor_Click(object sender, RoutedEventArgs e)
        {
            var colorPicker = new ColorPicker();
            var dialog = new ContentDialog
            {
                Title = "��ܤ�r�C��",
                Content = colorPicker,
                PrimaryButtonText = "�T�w",
                CloseButtonText = "����",
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var color = colorPicker.Color;
                string hex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
                if (DataContext is SettingsDialogViewModel vm)
                {
                    vm.CustomTextColorText = hex;
                }
            }
        }
    }
}
