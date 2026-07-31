using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using MeowTextReader.Repo;
using MeowTextReader.Repo.Chapter;
using MeowTextReader.Repo.Model;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace MeowTextReader.ReaderPage
{
    public class ReaderPageViewModel : INotifyPropertyChanged
    {
        private string? _fileName;
        private double _fontSize;
        private double _lineSpacing;
        private Brush? _backgroundBrush;
        private Brush? _foregroundBrush;
        private bool _chaptersLoaded;
        private bool _hasChapters;
        private int _currentChapterIndex = -1;
        public ObservableCollection<LineItem> FileLines { get; } = new();

        /// <summary>目前檔案掃描出來的章節。第一次打開章節清單時才載入。</summary>
        public ObservableCollection<ChapterItem> Chapters { get; } = new();

        /// <summary>沒有章節時要改顯示提示文字，而不是一片空白的清單。</summary>
        public bool HasChapters
        {
            get => _hasChapters;
            private set
            {
                if (_hasChapters == value) return;
                _hasChapters = value;
                OnPropertyChanged();
            }
        }

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
        /// 第一次打開章節清單時才掃描。開檔時就掃會讓大檔案的開啟變慢，
        /// 而多數時候使用者根本不會打開章節清單。
        /// </summary>
        public void EnsureChaptersLoaded()
        {
            if (_chaptersLoaded) return;
            LoadChapters(forceRefresh: false);
        }

        /// <summary>
        /// 載入章節。有沿用得上的快取就直接用，否則重新掃描並寫回閱讀紀錄，
        /// 下次開同一個檔案就不必再掃。
        /// </summary>
        public void LoadChapters(bool forceRefresh)
        {
            _chaptersLoaded = true;
            Chapters.Clear();
            _currentChapterIndex = -1;

            if (string.IsNullOrEmpty(FileName))
            {
                HasChapters = false;
                return;
            }

            var patterns = MainRepo.Instance.ChapterRegexList;
            var titleMaxLength = MainRepo.Instance.ChapterTitleMaxLength;
            var skipLines = MainRepo.Instance.ChapterSkipLines;
            var cacheKey = MainRepo.BuildChapterCacheKey(patterns, titleMaxLength, skipLines, FileLines.Count);
            var history = MainRepo.Instance.GetHistoryItem(FileName);

            List<ChapterItem>? chapters = null;
            if (!forceRefresh && history?.Chapters != null && history.ChapterCacheKey == cacheKey)
                chapters = history.Chapters;

            if (chapters == null)
            {
                chapters = ChapterParser.Parse(
                    FileLines.Select(l => l.Text).ToList(), patterns, titleMaxLength, skipLines);
                MainRepo.Instance.UpdateChapters(FileName, chapters, cacheKey);
            }

            foreach (var chapter in chapters)
                Chapters.Add(chapter);

            HasChapters = Chapters.Count > 0;
        }

        /// <summary>
        /// 指定行所屬的章節在清單中的位置（該行之前最近的一個章節）。找不到時回傳 -1。
        /// </summary>
        public int FindChapterIndexForLine(int lineIndex)
        {
            int result = -1;
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].LineIndex > lineIndex) break;
                result = i;
            }
            return result;
        }

        /// <summary>
        /// 依目前閱讀到的行數，重新計算並標記正在讀的章節（<see cref="ChapterItem.IsCurrent"/>）。
        /// 每次打開或重新整理章節清單時呼叫，結果供畫面高亮與自動捲動使用。
        /// </summary>
        public void UpdateCurrentChapter(int lineIndex)
        {
            int index = FindChapterIndexForLine(lineIndex);
            if (index == _currentChapterIndex) return;

            if (_currentChapterIndex >= 0 && _currentChapterIndex < Chapters.Count)
                Chapters[_currentChapterIndex].IsCurrent = false;

            _currentChapterIndex = index;

            if (_currentChapterIndex >= 0 && _currentChapterIndex < Chapters.Count)
                Chapters[_currentChapterIndex].IsCurrent = true;
        }

        /// <summary>目前正在讀的章節在清單中的位置，需先呼叫 <see cref="UpdateCurrentChapter"/>。</summary>
        public int CurrentChapterIndex => _currentChapterIndex;

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

        public HistoryItem? GetSavedPosition()
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