using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

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
    }
}
