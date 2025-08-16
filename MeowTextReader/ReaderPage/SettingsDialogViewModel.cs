using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeowTextReader.ReaderPage
{
    public class SettingsDialogViewModel : INotifyPropertyChanged
    {
        private double _fontSize;
        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged();
                    MeowTextReader.MainRepo.Instance.FontSize = value;
                }
            }
        }

        public SettingsDialogViewModel()
        {
            _fontSize = MeowTextReader.MainRepo.Instance.FontSize;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
