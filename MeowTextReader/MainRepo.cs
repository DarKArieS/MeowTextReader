using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace MeowTextReader
{
    public class MainRepo
    {
        private static readonly Lazy<MainRepo> _instance = new(() => new MainRepo());
        public static MainRepo Instance => _instance.Value;

        private string? _folderPath;
        private readonly string _saveFilePath;
        private AppConfig _config = new();

        public class HistoryItem
        {
            public string? FileName { get; set; }
            public int ScrollOffset { get; set; }
        }

        private class AppConfig
        {
            public string? folderPath { get; set; }
            public string? OpenFilePath { get; set; }
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
