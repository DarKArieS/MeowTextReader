using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MeowTextReader.MainPage;

namespace MeowTextReader.GlobalSetting
{
    /// <summary>
    /// 清單上的一列 Regex。用物件而非裸字串，TextBox 才綁得到 TwoWay，
    /// 內容重複時也還認得出是哪一列。
    /// </summary>
    public class ChapterRegexEntry : INotifyPropertyChanged
    {
        private string _pattern;

        public ChapterRegexEntry(string pattern = "")
        {
            _pattern = pattern;
        }

        public string Pattern
        {
            get => _pattern;
            set
            {
                if (_pattern == value) return;
                _pattern = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ListViewItem 的自動化名稱會回退到 ToString()，這裡回傳 Regex 本身，
        /// 避免朗讀程式讀出型別名稱。
        /// </summary>
        public override string ToString() => _pattern;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class GlobalSettingDialogViewModel : INotifyPropertyChanged
    {
        private string? _errorMessage;
        private double _titleMaxLength;

        public ObservableCollection<ChapterRegexEntry> ChapterRegexItems { get; } = new();

        /// <summary>
        /// 章節標題字數上限。NumberBox 綁的是 double，空白輸入會給 NaN，
        /// 這種情況保留原值不動。
        /// </summary>
        public double TitleMaxLength
        {
            get => _titleMaxLength;
            set
            {
                if (double.IsNaN(value))
                {
                    OnPropertyChanged(); // 把畫面上的值改回目前設定
                    return;
                }
                if (_titleMaxLength == value) return;
                _titleMaxLength = value;
                OnPropertyChanged();
            }
        }

        public double TitleMaxLengthMinimum => MainRepo.MinChapterTitleLength;

        public double TitleMaxLengthMaximum => MainRepo.MaxChapterTitleLength;

        public ICommand RemoveCommand { get; }

        /// <summary>驗證失敗時顯示在對話框底部；null 表示沒有錯誤。</summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (_errorMessage == value) return;
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        public GlobalSettingDialogViewModel()
        {
            RemoveCommand = new RelayCommand<ChapterRegexEntry>(Remove);

            foreach (var pattern in MainRepo.Instance.ChapterRegexList)
                ChapterRegexItems.Add(new ChapterRegexEntry(pattern));

            _titleMaxLength = MainRepo.Instance.ChapterTitleMaxLength;
        }

        public void Add() => ChapterRegexItems.Add(new ChapterRegexEntry());

        /// <summary>把預設的章節 Regex 補回清單（已存在的不重複加）。</summary>
        public void RestoreDefaults()
        {
            foreach (var pattern in MainRepo.DefaultChapterRegexList)
            {
                if (ChapterRegexItems.Any(e => e.Pattern.Trim() == pattern)) continue;
                ChapterRegexItems.Add(new ChapterRegexEntry(pattern));
            }
            ErrorMessage = null;
        }

        private void Remove(ChapterRegexEntry? entry)
        {
            if (entry == null) return;
            ChapterRegexItems.Remove(entry);
            ErrorMessage = null;
        }

        /// <summary>
        /// 寫回設定檔。Regex 寫錯就不存，回傳 false 讓對話框留在原地讓使用者修正。
        /// </summary>
        public bool TrySave()
        {
            var patterns = new List<string>();

            foreach (var entry in ChapterRegexItems)
            {
                var pattern = entry.Pattern?.Trim() ?? string.Empty;
                if (pattern.Length == 0) continue; // 空白列視為使用者還沒填，直接忽略

                if (!ChapterParser.IsValidPattern(pattern))
                {
                    ErrorMessage = $"Regex 語法錯誤：{pattern}";
                    return false;
                }

                if (!patterns.Contains(pattern))
                    patterns.Add(pattern);
            }

            ErrorMessage = null;
            MainRepo.Instance.ChapterRegexList = patterns;
            MainRepo.Instance.ChapterTitleMaxLength = (int)Math.Round(_titleMaxLength);
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
