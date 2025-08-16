using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeowTextReader.ReaderPage
{
    public class SettingsDialogViewModel : INotifyPropertyChanged
    {
        private double _fontSize;
        private bool _isCustomColor;
        private string? _customColorText;
        private bool _isCustomTextColor;
        private string? _customTextColorText;

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

        public bool IsCustomTextColor
        {
            get => _isCustomTextColor;
            set
            {
                if (_isCustomTextColor != value)
                {
                    _isCustomTextColor = value;
                    OnPropertyChanged();
                    if (!value)
                    {
                        MeowTextReader.MainRepo.Instance.SetForegroundColor(null, false);
                    }
                    else if (string.IsNullOrWhiteSpace(CustomTextColorText))
                    {
                        // Do Nothing
                    }
                    else
                    {
                        MeowTextReader.MainRepo.Instance.SetForegroundColor(CustomTextColorText, true);
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
                    if (IsCustomTextColor && !string.IsNullOrWhiteSpace(value))
                    {
                        MeowTextReader.MainRepo.Instance.SetForegroundColor(value, true);
                    }
                }
            }
        }

        public SettingsDialogViewModel()
        {
            _fontSize = MeowTextReader.MainRepo.Instance.FontSize;
            var setting = MeowTextReader.MainRepo.Instance.ReaderSettingObj;
            // ≠I¥∫¶‚
            if (setting.UseCustomBackgroundColor && !string.IsNullOrWhiteSpace(setting.CustomBackgroundColor))
            {
                _isCustomColor = true;
                _customColorText = setting.CustomBackgroundColor;
            }
            else
            {
                _isCustomColor = false;
                if (!string.IsNullOrWhiteSpace(setting.CustomBackgroundColor))
                {
                    _customColorText = setting.CustomBackgroundColor;
                }
                else {
                    _customColorText = null;
                }
            }
            // §Â¶r√C¶‚
            if (setting.UseCustomForegroundColor && !string.IsNullOrWhiteSpace(setting.CustomForegroundColor))
            {
                _isCustomTextColor = true;
                _customTextColorText = setting.CustomForegroundColor;
            }
            else
            {
                _isCustomTextColor = false;
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
