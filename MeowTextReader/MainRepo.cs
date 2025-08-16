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

        private string? _folderPath;
        private readonly string _saveFilePath;
        private AppConfig _config = new();
        private AppPage? _lastPageCache = null;

        public class HistoryItem
        {
            public string? FileName { get; set; }
            public int ScrollOffset { get; set; }
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

        public void UpdateHistory(string fileName, double scrollOffset)
        {
            int offsetInt = (int)Math.Round(scrollOffset);
            var item = _config.history.FirstOrDefault(h => h.FileName == fileName);
            if (item == null)
            {
                item = new HistoryItem { FileName = fileName, ScrollOffset = offsetInt };
                _config.history.Add(item);
            }
            else
            {
                item.ScrollOffset = offsetInt;
            }
            SaveConfig();
        }

        public int? GetHistoryScrollOffset(string fileName)
        {
            var item = _config.history.FirstOrDefault(h => h.FileName == fileName);
            return item?.ScrollOffset;
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
