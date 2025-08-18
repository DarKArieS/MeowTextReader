using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeowTextReader.ReaderPage
{
    public class SettingsDialogViewModel : INotifyPropertyChanged
    {
        private double _fontSize;
        private bool _isCustomColor;
        private string? _customBackgroundColorText;
        private string? _customTextColorText;
        private MainRepo repo = MainRepo.Instance;

        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged();
                    repo.FontSize = value;
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
                        repo.SetBackgroundColor(null, false);
                        repo.SetForegroundColor(null, false);
                    }
                    else if (string.IsNullOrWhiteSpace(CustomBackgroundColorText))
                    {
                        // Do Nothing
                    }
                    else
                    {
                        repo.SetBackgroundColor(CustomBackgroundColorText, true);
                        repo.SetForegroundColor(CustomTextColorText, true);
                    }
                }
            }
        }

        public string? CustomBackgroundColorText
        {
            get => _customBackgroundColorText;
            set
            {
                if (_customBackgroundColorText != value)
                {
                    _customBackgroundColorText = value;
                    OnPropertyChanged();
                    if (IsCustomColor && !string.IsNullOrWhiteSpace(value))
                    {
                        repo.SetBackgroundColor(value, true);
                    }
                }
            }
        }

        public string? CustomTextColorText
        {
            get => _customTextColorText;
            set
            {
                if (_customTextColorText != value)
                {
                    _customTextColorText = value;
                    OnPropertyChanged();
                    if (IsCustomColor && !string.IsNullOrWhiteSpace(value))
                    {
                        repo.SetForegroundColor(value, true);
                    }
                }
            }
        }

        public SettingsDialogViewModel()
        {
            _fontSize = repo.FontSize;
            var setting = repo.ReaderSettingObj;
            // ≠I¥∫¶‚
            if (setting.UseCustomBackgroundColor && !string.IsNullOrWhiteSpace(setting.CustomBackgroundColor))
            {
                _isCustomColor = true;
                _customBackgroundColorText = setting.CustomBackgroundColor;
            }
            else
            {
                _isCustomColor = false;
                if (!string.IsNullOrWhiteSpace(setting.CustomBackgroundColor))
                {
                    _customBackgroundColorText = setting.CustomBackgroundColor;
                }
                else {
                    _customBackgroundColorText = null;
                }
            }
            // §Â¶r√C¶‚
            if (setting.UseCustomForegroundColor && !string.IsNullOrWhiteSpace(setting.CustomForegroundColor))
            {
                _customTextColorText = setting.CustomForegroundColor;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(setting.CustomForegroundColor))
                {
                    _customTextColorText = setting.CustomForegroundColor;
                }
                else
                {
                    _customTextColorText = null;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
