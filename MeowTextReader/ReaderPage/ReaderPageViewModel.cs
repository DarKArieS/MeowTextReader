using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace MeowTextReader.ReaderPage
{
    public class ReaderPageViewModel : INotifyPropertyChanged
    {
        private string? _fileName;
        private double _fontSize;
        private Brush? _backgroundBrush;
        public ObservableCollection<string> FileLines { get; } = new();

        public string? FileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    OnPropertyChanged();
                }
            }
        }

        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public Brush? BackgroundBrush
        {
            get => _backgroundBrush;
            set
            {
                if (_backgroundBrush != value)
                {
                    _backgroundBrush = value;
                    OnPropertyChanged();
                }
            }
        }

        public ReaderPageViewModel()
        {
            var path = MainRepo.Instance.OpenFilePath;
            if (!string.IsNullOrEmpty(path))
            {
                FileName = Path.GetFileNameWithoutExtension(path);
                LoadFileLines(path);
            }
            FontSize = MainRepo.Instance.FontSize;
            UpdateBackgroundBrush();
            MainRepo.ReaderSettingChanged += OnReaderSettingChanged;
        }

        private void OnReaderSettingChanged()
        {
            FontSize = MainRepo.Instance.FontSize;
            UpdateBackgroundBrush();
        }

        private void LoadFileLines(string? path)
        {
            FileLines.Clear();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                    FileLines.Add(line);
            }
        }

        public void SaveScrollOffset(double offset)
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                MainRepo.Instance.UpdateHistory(FileName, offset);
            }
        }

        public int? GetSavedScrollOffset()
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                return MainRepo.Instance.GetHistoryScrollOffset(FileName);
            }
            return null;
        }

        private void UpdateBackgroundBrush()
        {
            var setting = MainRepo.Instance.ReaderSettingObj;
            if (!setting.UseCustomBackgroundColor || string.IsNullOrWhiteSpace(setting.CustomBackgroundColor))
            {
                BackgroundBrush = null;
                return;
            }
            try
            {
                var colorStr = setting.CustomBackgroundColor;
                var color = ColorHelper.FromArgb(
                    Convert.ToByte(colorStr.Substring(1, 2), 16),
                    Convert.ToByte(colorStr.Substring(3, 2), 16),
                    Convert.ToByte(colorStr.Substring(5, 2), 16),
                    Convert.ToByte(colorStr.Substring(7, 2), 16));
                BackgroundBrush = new SolidColorBrush(color);
            }
            catch
            {
                BackgroundBrush = null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}