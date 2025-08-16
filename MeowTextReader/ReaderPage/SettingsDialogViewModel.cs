using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeowTextReader.ReaderPage
{
    public class SettingsDialogViewModel : INotifyPropertyChanged
    {
        private double _fontSize;
        private bool _isCustomColor;
        private string? _customColorText;

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

        public bool IsCustomColor
        {
            get => _isCustomColor;
            set
            {
                if (_isCustomColor != value)
                {
                    _isCustomColor = value;
                    OnPropertyChanged();
                    if (!value)
                    {
                        MeowTextReader.MainRepo.Instance.SetBackgroundColor(null, false);
                    }
                    else if (string.IsNullOrWhiteSpace(CustomColorText))
                    {
                        // Do Nothing
                    }
                    else
                    {
                        MeowTextReader.MainRepo.Instance.SetBackgroundColor(CustomColorText, true);
                    }
                }
            }
        }

        public string? CustomColorText
        {
            get => _customColorText;
            set
            {
                if (_customColorText != value)
                {
                    _customColorText = value;
                    OnPropertyChanged();
                    if (IsCustomColor && !string.IsNullOrWhiteSpace(value))
                    {
                        MeowTextReader.MainRepo.Instance.SetBackgroundColor(value, true);
                    }
                }
            }
        }

        public SettingsDialogViewModel()
        {
            _fontSize = MeowTextReader.MainRepo.Instance.FontSize;
            var setting = MeowTextReader.MainRepo.Instance.ReaderSettingObj;
            if (setting.UseCustomBackgroundColor && !string.IsNullOrWhiteSpace(setting.CustomBackgroundColor))
            {
                _isCustomColor = true;
                _customColorText = setting.CustomBackgroundColor;
            }
            else
            {
                _isCustomColor = false;
                _customColorText = null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
