using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MeowTextReader.MainPage;
using MeowTextReader.Repo.Chapter;

namespace MeowTextReader.ChapterSetting
{
    /// <summary>
    /// 章節抓取設定區塊（Regex 清單、標題字數上限、開頭跳過行數）的共用邏輯。
    /// 不知道自己編輯的是全域預設值還是單一檔案的專屬設定，由呼叫端決定初始值與存檔目標，
    /// 讓 GlobalSettingDialog 與 ChapterSettingDialog 可以共用同一份 UI 與驗證邏輯。
    /// </summary>
    public class ChapterRegexSettingViewModel : INotifyPropertyChanged
    {
        private string? _errorMessage;
        private double _titleMaxLength;
        private double _skipLines;

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

        public double TitleMaxLengthMinimum => ChapterRegexSetting.MinTitleMaxLength;

        public double TitleMaxLengthMaximum => ChapterRegexSetting.MaxTitleMaxLength;

        /// <summary>
        /// 開頭要跳過的行數，跳過的行不參與章節比對。NumberBox 綁的是 double，
        /// 空白輸入會給 NaN，這種情況保留原值不動。
        /// </summary>
        public double SkipLines
        {
            get => _skipLines;
            set
            {
                if (double.IsNaN(value))
                {
                    OnPropertyChanged(); // 把畫面上的值改回目前設定
                    return;
                }
                if (_skipLines == value) return;
                _skipLines = value;
                OnPropertyChanged();
            }
        }

        public double SkipLinesMinimum => ChapterRegexSetting.MinSkipLines;

        public double SkipLinesMaximum => ChapterRegexSetting.MaxSkipLines;

        public ICommand RemoveCommand { get; }

        /// <summary>驗證失敗時顯示在畫面底部；null 表示沒有錯誤。</summary>
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

        public ChapterRegexSettingViewModel(ChapterRegexSetting initial)
        {
            RemoveCommand = new RelayCommand<ChapterRegexEntry>(Remove);

            foreach (var pattern in initial.EffectiveRegexList())
                ChapterRegexItems.Add(new ChapterRegexEntry(pattern));

            _titleMaxLength = initial.EffectiveTitleMaxLength();
            _skipLines = initial.EffectiveSkipLines();
        }

        public void Add() => ChapterRegexItems.Add(new ChapterRegexEntry());

        /// <summary>把預設的章節 Regex 補回清單（已存在的不重複加）。</summary>
        public void RestoreDefaults()
        {
            foreach (var pattern in ChapterRegexSetting.DefaultRegexList)
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
        /// 驗證並打包成 <see cref="ChapterRegexSetting"/>。Regex 寫錯就不回傳，
        /// 回傳 false 讓呼叫端把畫面留在原地讓使用者修正。
        /// </summary>
        public bool TrySave(out ChapterRegexSetting result)
        {
            result = new ChapterRegexSetting();
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
            result.ChapterRegexList = patterns;
            result.ChapterTitleMaxLength = (int)Math.Round(_titleMaxLength);
            result.ChapterSkipLines = (int)Math.Round(_skipLines);
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
