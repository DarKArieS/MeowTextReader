using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace MeowTextReader
{
    public enum AppPage
    {
        MainPage,
        ReaderPage
    }

    public class ReaderSetting // 移出 MainRepo class，作為獨立 public class
    {
        public double FontSize { get; set; } = 20.0;

        /// <summary>
        /// 行距倍率，實際行高為 FontSize * LineSpacing。
        /// </summary>
        public double LineSpacing { get; set; } = 1.5;

        public string? CustomBackgroundColor { get; set; } = null; // 改名
        public bool UseCustomBackgroundColor { get; set; } = false; // 新增
        public string? CustomForegroundColor { get; set; } = null; // 新增
        public bool UseCustomForegroundColor { get; set; } = false; // 新增
    }

    /// <summary>
    /// 視窗上次關閉時的位置與大小（實體像素）。最大化時記錄的是還原後的大小，
    /// 這樣下次開啟先最大化、使用者按還原時才會回到合理尺寸。
    /// </summary>
    public class WindowPlacement
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsMaximized { get; set; }
    }

    public class MainRepo
    {
        private static readonly Lazy<MainRepo> _instance = new(() => new MainRepo());
        public static MainRepo Instance => _instance.Value;

        private readonly string _saveFilePath;
        private AppConfig _config = new();
        private AppPage? _lastPageCache = null;

        public class HistoryItem
        {
            public string? FileName { get; set; }

            /// <summary>
            /// 舊版欄位：像素捲動偏移量。僅供一次性資料遷移，新程式不再寫入。
            /// </summary>
            public int ScrollOffset { get; set; }

            /// <summary>
            /// 最上方可見行的索引（0-based）。這是位置的絕對錨點，不受字體大小、
            /// 視窗尺寸或虛擬化估算誤差影響。
            /// </summary>
            public int? LineIndex { get; set; }

            /// <summary>
            /// 在 LineIndex 該行內的細部偏移比例，範圍 [0, 1)。
            /// </summary>
            public double LineFraction { get; set; }
        }

        /// <summary>
        /// MainPage 各資料夾的瀏覽位置。以項目索引為錨點，理由同 <see cref="HistoryItem.LineIndex"/>。
        /// </summary>
        public class FolderScrollHistoryItem
        {
            public string? FolderPath { get; set; }

            /// <summary>最上方可見項目的索引（0-based）。</summary>
            public int ItemIndex { get; set; }

            public double HorizontalOffset { get; set; }
        }

        private class AppConfig
        {
            public string? folderPath { get; set; }
            public string? OpenFilePath { get; set; }
            public string? LastPage { get; set; } // serialized as string
            public ReaderSetting ReaderSetting { get; set; } = new ReaderSetting();
            public List<HistoryItem> history { get; set; } = new();
            public List<FolderScrollHistoryItem> FolderScrollPositions { get; set; } = new();
            public WindowPlacement? WindowPlacement { get; set; }
        }

        private MainRepo()
        {
            _saveFilePath = GetSaveFilePath();
            LoadConfig();
        }

        private static string GetSaveFilePath()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(folder, "MeowTextReader");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "appConfig.json");
        }

        public string? FolderPath
        {
            get => _config.folderPath;
            set
            {
                if (_config.folderPath != value)
                {
                    _config.folderPath = value;
                    SaveConfig();
                }
            }
        }

        public string? OpenFilePath
        {
            get => _config.OpenFilePath;
            set
            {
                if (_config.OpenFilePath != value)
                {
                    _config.OpenFilePath = value;
                    SaveConfig();
                }
            }
        }

        public AppPage LastPage
        {
            get
            {
                if (_lastPageCache.HasValue) return _lastPageCache.Value;
                if (Enum.TryParse<AppPage>(_config.LastPage, out var page))
                {
                    _lastPageCache = page;
                    return page;
                }
                return AppPage.MainPage;
            }
            set
            {
                if (LastPage != value)
                {
                    _config.LastPage = value.ToString();
                    _lastPageCache = value;
                    SaveConfig();
                }
            }
        }

        private ReaderSetting _readerSettingCache => _config.ReaderSetting ??= new ReaderSetting();
        public ReaderSetting ReaderSettingObj
        {
            get => _readerSettingCache;
            set
            {
                _config.ReaderSetting = value;
                SaveConfig();
            }
        }
        public double FontSize
        {
            get => _readerSettingCache.FontSize;
            set
            {
                if (_readerSettingCache.FontSize != value)
                {
                    _readerSettingCache.FontSize = value;
                    SaveConfig();
                    ReaderSettingChanged?.Invoke();
                }
            }
        }

        // 下限不設 1.0：行高等於字級時中文字的上下會被裁掉。
        public const double MinLineSpacing = 1.2;
        public const double MaxLineSpacing = 3.0;
        public const double LineSpacingStep = 0.1;
        public const double DefaultLineSpacing = 1.5;

        public double LineSpacing
        {
            get
            {
                // 舊設定檔沒有這個欄位時可能讀到 0，視為未設定。
                var value = _readerSettingCache.LineSpacing;
                return value > 0 ? value : DefaultLineSpacing;
            }
            set
            {
                var clamped = Math.Round(Math.Clamp(value, MinLineSpacing, MaxLineSpacing), 1);
                if (_readerSettingCache.LineSpacing != clamped)
                {
                    _readerSettingCache.LineSpacing = clamped;
                    SaveConfig();
                    ReaderSettingChanged?.Invoke();
                }
            }
        }

        public static event Action? ReaderSettingChanged;

        public List<HistoryItem> History => _config.history;

        /// <summary>
        /// 記錄閱讀位置。以行索引為錨點，不再儲存像素偏移量。
        /// </summary>
        public void UpdateHistory(string fileName, int lineIndex, double lineFraction)
        {
            var item = _config.history.FirstOrDefault(h => h.FileName == fileName);
            if (item == null)
            {
                item = new HistoryItem { FileName = fileName };
                _config.history.Add(item);
            }
            else if (item.LineIndex == lineIndex && Math.Abs(item.LineFraction - lineFraction) < 0.01)
            {
                return; // 位置沒有實質變化，不需要重寫設定檔
            }

            item.LineIndex = lineIndex;
            item.LineFraction = lineFraction;
            item.ScrollOffset = 0; // 舊欄位已遷移完成
            SaveConfig();
        }

        public HistoryItem? GetHistoryItem(string fileName)
        {
            return _config.history.FirstOrDefault(h => h.FileName == fileName);
        }

        /// <summary>
        /// 記錄某個資料夾在 MainPage 的瀏覽位置。
        /// </summary>
        public void UpdateFolderScrollPosition(string folderPath, int itemIndex, double horizontalOffset)
        {
            if (string.IsNullOrEmpty(folderPath)) return;

            var item = FindFolderScrollItem(folderPath);
            if (item == null)
            {
                item = new FolderScrollHistoryItem { FolderPath = folderPath };
                _config.FolderScrollPositions.Add(item);
            }
            else if (item.ItemIndex == itemIndex && Math.Abs(item.HorizontalOffset - horizontalOffset) < 1.0)
            {
                return; // 位置沒有實質變化，不需要重寫設定檔
            }

            item.ItemIndex = itemIndex;
            item.HorizontalOffset = horizontalOffset;
            SaveConfig();
        }

        public FolderScrollHistoryItem? GetFolderScrollPosition(string folderPath)
        {
            return string.IsNullOrEmpty(folderPath) ? null : FindFolderScrollItem(folderPath);
        }

        private FolderScrollHistoryItem? FindFolderScrollItem(string folderPath)
        {
            return _config.FolderScrollPositions.FirstOrDefault(
                f => string.Equals(f.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));
        }

        public WindowPlacement? WindowPlacement
        {
            get => _config.WindowPlacement;
            set
            {
                var current = _config.WindowPlacement;
                if (current != null && value != null
                    && current.X == value.X && current.Y == value.Y
                    && current.Width == value.Width && current.Height == value.Height
                    && current.IsMaximized == value.IsMaximized)
                {
                    return; // 沒有實質變化，不需要重寫設定檔
                }

                _config.WindowPlacement = value;
                SaveConfig();
            }
        }

        public void SetOpenFilePath(string path)
        {
            OpenFilePath = path;
        }

        public void SetBackgroundColor(string? color, bool useCustom)
        {
            if (useCustom) {
                _readerSettingCache.CustomBackgroundColor = color;
            }
            _readerSettingCache.UseCustomBackgroundColor = useCustom;
            SaveConfig();
            ReaderSettingChanged?.Invoke();
        }

        public void SetForegroundColor(string? color, bool useCustom)
        {
            if (useCustom) {
                _readerSettingCache.CustomForegroundColor = color;
            }
            _readerSettingCache.UseCustomForegroundColor = useCustom;
            SaveConfig();
            ReaderSettingChanged?.Invoke();
        }

        private void SaveConfig()
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_saveFilePath, json);
        }

        private void LoadConfig()
        {
            if (File.Exists(_saveFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_saveFilePath);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch
                {
                    _config = new AppConfig();
                }
            }
        }
    }
}
