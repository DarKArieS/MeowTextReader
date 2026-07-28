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
        private double _lineSpacing;
        private Brush? _backgroundBrush;
        private Brush? _foregroundBrush;
        public ObservableCollection<LineItem> FileLines { get; } = new();

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
                    OnPropertyChanged(nameof(LineHeight));
                }
            }
        }

        /// <summary>
        /// 行距倍率。設定值本身，實際渲染用 <see cref="LineHeight"/>。
        /// </summary>
        public double LineSpacing
        {
            get => _lineSpacing;
            set
            {
                if (_lineSpacing != value)
                {
                    _lineSpacing = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineHeight));
                }
            }
        }

        /// <summary>
        /// 每一行文字的實際行高（含行距）。
        /// </summary>
        public double LineHeight => FontSize * LineSpacing;

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

        public Brush? ForegroundBrush
        {
            get => _foregroundBrush;
            set
            {
                if (_foregroundBrush != value)
                {
                    _foregroundBrush = value;
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
            LineSpacing = MainRepo.Instance.LineSpacing;
            UpdateBackgroundBrush();
            UpdateForegroundBrush();
            MainRepo.ReaderSettingChanged += OnReaderSettingChanged;
        }

        private void OnReaderSettingChanged()
        {
            FontSize = MainRepo.Instance.FontSize;
            LineSpacing = MainRepo.Instance.LineSpacing;
            UpdateBackgroundBrush();
            UpdateForegroundBrush();
        }

        private void LoadFileLines(string? path)
        {
            FileLines.Clear();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                    FileLines.Add(new LineItem(i, lines[i]));
            }
        }

        /// <summary>
        /// 以行索引 + 行內比例記錄閱讀位置，同時記下已讀行數供 MainPage 顯示進度。
        /// </summary>
        public void SaveReadingPosition(int lineIndex, double lineFraction, int readLines)
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                MainRepo.Instance.UpdateHistory(FileName, lineIndex, lineFraction, readLines, FileLines.Count);
            }
        }

        public MainRepo.HistoryItem? GetSavedPosition()
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                return MainRepo.Instance.GetHistoryItem(FileName);
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
                // #RRGGBB
                var color = ColorHelper.FromArgb(
                    255,
                    Convert.ToByte(colorStr.Substring(1, 2), 16),
                    Convert.ToByte(colorStr.Substring(3, 2), 16),
                    Convert.ToByte(colorStr.Substring(5, 2), 16));
                BackgroundBrush = new SolidColorBrush(color);
            }
            catch
            {
                BackgroundBrush = null;
            }
        }

        private void UpdateForegroundBrush()
        {
            var setting = MainRepo.Instance.ReaderSettingObj;
            if (!setting.UseCustomForegroundColor || string.IsNullOrWhiteSpace(setting.CustomForegroundColor))
            {
                ForegroundBrush = null;
                return;
            }
            try
            {
                var colorStr = setting.CustomForegroundColor;
                // #RRGGBB
                var color = ColorHelper.FromArgb(
                    255,
                    Convert.ToByte(colorStr.Substring(1, 2), 16),
                    Convert.ToByte(colorStr.Substring(3, 2), 16),
                    Convert.ToByte(colorStr.Substring(5, 2), 16));
                ForegroundBrush = new SolidColorBrush(color);
            }
            catch
            {
                ForegroundBrush = null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}