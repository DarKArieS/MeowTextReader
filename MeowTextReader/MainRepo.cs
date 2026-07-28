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
        public string? CustomBackgroundColor { get; set; } = null; // 改名
        public bool UseCustomBackgroundColor { get; set; } = false; // 新增
        public string? CustomForegroundColor { get; set; } = null; // 新增
        public bool UseCustomForegroundColor { get; set; } = false; // 新增
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

        private class AppConfig
        {
            public string? folderPath { get; set; }
            public string? OpenFilePath { get; set; }
            public string? LastPage { get; set; } // serialized as string
            public ReaderSetting ReaderSetting { get; set; } = new ReaderSetting();
            public List<HistoryItem> history { get; set; } = new();
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
