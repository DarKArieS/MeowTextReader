using Microsoft.UI.Xaml.Controls;

namespace MeowTextReader.ReaderPage
{
    public sealed partial class SettingsDialog : UserControl
    {
        public SettingsDialog()
        {
            this.InitializeComponent();
            this.DataContext = new SettingsDialogViewModel();
        }
    }
}
